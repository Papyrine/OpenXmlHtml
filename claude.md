# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Build & Test

All commands run from `src/`.

```bash
dotnet build OpenXmlHtml.slnx
dotnet test OpenXmlHtml.slnx                        # both net10.0 and net48
dotnet test OpenXmlHtml.slnx --framework net10.0
dotnet test OpenXmlHtml.slnx --filter "TestName"
```

- Local `Release` builds need `-p:IsPackable=false`, or SponsorCheck fails with `SC100: Platform fetch failed. GitHub GraphQL HTTP 401`. It only bundles on CI.
- Building the test project runs MarkdownSnippets, which rewrites `readme.md` from the `#region` snippets in `src/OpenXmlHtml.Tests/Samples/`.

## Verify Snapshot Testing

Tests use [Verify](https://github.com/VerifyTests/Verify) with TUnit.

- **Accept with `dotnet verify accept -y` from the repository root.** Received and verified files do not share a stem: `Foo.DotNet10_0.received.xml` becomes `Foo.DotNet.verified.xml`, and `Foo.Net4_8.received.xml` becomes `Foo.Net.verified.xml`. Copying the stem verbatim writes a second snapshot corpus no test reads. By hand, take the destination from the `Verified:` line of the failure.
- A new snapshot test needs **both** baselines. A missing `.Net.verified.xml` fails only on the net48 run.
- Output of ten lines or fewer is pinned inline with `.Snapshot("""...""")` rather than in a file. When output shrinks under that, Verify asks for the files to be deleted and the snapshot moved into the test.
- Tests producing docx binaries or floating-point output use `.UniqueForTargetFrameworkAndVersion()`.
- `VerifyOpenXmlConverter.cs` serialises OpenXml objects to `.OuterXml`.

**Reading the png snapshots.** A `.verified.png` is Morph's render of the docx, tied to the `Morph.Skia` version in `src/Directory.Packages.props`. Always diff the docx xml first; it is what this library produces.

- **PNG moved, docx identical**: the rasteriser changed. Every `Morph.Skia` bump has done this to the text-heavy full-page tests. Re-accept the pngs.
- **PNG moved and docx moved**: read the docx diff before judging the render. Morph is not Word: it renders a `w:tblHeader` row slightly taller, though Word lays the page out identically.

## Architecture

Two code paths produce Word output:

- **Flat segment path** (`ToParagraphs`): HTML → AngleSharp DOM → `List<TextSegment>` → paragraphs and runs. Needs no `MainDocumentPart`; has no tables, heading styles, numbering or style mapping. **`SpreadsheetHtmlConverter` rides this path**, so a change here moves xlsx output too.
- **DOM path** (`ToElements`/`AppendHtml`/`ConvertToDocx`): HTML → AngleSharp DOM → `WordContentBuilder.Build` → `List<OpenXmlElement>`. Full-featured.

**The two paths must agree on text handling, and each tracks its state differently.** This is the most common source of subtle bugs here. The segment path carries whitespace fold state in the already-emitted `segments` list; the DOM path carries it on `WordBuildContext.LastWasSpace`. A rule added to one and not the other makes `ToParagraphs` and `ToElements` differ for the same html, which no single-path test catches. Add the same case to both; `WordEdgeCaseTests` pairs them deliberately.

### Design decisions worth not undoing

**Formatting**

- **`FormatState.Bold`, `Italic`, `SmallCaps` and `Shadow` are `bool?`, and `false` is always emitted** (`<w:b w:val="false"/>`). A run also inherits from its paragraph style, which the parser never sees, so `font-weight: normal` in an `<h3>` only works because the off is explicit.
- **`Strikethrough` stays a plain `bool`.** A css decoration propagates to descendants and a descendant's `text-decoration: none` cannot cancel it. That value does cancel the line the element's own tag draws (`<a>`, `<u>`, `<s>`).
- **A tag's default formatting is set outright, not with `??=`.** `<code>` is monospace inside a paragraph that names a font, a link is blue inside coloured text, `<mark>` is yellow inside a background. The element's own `style` is applied afterwards and still wins. The underline on `<u>`, `<ins>` and `<a>` keeps `??=` to preserve an enclosing dotted or wavy style. Only an `<a>` with an `href` is styled as a link.
- **`<font size>` is html's 1 to 7 scale, and a value above 7 is points.** The second half is this library's own reading (`size="14"`), kept because callers rely on it.
- **`FormatState` is passed as `in`.** It is 152 bytes; by value each nesting level cost several copies of it in stack.

**Whitespace and blocks**

- **Whitespace at the edge of a block is not content.** A browser draws no space at the start or end of a line, so `<p>⏎  Hello⏎</p>` is "Hello". Both paths treat the start of a paragraph as if a space had just been written, and both trim the one folded space a paragraph can end in. Otherwise whitespace between an inline element and a block becomes a paragraph holding one space. Three kinds of space are content and stay: preformatted (`TextSegment.Preformatted`), `white-space: nowrap`, and a text list marker's own (`IsListMarker`, `WordBuildContext.ListMarker`).
- **What an enclosing block owes its paragraphs lives on the context as an "ambient" value, restored on the way out.** Entering a child block flushes first, which clears per-paragraph state before any text arrives. `AmbientParagraphStyleId` carries a class-mapped style; `AmbientParagraphFormat` carries alignment, line height, indent, background, border and side margins. Top and bottom margins are the block's own. A table cell's `text-align` reaches its paragraphs the same way.
- **A block's border is its paragraph's (`pBdr`) and a cell's is its `tcBorders`; neither goes on the runs.** `ApplyInlineStyle` reads `border` into `FormatState` for any element, so block and cell handling reset it. Left alone it drew a second box around the text.
- **A block bookmark waits in `PendingBookmarks` for a paragraph to sit in.** Written straight into the pending runs, a container's markers were flushed out as two empty paragraphs.
- **Both walks check the stack before descending (`HasStackToRecurse`) and flatten an over-deep subtree to its text.** html has no nesting limit and a stack overflow cannot be caught.

**Schema order**

- **A `pPr` child goes in through `AddParagraphProperty`, never `Append`.** `CT_PPrBase` fixes the order of its children and css arrives in a different one. Word repairs a wrong order silently, so a snapshot will not catch it. Anything new belongs in `paragraphPropertyOrder`. `sectPr` is the same: `EnsureHeaderFooterReference` inserts rather than appends.
- **`tblPr` and `tcPr` children go through `NormaliseProperties`**, which orders them and drops a repeat. css is read before the attribute it replaced, so where both speak (`bgcolor` and `background-color`, `cellpadding` and `padding`) css is kept.

**Tables and lists**

- **Percentage widths are supported only where OOXML has a percentage form.** `w:tcW` and `w:tblW` take `w:type="pct"` (fiftieths of a percent). `w:gridCol` and an image's `wp:extent` do not, so a percentage on `<col>` or `<img>` is dropped; the tests named `...IgnoredBecause...` say why.
- **An absolute table width is shared across the columns with `tblLayout` fixed; a percentage width is not.** Word's autofit treats `tblW` as a preference. A percentage has nothing to share out.
- **Table spans are clamped and the grid is counted by where cells start.** `colspan` and `col span` stop at 1000 and `rowspan` at 65534, html's limits; a table stops at 63 columns, Word's. `colspan="100"` for "all of them" does not make a hundred columns. Two tables in a row get an empty paragraph between them, because Word joins adjacent tables.
- **Ordered lists of one number format share an abstract definition, and every `<ol>` carries a start override on its own instance**, at the level the list sits at. The override is what makes each list restart.
- **A text list marker is written by `HtmlSegmentParser.OrderedMarker`, for both paths.** Where a list has no real numbering (a cell, `ToParagraphs`, no `MainDocumentPart`, any reversed list) the marker is text and follows `start`, `reversed`, `type` and `list-style-type`. One source keeps the paths from drifting. Punctuation matches the real numbering: "1." for decimal, "a)" otherwise.

**Parts, relationships and ids**

- **Relationships are registered on the part that holds the xml, `WordBuildContext.RelationshipPart`.** An `r:embed` or `r:id` resolves against the part it sits in, so a header's image and link belong on the header part. Styles, numbering and footnotes stay on `MainPart`; a header or footer writes no footnote reference.
- **Every document-unique id carries on from what is already there.** Several calls against one `MainDocumentPart` is a documented way to build a document. Footnote and numbering ids are read from their parts, image indexes from the highest `rImage{n}` (`LastImageIndex`), bookmark ids from the document's bookmarks, lazily (`SeedBookmarkId`), since that read is the whole document. `HtmlNumberingSession` carries the next numbering id and the highest bookmark id between calls. Calls that neither share a session nor add their elements before the next call can still repeat a bookmark id.
- **Parts and external links get fixed relationship ids from `PartRelationshipId.Next`** (`rNumbering`, `rStyles`, `rLink`, `rFootnotes`, `rHeader`, `rFooter`, `rImage{n}`). The package is still not byte-identical between runs, because zip entries record when they were written. Compare parts, not bytes.
- **An image used more than once is stored once (`StoredImage`), and repeats share the first use's relationship id.** The SDK allows one relationship from an owner to a part. A repeat's `wp:docPr` id comes from a range above `0x40000000`, because the next call seeds from the highest `rImage{n}` and a repeat adds none.
- **An image's missing dimension comes from the image (`ImageSize`).** With one dimension given, the other follows the natural proportions; with none, it is the natural size, read from the png, gif, bmp or jpeg header. Two departures from a browser: an unsized image is scaled down to 602 pixels wide, the text width of A4; and where the size cannot be read the fallback is 100. Svg uses its own attributes or `viewBox`.

**Diagnostics**

- **`OnDiagnostic` reports only deliberate drops, never every unrecognised declaration.** The signal is "understood, and could not be expressed". A conversion that reports nothing has to mean nothing was lost. Metadata tags and author-hidden elements stay silent. `Diagnostic` allocates nothing while the sink is null.
- **Diagnostics live in the shared helpers** (`ImageResolver.Resolve`, `ParseImageDimensions`, `IsHiddenElement`) so both paths report identically; `WordDiagnosticsTests.BothPathsAgree` asserts it. `ImageData.SourceTag` exists so a report can name the tag the author wrote.

### Key classes

Public: `WordHtmlConverter` (all Word entry points), `SpreadsheetHtmlConverter`, `HtmlConvertSettings`, `ImagePolicy`, `HtmlNumberingSession`, `WordStyles`, `WordNumbering`.

Internal:

- `HtmlSegmentParser`: the segment path, and the helpers both paths share.
- `WordContentBuilder`: the DOM path. Walks the tree, accumulates runs in `WordBuildContext`, flushes paragraphs.
- `ImageResolver`: resolves `<img>` sources. `data:` URIs are always allowed; web and local sources are checked against `HtmlConvertSettings`. Returns null for the alt-text fallback.
- `ImageSize`, `StoredImage`: image dimensions and reuse, as above.
- `WordNumberingBuilder`: the numbering part and its definitions.
- `WordStyleLookup`: maps css class names to Word style ids, case-insensitively.
- `PartRelationshipId`: deterministic relationship ids.
- `LinkTarget`: whether an `href` is an absolute url. `Uri.TryCreate` alone accepts `//host/page` on Windows and `/path` on Unix as file paths.
- `ParagraphFormatState`: paragraph-level css, applied at flush.
- `ColorParser`, `StyleParser`, `AtPageParser`: css colours, declarations and lengths, and the `@page` rule.

## Test Organization

**Every new feature and bug fix needs a dedicated test**, named after it (`SmallCapsTag`, `NestedListNumberingRestart`). Do not rely on combo tests or incidental coverage.

- Put it in the file for its feature, or create `Word<Feature>Tests.cs`. Files are named for what they cover: `WordTableTests`, `WordBorderTests`, `WordListStyleTests`, `WordPageBreakTests` and so on, with `Spreadsheet*Tests` mirroring them.
- Features needing a `MainDocumentPart` (styles, numbering, images) use `ConvertToDocx` or `AppendHtml`. Simple formatting can use `ToParagraphs`.
- Where both paths are involved, run the case through both and compare.

Files whose scope is not obvious from the name:

| Test file | Covers |
|---|---|
| `WordEdgeCaseTests` | Whitespace folding, malformed html, deep nesting, alt-text fallback, the paired both-paths cases |
| `WordAnchorTests` | Links, bookmarks from `id` and named anchors, bookmark ids across calls, relative `href`s |
| `WordIntegrationTests` | `AppendHtml` and `ToParagraphs` scenarios, images across several calls, repeated images |
| `WordValidationTests` | Schema validity of generated documents: property order, out-of-range lengths, unique drawing ids, reproducible relationship ids |
| `SchemaValidationTests` | Every checked-in `.verified.docx` and `.verified.xlsx`, swept against the schema |
| `WordDiagnosticsTests` | Every deliberate drop site, and the markup that must stay silent |
| `WordImageSizingTests` | css and attribute sizes, a missing dimension taken from the image, data uri decoding |
| `WordImageFallbackTests`, `WordRemoteImageTests` | Images that cannot be loaded; `ImagePolicy` |
| `WordListNumberingTests`, `WordListNumberingSessionTests` | Real numbering, shared definitions, the session |
| `WordListStyleTests`, `WordReversedListTests` | `type`, `start`, `reversed`, `list-style-type`, text markers on both paths |
| `WordNumberingTests`, `WordStylesTests` | `WordNumbering.EnsureListDefinitions`, `WordStyles.EnsureStyleDefinitions` |
| `WordStyleComboTests`, `WordNestedComboTests` | Single large documents exercising many features together |

## Key Conventions

- **Multi-target**: the library targets `net48;net10.0`. [Polyfill](https://github.com/SimonCropp/Polyfill) and `System.Memory` supply span support on net48.
- **Namespace conflicts**: the Spreadsheet and Wordprocessing namespaces share type names (`Run`, `Bold`, `Color`, `Table`, `Break`). `GlobalUsings.cs` aliases the common ones (`SpreadsheetRun`, `WRun`); qualify the rest.
- **Code style**: EditorConfig enforces `var`, expression bodies, braces, file-scoped namespaces and no access modifiers on types. `TreatWarningsAsErrors` is on.
- **Strong naming**: `key.snk` via ProjectDefaults. `InternalsVisibleTo` needs the full public key.
- **Central package management**: versions live in `src/Directory.Packages.props`.

## Review history

- **2026-10-11**: performance and bug scan of the library (`src/OpenXmlHtml`).
- **2026-10-12**: what the scan found was fixed, one PR each (#54 to #99). The design decisions that came out of it are above.
