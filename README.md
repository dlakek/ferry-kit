# 🛳️ Ferry Kit
**Ferry Kit** is a collection of Unity utility scripts and common patterns designed to streamline your development journey.

> "Designed to ferry your project to completion with efficiency."

Unlike heavy frameworks, Ferry Kit aims for **performance and memory optimization** in frequently used patterns (like Singletons, ID Pooling, and Logging), while providing various **convenience features** to improve developer productivity.

## 🌟 Key Philosophy
* **Optimization-Oriented**: Focuses on minimizing GC allocations (boxing avoidance) and runtime overhead (aggressive inlining).
* **Essential Patterns**: Implements "Must-Have" patterns for game development in a clean, reusable structure.
* **Developer Convenience**: Provides handy tools to simplify repetitive tasks.

## Parsing rows and values

The default wrapper selects the built-in Parser, so common calls do not expose Parser type arguments:

```csharp
var reader = new LineReader("7,1;2;3,active".AsSpan());
reader.TryRead(out int id);
reader.TryRead(out List<int> values);
reader.TryRead(out string state);

if (reader.HasRemaining)
    reader.SkipRemaining(); // opt in when trailing columns are intentionally ignored
```

`TryReadNext` distinguishes an actual empty cell (`true` with an empty span) from a missing cell (`false`). Empty strings and collections are valid; empty scalar, enum, date, and boolean cells fail. Row parsers reject unconsumed trailing columns unless they explicitly call `SkipRemaining()`.

Custom policies keep the same convenient shape:

```csharp
var policy = new CustomPolicy(colSep: '|', arrSep: '/');
var reader = new LineReader<CustomPolicy>("7|1/2/3".AsSpan(), policy);
reader.TryRead(out int id);
reader.TryRead(out List<int> values);
```

Use `CustomPolicy.Default` when the generic policy type must be `CustomPolicy` without any overrides. Unity's C# 9 compiler zero-initializes `new CustomPolicy()` instead of invoking its optional-parameter constructor.

Nested collections and custom values use a struct Parser. Implement `IParser<T>` for the default path or `IParser<T, P>` for a policy path, read the cell explicitly, and pass it to the Parser or a span collection helper:

```csharp
if (reader.TryReadNext(out var cell))
    cell.TryToWith(out CustomValue value, parser);

if (reader.TryReadNext(out cell))
    cell.TryToListWith(out List<CustomValue> values, parser);

List<CustomValue> requiredValues = cell.ToListWith<CustomValue, CustomValueParser>(parser);
```

## Fixed arrays

Built-in unmanaged values retain the simple API:

```csharp
var values = new FixedArray4<uint>();
values.TryParse("1;2;3".AsSpan());

var reader = new LineReader("4;5;6".AsSpan());
reader.TryRead(ref values);
```

Fixed arrays use the built-in parser for supported unmanaged scalar and enum element types. To customize separators, call `TryParse(span, policy)` or use `LineReader<P>.TryRead(ref values)`. There is currently no public `IParser` overload for custom FixedArray element types. Parsing commits only after every element succeeds, so a failed parse preserves the previous count and elements.

## DataTable migration

`DataTable<T>` no longer exposes its mutable backing `DataList`. Read through `Count`, the indexer, or its concrete enumerator:

```csharp
for (int i = 0; i < table.Count; ++i)
    Consume(table[i]);

foreach (var row in table)
    Consume(row);
```

`Load` parses into a replacement list and swaps it only on success, preserving serialized data after a failed reimport.

## Validation

This repository does not include a Unity test host or automated test runner. Run validation locally from the Unity project that consumes this package.

Tests marked `Explicit` are benchmark probes; run them explicitly when collecting performance measurements.
