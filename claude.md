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

Tests use [Verify](https://github.com/VerifyTests/Verify) with TUnit. When test output changes:
- `.received.*` files appear next to `.verified.*` files
- **Received and verified are not named the same stem**, so accepting is a rename. Received carries a target-framework tag; verified is named for the runtime:
  - `Foo.DotNet10_0.received.xml` → `Foo.DotNet.verified.xml`
  - `Foo.Net4_8.received.xml` → `Foo.Net.verified.xml`

  Copying the stem verbatim writes a second, parallel snapshot corpus no test ever reads, leaving the real baseline stale and the suite still failing. So map the name, do not copy it.

  When accepting by hand, take the destination from the `Verified:` line of the failure message rather than deriving it from the received name. `dotnet verify accept` (Verify.Tool 0.8.0 and later) reads the pairing that Verify records under `obj`, so it maps these names correctly and is safe to use from the repository root. Run it from a directory above `obj`, since that is where the records live.
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

- **A `pPr` child goes in through `AddParagraphProperty`, never `Append`.** `CT_PPrBase` fixes the order of its children and css arrives in a different one: the background is read after the alignment and the border after both, so appending produced `spacing, ind, jc, shd, bidi, textDirection, pBdr` where the schema wants `pBdr, shd, bidi, spacing, ind, jc, textDirection`. Word repairs a document that gets this wrong, which is why it went unseen until `SchemaValidationTests` swept the snapshots — and why a snapshot alone will not catch the next one, since it only covers the css combinations some test happened to write. Anything new emitted into a `pPr` belongs in `paragraphPropertyOrder`, and `WordValidationTests.ParagraphPropertiesFollowSchemaOrder` pins the sequence. `sectPr` has the same shape: the header and footer references open its sequence, ahead of the page setup, so `EnsureHeaderFooterReference` inserts rather than appends.

- **Whatever an enclosing block owes the paragraphs inside it lives on the context as an "ambient" value, restored on the way out.** Entering a child block flushes first, and that flush clears per-paragraph state before any text has arrived. `AmbientParagraphStyleId` carries a class-mapped style across that flush and `AmbientParagraphFormat` carries alignment, line height, indent, background, border and the side margins: `<div style="text-align:center"><p>x</p></div>` lost the alignment without it. Top and bottom margins are the block's own and are not handed down. A table cell's `text-align` reaches its paragraphs the same way, since Word aligns paragraphs, not cells.
- **A block's border is its paragraph's (`pBdr`) and a cell's is its `tcBorders`; neither goes on the runs.** `ApplyInlineStyle` reads `border` into `FormatState` for any element, so the block and cell handling put the run border back to what was inherited. Left alone it drew a second box around the text inside the first.
- **A tag's default formatting is set outright, not with `??=`.** `<code>` is monospace inside a paragraph that names a font, a link is blue inside coloured text, `<mark>` is yellow inside a background: these are the browser's rules for the element and outrank an inherited value. The element's own `style` is applied afterwards and still wins. The underline on `<u>`, `<ins>` and `<a>` keeps `??=` on purpose, to preserve an enclosing dotted or wavy style. Only an `<a>` with an `href` is styled as a link.
- **Relationships are registered on the part that will hold the xml, `WordBuildContext.RelationshipPart`, not always on the main part.** An `r:embed` or `r:id` is resolved against the part it sits in, so a header's image and link have to be on the header part. Styles, numbering and footnotes stay document-level on `MainPart`, and a header or footer writes no footnote reference because it cannot hold one.
- **Every id that has to be unique in the document carries on from what is already there.** Several calls against one `MainDocumentPart` is a documented way to build a document. Footnote ids and numbering ids are read from their parts, image indexes from the highest `rImage{n}` relationship (`LastImageIndex`). A counter that restarted at 1 is how the second call's picture came to show the first call's image. `HtmlNumberingSession` remembers the next numbering id between calls so it is not recounted each time, and rechecks whenever the numbering part no longer ends where it left it.
- **Parts and external links get fixed relationship ids from `PartRelationshipId.Next`** (`rNumbering`, `rStyles`, `rLink`, `rFootnotes`, `rHeader`, `rFooter`, plus `rImage{n}`), so the same html gives the same parts. The package is still not byte-identical from run to run: zip entries record when they were written. A test of reproducibility should compare parts, not bytes.
- **An image used more than once is stored once (`StoredImage`), and the repeats share the first use's relationship id.** The SDK allows only one relationship from an owner to a given part. A repeat's `wp:docPr` id comes from a range above `0x40000000` rather than from its image index, because the next call seeds from the highest `rImage{n}` and a repeat adds none.
- **Ordered lists of one number format share an abstract definition, and every `<ol>` carries a start override on its own instance.** The override is what makes each list restart; without it instances sharing a definition would carry on from one another. It names the level the list sits at, not level 0.
- **`tblPr` and `tcPr` children go through `NormaliseProperties`, which puts them in schema order and drops a repeat.** Same reason as `AddParagraphProperty` below. css is read before the attribute it replaced and the sort is stable, so where both speak (`bgcolor` and `background-color`, `cellpadding` and `padding`) the css one is kept.
- **Table spans are clamped and the grid is counted by where cells start.** `colspan` and `col span` stop at 1000 and `rowspan` at 65534, html's own limits, and a table at 63 columns, Word's. A column exists where a cell starts, not everywhere a span reaches, so `colspan="100"` for "all of them" does not make a hundred columns. Two tables in a row get an empty paragraph between them, because Word joins adjacent tables into one.
- **Both walks check the stack before descending (`HasStackToRecurse`) and flatten an over-deep subtree to its text.** html has no nesting limit and a stack overflow cannot be caught. `FormatState` is 152 bytes and is passed as `in` for the same reason: by value, each level cost several copies of it in stack.
- **`<font size>` is html's 1 to 7 scale, and a value above 7 is points.** The second half is this library's own long-standing reading (`size="14"`), kept because callers rely on it.
- **A block bookmark waits in `PendingBookmarks` for a paragraph to sit in.** A block that only holds other blocks has no paragraph of its own; written straight into the pending runs its markers were flushed out as two empty paragraphs.

### Key internal classes

- `HtmlSegmentParser` — Parses HTML via AngleSharp into flat `TextSegment(string Text, FormatState Format)` list. Used by both `ToParagraphs` (flat path) and `SpreadsheetHtmlConverter`.
- `WordContentBuilder` — DOM-based Word builder. Walks the AngleSharp DOM tree via `ProcessElement`/`ProcessChildren`, accumulates runs in `WordBuildContext`, flushes paragraphs with full styling (heading styles, list numbering, CSS class styles, paragraph spacing).
- `ImageResolver` — Resolves `<img>` sources: `data:` URIs (always allowed), HTTP/HTTPS URLs (checked against `HtmlConvertSettings.WebImages` policy), local files (checked against `LocalImages` policy). Returns `ImageData` or null (alt text fallback).
- `WordNumberingBuilder` — Creates `NumberingDefinitionsPart` with bullet/decimal abstract numbering definitions. Each `<ul>`/`<ol>` gets its own numbering instance; nested lists increment `ilvl`.
- `WordStyleLookup` — Reads `StyleDefinitionsPart` to build a map from CSS class names to Word paragraph/character style IDs (case-insensitive).
- `PartRelationshipId` — Mints deterministic relationship ids for parts and external links.
- `LinkTarget` — Decides whether an `href` is an absolute url. `Uri.TryCreate` alone accepts `//host/page` on Windows and `/path` on Unix as file paths, so a leading slash is ruled out first.
- `StoredImage` — An image part already written in this conversion, so a repeat reuses it.
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
| `WordAnchorTests` | `a` (hyperlinks, internal `#id` links), `id` attribute bookmarks on blocks, inline elements and named anchors, relative `href`s |
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
| `WordIntegrationTests` | `AppendHtml`, `ToParagraphs` rich document scenarios, images across several calls, repeated images stored once |
| `WordValidationTests` | Schema validity of generated documents: property order in `pPr`/`tblPr`/`tcPr`, out-of-range css lengths, unique drawing ids, reproducible relationship ids |
| `WordFootnoteIdTests` | Footnote ids unique across table cells and across calls |
| `WordBookmarkIdTests` | Bookmark ids unique across table cells |
| `WordImageFallbackTests` | Alt-text fallback when an image cannot be decoded, downloaded or read |
| `WordListBulletGlyphTests` | Bullet glyph fonts per level, list items with block children |
| `WordListNumberingSessionTests` | `HtmlNumberingSession` sharing a bullet definition across calls |
| `WordNumberingTests` | `WordNumbering.EnsureListDefinitions` |
| `WordStylesTests` | `WordStyles.EnsureStyleDefinitions` |
| `WordRowspanTests` | Rowspans that would otherwise drop trailing cells |
| `WordNestedComboTests` | Single large docx of nested feature combinations |
| `WordStyleComboTests` | Single large docx exercising all features together |
| `ImagePolicyTests` | `ImagePolicy` unit tests (Deny/AllowAll/SafeDomains/SafeDirectories/Filter) |
| `StyleParserTests` | CSS parsing, `ParseFontSize`, `ParseLengthToTwips` |
| `ColorParserTests` | Hex/named (148 CSS colors)/RGB/RGBA color parsing |
| `Spreadsheet*Tests` | Mirror of Word tests for spreadsheet-supported features |
| `SchemaValidationTests` | Every checked-in `.verified.docx`/`.verified.xlsx`, swept against the OOXML schema |

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

## Review history

- **2026-10-11**: performance and bug scan of the library (`src/OpenXmlHtml`).
- **2026-10-12**: the bugs and performance problems that scan found were fixed, one PR each (#54 to #94). The design decisions above that came out of it are written up there.
