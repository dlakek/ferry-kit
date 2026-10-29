# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Array searches delegate to `System.Array` for consistent null, predicate, and range validation.
- `FindAll` allocates temporary storage only after its first match, sized to the remaining input, while preserving predicate mutations to the source array.
- Bit-array ID pools track availability to detect exhaustion without scanning the entire pool.

### Fixed

- Boolean parsing, numeric-start detection, negative fixed-array and fallback indexes, size formatting, quote-search indexes, and numeric conversions preserve their contracts with checked arithmetic.
- Circular and recyclable ID pools support inclusive single-ID ranges and zero-based unsigned allocation without underflow.
- Bit-array ID releases validate the ID range before subtraction and narrowing conversions.
- Quote searches preserve the established behavior for quote and backslash search characters.
- Rich-text size values use invariant numeric syntax even with custom culture negative signs.
- Data importer settings prefer the default asset, require explicit selection for multiple alternatives, and create files relative to the project's Assets directory without overwriting an existing file or saving unrelated assets.
- Singleton lookups reject cached Unity objects destroyed while inactive, even if `OnDestroy` never cleared the cache.

## [0.2.0] - 2026-07-19

### Added

- Added strict `LineReader` cell-state APIs (`TryReadNext`, `HasRemaining`, and `SkipRemaining`) and trailing-column validation.
- Added default and policy scalar/collection parsing paths without runtime generic-method construction.
- Added scalar and collection `ToWith` / `TryToWith` entry points for default-policy and policy-aware custom parsers.
- Added explicit Parser paths for nested and custom scalar/collection values.
- Added default-policy and explicit-policy parsing and reader support for `FixedArray2` through `FixedArray5` with supported unmanaged element types.
- Added safe public numeric-start, boundary-whitespace, and quote-boundary string helpers while keeping unchecked variants internal.
- Added a Unity 6 host project, EditMode and runtime player test assemblies, Performance Testing coverage, and self-hosted Windows/Android IL2CPP validation scripts.

### Changed

- Replaced the `ParseHelper.Parser<T, P>` / `TryParser<T, P>` delegates with struct-constrained `IParser<T>` / `IParser<T, P>` contracts and the built-in `Parser` / `ParserAdapter` implementations.
- Moved `ParseHelper.Default` / `ParseHelper.Custom` to `DefaultPolicy` / `CustomPolicy`, and moved their shared constants to `ParsePolicyDefaults`.
- Replaced `IParsableWithPolicy` / `ITryParsableWithPolicy` with the policy-specific `IParsable<P>` / `ITryParsable<P>` interfaces.
- Moved `StringHelper.QuoteEscapeMode` to `QuoteEscapeMode`; use `ParseHelper.IndexOfUnquoted` with `QuoteEscapeMode.Csv` instead of `StringHelper.IndexOfUnquoted` or `StringHelper.IndexOfUnquotedCsv`.
- Removed parser-taking `LineReader` overloads; custom values now read a cell explicitly before invoking their parser.
- Arrays, lists, sets, and dictionaries now count elements first and allocate exact initial capacity.
- Renamed public named arguments from `isSkipFirstLine` to `skipFirstLine` and `isRevertWhenFail` to `revertOnFailure`; parser arguments now consistently use `span`, `result`, and `policy`.
- `DataTable<T>` imports atomically and exposes `Count`, an indexer, and a concrete list enumerator instead of its mutable backing list.
- `ListHelper.Append` uses geometric growth for positive counts and preserves the previous no-op behavior for negative counts.
- Enum caches initialize only the lookup features each enum uses.
- Default text parsing uses a bounded multi-row capacity estimate while keeping single-row allocation small.

### Fixed

- Added `CustomPolicy.Default` as the explicit fully initialized default instance because C# 9 zero-initializes `new CustomPolicy()`.
- `LineReader` empty cells are now distinct from missing cells: strings and collections accept empty cells while scalar values reject them.
- Start-index quote searches now preserve quote state before the requested index while internal parser loops retain boundary-based scanning.
- Default text parsing excludes quoted embedded newlines from its bounded row-capacity estimate.
- Fixed arrays parse into stack storage and commit only after complete success, preserving their previous value on failure.
- Fixed-array indexers remain readonly to avoid defensive copies in readonly contexts.
- Data importer settings and data-table storage initialize their serialized lists, and successful imports save only the modified destination asset.
- Unity validation scripts resolve their default project and output paths after parameter binding, remove stale result XML, and validate the NUnit result status and counters.
- Self-hosted pull-request validation skips fork branches and runs with read-only repository contents permission.
- Removed runtime `GetMethod`, `MakeGenericMethod`, and delegate construction from default parsing paths.

## [0.1.1] - 2026-07-18

### Added

- Added `LineReader.TryRead` extension overloads for `FixedArray2` through `FixedArray5`, including custom parse-policy readers.

## [0.1.0] - 2026-07-18

### Added

- Added `FixedArray2<T>` through `FixedArray5<T>` for fixed-size, allocation-conscious value storage.
- Added EditMode coverage for core utilities, parsing, ID pools, and Unity integration.
- Added default-policy wrappers for parsing and fixed-array utilities.

### Changed

- Optimized `TextParser` and improved its fallback parsing behavior.
- Made numeric and date parsing culture-invariant and reduced reflection overhead through AOT-compatible cached dispatch.
- Refined `SingletonBase` and separated editor-only code into its own assembly.

### Fixed

- Fixed `BitArrayIdPool` capacity range validation.
- Fixed enum parsing behavior and compatibility issues in `TextParser`.

[0.2.0]: https://github.com/dlakek/ferry-kit/releases/tag/v0.2.0
[0.1.1]: https://github.com/dlakek/ferry-kit/releases/tag/v0.1.1
[0.1.0]: https://github.com/dlakek/ferry-kit/releases/tag/v0.1.0
