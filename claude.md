# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test

```bash
# Build
cd src && dotnet build OpenXmlHtml.slnx

# Run all tests (both net10.0 and net48)
cd src && dotnet test OpenXmlHtml.slnx

# Run single framework
cd src && dotnet test OpenXmlHtml.slnx --framework net10.0

# Run single test
cd src && dotnet test OpenXmlHtml.slnx --filter "TestName"
```

The solution file is at `src/OpenXmlHtml.slnx`. All commands must run from the `src/` directory.

Local `Release` builds need `-p:IsPackable=false`, or SponsorCheck fails the build with `SC100: Platform fetch failed. GitHub GraphQL HTTP 401` — it only bundles on CI, where it has credentials.

Building the test project triggers MarkdownSnippets, which updates `readme.md` from `#region` snippets in `src/OpenXmlHtml.Tests/Samples/`.

## Verify Snapshot Testing

Tests use [Verify](https://github.com/VerifyTests/Verify) with NUnit. When test output changes:
- `.received.*` files appear next to `.verified.*` files
- **Received and verified are not named the same stem**, so accepting is a rename. Received carries a target-framework tag; verified is named for the runtime:
  - `Foo.DotNet10_0.received.xml` → `Foo.DotNet.verified.xml`
  - `Foo.Net4_8.received.xml` → `Foo.Net.verified.xml`

  Copying the stem verbatim writes a second, parallel snapshot corpus no test ever reads, leaving the real baseline stale and the suite still failing. `dotnet verify accept` derives one name from the other and does the same thing — do not use it here. Map by hand.
- A new snapshot test needs **both** baselines, since the suite multi-targets `net10.0` and `net48`. A missing `.Net.verified.xml` fails only on the net48 run.
- Tests producing docx binaries or floating-point output use `.UniqueForTargetFrameworkAndVersion()` since output differs between net10.0 and net48
- Custom Verify converters in `VerifyOpenXmlConverter.cs` serialize OpenXml objects to `.OuterXml`

**Reading the png snapshots.** A `.verified.png` is Morph's deterministic render of the docx, so it is coupled to the exact `Morph.Skia` version pinned in `src/Directory.Packages.props`, and two failure shapes look alike:

- **PNG moved, docx identical** → the rasterizer changed, not this library. Every `Morph.Skia` bump so far has done this: 1.1.3→1.1.4 and 1.1.5 each failed a wave of the text-heavy full-page tests (`FullFeatureDocx`, `EmployeeOnboardingGuide`, `ConvertFullHtmlPage`, `RichDocument`, `NestedCombinations`, `PageBreaks`, `BorderStyleVariants`, `BookmarksAndLinksDocx`, `CombinedCellStyles`) with byte-identical docx output. Re-accept the pngs. Expect it on the next bump.
- **PNG moved and docx moved** → read the docx diff before assuming the render is wrong. Morph is not Word: it treats `w:tblHeader` as affecting layout and renders a marked header row slightly taller, though the flag is pure pagination and Word lays out a single page identically with or without it. Deterministic, so a changed png with a one-element docx diff can still be correct.

Always diff the docx xml first — it is the thing this library produces.

## Architecture

Two code paths exist for Word output:

**Flat segment path** (`ToParagraphs`): HTML → AngleSharp DOM → `List<TextSegment>` → `Paragraph`/`Run` elements. Simple, no `MainDocumentPart` required, but no tables, headings styles, numbering, or style mapping.

**DOM-based path** (`ToElements`/`AppendHtml`/`ConvertToDocx`): HTML → AngleSharp DOM → `WordContentBuilder.Build` → `List<OpenXmlElement>`. Full-featured: tables, heading styles, real list numbering, CSS class→style mapping, paragraph spacing, images, footnotes, bookmarks.

**The two paths must agree on text handling, and each tracks its state differently** — this is the most common source of subtle bugs here. Whitespace folding is the worst offender: the segment path carries fold state through the already-emitted `segments` list (`segments[^1]`), while the DOM path carries it on `WordBuildContext.LastWasSpace`. A rule added to one and not the other produces output that differs between `ToParagraphs` and `ToElements` for identical html, which no single test catches. When changing either, add the same case to both — `WordEdgeCaseTests` pairs them deliberately.

**`SpreadsheetHtmlConverter` rides the segment path**, so a segment-path change moves xlsx output too. Dropping the space after a `<br>` re-baselined three spreadsheet snapshots that had nothing to do with Word.

### Design decisions worth not undoing

- **`FormatState.Bold`, `Italic`, `SmallCaps` and `Shadow` are `bool?`, and the "off" value is emitted unconditionally.** `null` means unspecified (inherit); `false` means explicitly off and reaches Word as `<w:b w:val="false"/>` and friends. The off cannot be suppressed when no enclosing element set the property, because a run also inherits from its **paragraph style**, which the html parser never sees — that is the `<h3>` case, where `font-weight: normal` renders unbolded only because the override is explicit. The cost is an explicit off in the standalone case (a bare `text-shadow: none` carries one), which is correct rather than noise.
- **`Strikethrough` stays a plain `bool` on purpose.** CSS `text-decoration` propagates to descendants and cannot be cancelled by a descendant's `text-decoration: none`, so absent-means-off is the right model. Do not "make it consistent" with the four above.
- **Percentage widths are supported where OOXML has a percentage form, and nowhere else.** `w:tcW` and `w:tblW` take `w:type="pct"` (fiftieths of a percent, so 35% is 1750). `w:gridCol` has no percentage unit and an inline image's `wp:extent` is an absolute EMU extent, so a percentage on `<col>` or `<img>` is dropped — the tests named `...IgnoredBecause...` say which and why.
- **An absolute table width is shared across the columns and switched to `tblLayout` fixed; a percentage table width deliberately is not.** Word's autofit treats `tblW` as a preference and resizes columns to content, which is why an absolute width needed the share-and-fix. A percentage has nothing to share out (`gridCol` takes no percentage), and a percentage-width table with auto columns is what a browser renders anyway.
- **`OnDiagnostic` reports only the deliberate drop sites, never every unrecognised declaration.** The signal worth having is "understood, and could not be expressed" — a `%` on `w:gridCol`, an `ImagePolicy` refusal, an `<iframe>`. Reporting unknown css too would bury that under the `display`/`cursor`/`float` an ordinary stylesheet carries, and would cost `NoneForFullySupportedMarkup` its meaning: a conversion that reports nothing has to mean nothing was lost. Both `<script>`-style metadata and author-hidden elements (`hidden`, `display: none`) stay silent — a browser draws nothing for them either, so nothing was dropped. Every report goes through `Diagnostic`, which allocates nothing while the sink is null.
- **Diagnostics live in the shared helpers so both code paths report identically.** `ImageResolver.Resolve`, `ImageResolver.ParseImageDimensions` and `IsHiddenElement` are called by the segment path and the DOM path alike, so reporting there covers both without duplication — the same rule as the whitespace folding above, and `WordDiagnosticsTests.BothPathsAgree` asserts it. `ImageData.SourceTag` exists only for this: the flat segment list erases whether an image came from `<img>` or `<svg>`, and a diagnostic has to name the tag the author actually wrote.

### Key internal classes

- `HtmlSegmentParser` — Parses HTML via AngleSharp into flat `TextSegment(string Text, FormatState Format)` list. Used by both `ToParagraphs` (flat path) and `SpreadsheetHtmlConverter`.
- `WordContentBuilder` — DOM-based Word builder. Walks the AngleSharp DOM tree via `ProcessElement`/`ProcessChildren`, accumulates runs in `WordBuildContext`, flushes paragraphs with full styling (heading styles, list numbering, CSS class styles, paragraph spacing).
- `ImageResolver` — Resolves `<img>` sources: `data:` URIs (always allowed), HTTP/HTTPS URLs (checked against `HtmlConvertSettings.WebImages` policy), local files (checked against `LocalImages` policy). Returns `ImageData` or null (alt text fallback).
- `WordNumberingBuilder` — Creates `NumberingDefinitionsPart` with bullet/decimal abstract numbering definitions. Each `<ul>`/`<ol>` gets its own numbering instance; nested lists increment `ilvl`.
- `WordStyleLookup` — Reads `StyleDefinitionsPart` to build a map from CSS class names to Word paragraph/character style IDs (case-insensitive).
- `ParagraphFormatState` — Holds paragraph-level CSS properties (margins, text-indent, line-height, text-align) parsed from inline `style` attributes, applied at paragraph flush time.

### Public API classes

- `WordHtmlConverter` (public) — All Word conversion entry points. `ToParagraphs` uses flat segments; everything else delegates to `WordContentBuilder`. All methods have overloads accepting `HtmlConvertSettings`.
- `SpreadsheetHtmlConverter` (public) — Converts segments to `InlineString` with spreadsheet `Run` elements for xlsx cells.
- `ImagePolicy` (public) — Controls which image sources are allowed: `Deny()`, `AllowAll()`, `SafeDomains(...)`, `SafeDirectories(...)`, `Filter(predicate)`.
- `HtmlConvertSettings` (public) — Settings for image resolution: `WebImages`/`LocalImages` policies, optional `HttpClient`.
- `ColorParser`, `StyleParser` (internal) — Parse CSS colors (hex/named/rgb), inline style attributes, and CSS lengths (pt/px/em/in/cm/mm → twips).

## Test Organization

Tests are organized by feature area. Each supported HTML element and CSS property should have a dedicated test. When adding a new feature, add a corresponding test in the appropriate file.

### Test file → feature mapping

| Test File | Covers |
|---|---|
| `WordBasicTests` | `b`, `strong`, `i`, `em`, `u`, `ins`, `s`, `strike`, `del`, `sub`, `sup`, `br`, `wbr`, HTML entities |
| `WordBlockTests` | `p`, `div`, `h1`–`h6`, `blockquote`, `pre`, `hr`, `ul`/`ol`/`li` (text prefix path), page breaks |
| `WordHeadingTests` | `h1`–`h6` heading styles |
| `WordColorAndFontTests` | `color`, `font-size`, `font-family`, `font` attributes, `small`, `code`/`kbd`/`samp`, named/hex/rgb colors |
| `WordMiscElementTests` | `abbr`, `acronym`, `time`, `q`, `figure`/`figcaption`, `svg`, `article`, `section`, `nav`, `main`, `header`, `footer`, `aside`, `dfn`, `cite`, `var`, `details`/`summary`, `address`, `dl`/`dt`/`dd` |
| `WordTableTests` | `table`, `tr`, `td`, `th`, `colspan`, `rowspan`, `thead`/`tbody`/`tfoot`, `caption`, nested tables |
| `WordTableStyleTests` | Cell `padding`/`width`/`background-color`/`vertical-align`, table `width`/`background-color`/`padding`, `cellpadding`/`bgcolor`/`width` HTML attributes |
| `WordColgroupTests` | `<colgroup>`, `<col>`, `col width=` / `col style="width"`, `col span=`, column width propagation to `tblGrid` and cells |
| `WordImageSizingTests` | CSS `width`/`height` on `<img>` (px, pt, em, in), precedence over HTML `width=`/`height=` attributes, percentage fallthrough |
| `WordImageFloatTests` | CSS `float: left`/`float: right` on `<img>` and `<svg>` → anchored drawing with text wrap |
| `WordAnchorTests` | `a` (hyperlinks, internal `#id` links), `id` attribute bookmarks |
| `WordNestedTests` | Deeply nested formatting combinations |
| `WordEdgeCaseTests` | Whitespace collapsing, malformed HTML, unclosed tags, unknown tags, image alt fallback |
| `WordParagraphSpacingTests` | `margin`, `text-indent`, `text-align`, `line-height` |
| `WordBackgroundColorTests` | `background-color` on runs/paragraphs, `background` shorthand, `<mark>` element |
| `WordUnderlineTests` | `text-decoration-style` variants (dotted, dashed, wavy, double), `<u>`/`<ins>` tags, spreadsheet underline |
| `WordBorderTests` | `border` on runs/paragraphs/cells/tables, per-side borders, `border: none`, `border="0"`, all style variants |
| `WordStyleMappingTests` | CSS `class` → Word paragraph/character style mapping |
| `WordListNumberingTests` | Real Word numbering (`NumberingDefinitionsPart`), nested lists, separate list restart, fallback |
| `WordListStyleTests` | `type` attribute (a/A/i/I), `start` attribute, `list-style-type` CSS, mixed list types |
| `WordListStylePositionTests` | `list-style-position: inside`/`outside` |
| `WordReversedListTests` | `<ol reversed>`, reversed with `start`, reversed fallback without MainPart |
| `WordSmallCapsTests` | `font-variant: small-caps` CSS |
| `WordTextTransformTests` | `text-transform`: uppercase, lowercase, capitalize (both code paths) |
| `WordTextShadowTests` | `text-shadow` CSS → `<w:shadow/>` run property |
| `WordLetterSpacingTests` | `letter-spacing` CSS → `<w:spacing>` character spacing |
| `WordWhiteSpaceTests` | `white-space`: pre/pre-wrap/break-spaces (preserve) and nowrap (spaces → nbsp) |
| `WordClickableImageTests` | Images inside `<a>` tags, external link hyperlinks, fallback without MainPart |
| `WordWritingModeTests` | `writing-mode` (vertical-rl, vertical-lr), `direction: rtl`, vertical text in cells |
| `WordDirectionAttributeTests` | HTML `dir="rtl"`/`dir="ltr"` attribute on body, block, inline, and table elements |
| `WordRowHeightTests` | `height` CSS and HTML attribute on `<tr>` |
| `WordPageSizeTests` | Explicit `pgSz` (A4) emitted so rendering is locale-independent |
| `WordPageLayoutTests` | `@page` CSS rule for size, margin, orientation, and column-count |
| `WordPageBreakTests` | `page-break-before/after` and modern `break-before/after` |
| `WordFontWeightTests` | Numeric `font-weight` (100–900), `bolder` |
| `WordTextDecorationColorTests` | `text-decoration-color` for colored underlines |
| `WordWordSpacingTests` | `word-spacing` CSS |
| `WordFontStretchTests` | `font-stretch` keywords and percentages → `<w:w>` character scale |
| `WordVisibilityTests` | `display: none`, `visibility: hidden`, HTML `hidden`, skipping `script`/`style`/`noscript` |
| `WordCheckboxTests` | `<input type="checkbox" checked>` rendered as ☑/☐ |
| `WordPictureTests` | `<picture>` with `<source>` siblings, falling back to `<img>` |
| `WordFieldsetTests` | `<fieldset>` with default border, `<legend>` bolded |
| `WordRubyTests` | `<ruby>`/`<rt>`/`<rp>` rendered with parens and reduced-size annotation |
| `WordBdoTests` | `<bdo dir="rtl">` emits `<w:rtl/>`, `<bdi>` falls through |
| `WordListStyleNoneTests` | `list-style-type: none` suppresses bullet/number, keeps indent |
| `WordRemoteImageTests` | `ImagePolicy` (Deny/AllowAll/SafeDomains/Filter/SafeDirectories), `FakeImageHandler` |
| `WordDiagnosticsTests` | `HtmlConvertSettings.OnDiagnostic` — every deliberate drop site, and the markup that must stay silent |
| `WordConvertToDocxTests` | Full docx output: images, SVG, footnotes, page breaks, CSS styles, lists, tables |
| `WordHeaderFooterTests` | `SetHeader`/`SetFooter` with plain and formatted HTML, tables in headers |
| `WordConvertFileTests` | `ConvertFileToDocx` file I/O |
| `WordIntegrationTests` | `AppendHtml`, `ToParagraphs` rich document scenarios |
| `WordStyleComboTests` | Single large docx exercising all features together |
| `ImagePolicyTests` | `ImagePolicy` unit tests (Deny/AllowAll/SafeDomains/SafeDirectories/Filter) |
| `StyleParserTests` | CSS parsing, `ParseFontSize`, `ParseLengthToTwips` |
| `ColorParserTests` | Hex/named (148 CSS colors)/RGB/RGBA color parsing |
| `Spreadsheet*Tests` | Mirror of Word tests for spreadsheet-supported features |

### Test requirements

**Every new feature and bug fix must have a dedicated test.** Do not rely on combo tests or incidental coverage. The test should be named after the specific feature or bug (e.g., `SmallCapsTag`, `BorderShorthand`, `NestedListNumberingRestart`).

1. Find the appropriate test file from the table above (or create a new `Word<Feature>Tests.cs`)
2. Add a test method named after the feature
3. For features requiring `MainDocumentPart` (styles, numbering, images), use `ConvertToDocx` or `AppendHtml` with a `MainDocumentPart`
4. For simple formatting, `ToParagraphs` is sufficient
5. Run test → copy `.received.*` to `.verified.*` → run again to confirm
6. Update the test file mapping table above if you create a new test file

## Key Conventions

- **Multi-target**: Library targets `net48;net10.0`. Tests target `net10.0;net48`. Uses [Polyfill](https://github.com/SimonCropp/Polyfill) + `System.Memory` for span support on net48.
- **Namespace conflicts**: `DocumentFormat.OpenXml.Spreadsheet` and `DocumentFormat.OpenXml.Wordprocessing` share type names (`Run`, `Bold`, `Color`, etc.). Resolved via global using aliases in `GlobalUsings.cs` (e.g., `SpreadsheetRun`, `SpreadsheetBold`).
- **Code style**: EditorConfig enforces `var` everywhere, expression bodies on methods/properties/constructors, braces always required, file-scoped namespaces, no access modifiers on types. `TreatWarningsAsErrors` is on.
- **Strong naming**: Uses `key.snk` via ProjectDefaults. `InternalsVisibleTo` in `AssemblyInfo.cs` requires full public key.
- **Central package management**: All versions in `src/Directory.Packages.props`.
- **LangVersion preview**: C# preview features are available (raw string literals, collection expressions, etc.).
