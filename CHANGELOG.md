# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-07-18

### Added

- Added `FixedArray<T>` for fixed-size, allocation-conscious value storage.
- Added editor and runtime test coverage for core utilities, parsing, ID pools, and Unity integration.
- Added default-policy wrappers for parsing and fixed-array utilities.

### Changed

- Optimized `TextParser` and improved its fallback parsing behavior.
- Made numeric and date parsing culture-invariant, AOT-safe, and reduced reflection overhead through caching.
- Refined `SingletonBase` and separated editor-only code into its own assembly.

### Fixed

- Fixed `BitArrayIdPool` capacity range validation.
- Fixed enum parsing behavior and compatibility issues in `TextParser`.

[0.1.0]: https://github.com/dlakek/ferry-kit/releases/tag/v0.1.0
