using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.PerformanceTesting;

namespace FerryKit.Core.Tests.Performance
{
    [TestFixture, Category("Performance")]
    public class CorePerformanceTests
    {
        [Flags] private enum Options : byte { None = 0, A = 1, B = 2 }
        private enum SmallEnum
        {
            Value00, Value01, Value02, Value03, Value04, Value05, Value06, Value07,
        }
        private enum ThresholdEnum
        {
            Value00, Value01, Value02, Value03, Value04, Value05, Value06, Value07,
            Value08, Value09, Value10, Value11, Value12, Value13, Value14, Value15,
        }
        private enum LargeEnum
        {
            Value00, Value01, Value02, Value03, Value04, Value05, Value06, Value07,
            Value08, Value09, Value10, Value11, Value12, Value13, Value14, Value15,
            Value16, Value17, Value18, Value19, Value20, Value21, Value22, Value23,
            Value24, Value25, Value26, Value27, Value28, Value29, Value30, Value31,
            Value32, Value33, Value34, Value35, Value36, Value37, Value38, Value39,
            Value40, Value41, Value42, Value43, Value44, Value45, Value46, Value47,
            Value48, Value49, Value50, Value51, Value52, Value53, Value54, Value55,
            Value56, Value57, Value58, Value59, Value60, Value61, Value62, Value63,
        }
        private enum HugeEnum
        {
            Value00, Value01, Value02, Value03, Value04, Value05, Value06, Value07,
            Value08, Value09, Value10, Value11, Value12, Value13, Value14, Value15,
            Value16, Value17, Value18, Value19, Value20, Value21, Value22, Value23,
            Value24, Value25, Value26, Value27, Value28, Value29, Value30, Value31,
            Value32, Value33, Value34, Value35, Value36, Value37, Value38, Value39,
            Value40, Value41, Value42, Value43, Value44, Value45, Value46, Value47,
            Value48, Value49, Value50, Value51, Value52, Value53, Value54, Value55,
            Value56, Value57, Value58, Value59, Value60, Value61, Value62, Value63,
            Value64,
        }
        private static readonly string[] _thresholdEnumNames = Enum.GetNames(typeof(ThresholdEnum));
        private static readonly ThresholdEnum[] _thresholdEnumValues = (ThresholdEnum[])Enum.GetValues(typeof(ThresholdEnum));
        private static readonly string[] _largeEnumNames = Enum.GetNames(typeof(LargeEnum));
        private static readonly LargeEnum[] _largeEnumValues = (LargeEnum[])Enum.GetValues(typeof(LargeEnum));
        private static readonly HashSet<LargeEnum> _largeEnumSet = new(_largeEnumValues);
        private static int _sink;

        [Test, Explicit("Run explicitly when collecting performance measurements.")]
        public void SpanSearchAndEnumParsing_MeasureHotPathAndVerifyNoAllocationWhenSupported()
        {
            ReadOnlyMemory<char> csv = "alpha,\"bravo,charlie\",delta".AsMemory();
            ReadOnlyMemory<char> option = "B".AsMemory();
            Action search = () => _sink = csv.Span.IndexOfUnquoted(',', QuoteEscapeMode.Csv);
            Action parse = () => { option.Span.TryToEnum(out Options value); _sink = (int)value; };

            Measurement searchResult = PerformanceTestUtility.Measure("IndexOfUnquoted CSV", search);
            Measurement parseResult = PerformanceTestUtility.Measure("TryToEnum Span", parse);
            PerformanceTestUtility.AssertNoAllocationIfSupported(searchResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(parseResult);
        }

        [Test, Explicit("Run explicitly when tuning enum lookup thresholds.")]
        public void EnumLookup_MeasuresLinearIndexedHitMissAndMembershipPaths()
        {
            ReadOnlyMemory<char> smallName = "Value07".AsMemory();
            ReadOnlyMemory<char> thresholdName = "Value15".AsMemory();
            ReadOnlyMemory<char> largeName = "Value63".AsMemory();
            ReadOnlyMemory<char> largeNameIgnoreCase = "VALUE63".AsMemory();
            ReadOnlyMemory<char> missingName = "Missing".AsMemory();
            ReadOnlyMemory<char> smallNumericValue = "7".AsMemory();
            ReadOnlyMemory<char> thresholdNumericValue = "15".AsMemory();
            ReadOnlyMemory<char> numericValue = "63".AsMemory();
            ReadOnlyMemory<char> hugeNumericValue = "64".AsMemory();

            Action smallHit = () => { smallName.Span.TryParse(out SmallEnum value); _sink = (int)value; };
            Action thresholdHit = () => { thresholdName.Span.TryParse(out ThresholdEnum value); _sink = (int)value; };
            Action thresholdLinearHit = () => { TryParseThresholdEnumLinear(thresholdName.Span, out ThresholdEnum value); _sink = (int)value; };
            Action largeHit = () => { largeName.Span.TryParse(out LargeEnum value); _sink = (int)value; };
            Action largeIgnoreCaseHit = () => { largeNameIgnoreCase.Span.TryParse(out LargeEnum value, ignoreCase: true); _sink = (int)value; };
            Action largeMiss = () => { bool success = missingName.Span.TryParse(out LargeEnum value); _sink = success ? (int)value : -1; };
            Action largeLinearHit = () => { TryParseLargeEnumLinear(largeName.Span, out LargeEnum value); _sink = (int)value; };
            Action largeLinearMiss = () => { bool success = TryParseLargeEnumLinear(missingName.Span, out LargeEnum value); _sink = success ? (int)value : -1; };
            Action smallNumericHit = () => { smallNumericValue.Span.TryParse(out SmallEnum value); _sink = (int)value; };
            Action thresholdNumericHit = () => { thresholdNumericValue.Span.TryParse(out ThresholdEnum value); _sink = (int)value; };
            Action largeNumericHit = () => { numericValue.Span.TryParse(out LargeEnum value); _sink = (int)value; };
            Action hugeNumericHit = () => { hugeNumericValue.Span.TryParse(out HugeEnum value); _sink = (int)value; };
            Action smallDefinedHit = () => _sink = SmallEnum.Value07.IsDefined() ? 1 : 0;
            Action thresholdDefinedHit = () => _sink = ThresholdEnum.Value15.IsDefined() ? 1 : 0;
            Action largeDefinedHit = () => _sink = LargeEnum.Value63.IsDefined() ? 1 : 0;
            Action largeDefinedMiss = () => _sink = ((LargeEnum)64).IsDefined() ? 1 : 0;
            Action hugeDefinedHit = () => _sink = HugeEnum.Value64.IsDefined() ? 1 : 0;
            Action largeHashSetHit = () => _sink = _largeEnumSet.Contains(LargeEnum.Value63) ? 1 : 0;
            Action largeHashSetMiss = () => _sink = _largeEnumSet.Contains((LargeEnum)64) ? 1 : 0;

            Measurement smallResult = PerformanceTestUtility.Measure("Enum name linear 8 hit-last", smallHit);
            Measurement thresholdResult = PerformanceTestUtility.Measure("Enum name indexed 16 hit-last", thresholdHit);
            Measurement thresholdLinearResult = PerformanceTestUtility.Measure("Enum name linear 16 hit-last baseline", thresholdLinearHit);
            Measurement largeResult = PerformanceTestUtility.Measure("Enum name indexed 64 hit-last", largeHit);
            Measurement largeIgnoreCaseResult = PerformanceTestUtility.Measure("Enum name indexed 64 ignore-case", largeIgnoreCaseHit);
            Measurement largeMissResult = PerformanceTestUtility.Measure("Enum name indexed 64 miss", largeMiss);
            Measurement largeLinearResult = PerformanceTestUtility.Measure("Enum name linear 64 hit-last baseline", largeLinearHit);
            Measurement largeLinearMissResult = PerformanceTestUtility.Measure("Enum name linear 64 miss baseline", largeLinearMiss);
            Measurement smallNumericResult = PerformanceTestUtility.Measure("Enum value linear 8 numeric hit", smallNumericHit);
            Measurement thresholdNumericResult = PerformanceTestUtility.Measure("Enum value binary 16 numeric hit", thresholdNumericHit);
            Measurement largeNumericResult = PerformanceTestUtility.Measure("Enum value HashSet 64 numeric hit", largeNumericHit);
            Measurement hugeNumericResult = PerformanceTestUtility.Measure("Enum value HashSet 65 numeric hit", hugeNumericHit);
            Measurement smallDefinedResult = PerformanceTestUtility.Measure("Enum value linear 8 defined hit", smallDefinedHit);
            Measurement thresholdDefinedResult = PerformanceTestUtility.Measure("Enum value binary 16 defined hit", thresholdDefinedHit);
            Measurement largeDefinedResult = PerformanceTestUtility.Measure("Enum value HashSet 64 defined hit", largeDefinedHit);
            Measurement largeUndefinedResult = PerformanceTestUtility.Measure("Enum value HashSet 64 defined miss", largeDefinedMiss);
            Measurement hugeDefinedResult = PerformanceTestUtility.Measure("Enum value HashSet 65 defined hit", hugeDefinedHit);
            Measurement largeHashSetResult = PerformanceTestUtility.Measure("Enum HashSet 64 defined hit baseline", largeHashSetHit);
            Measurement largeHashSetMissResult = PerformanceTestUtility.Measure("Enum HashSet 64 defined miss baseline", largeHashSetMiss);

            PerformanceTestUtility.AssertNoAllocationIfSupported(smallResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(thresholdResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(thresholdLinearResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(largeResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(largeIgnoreCaseResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(largeMissResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(largeLinearResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(largeLinearMissResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(smallNumericResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(thresholdNumericResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(largeNumericResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(hugeNumericResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(smallDefinedResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(thresholdDefinedResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(largeDefinedResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(largeUndefinedResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(hugeDefinedResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(largeHashSetResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(largeHashSetMissResult);
        }

        [Test, Performance, Explicit("Run explicitly when comparing EnumHelper with System.Enum.")]
        public void EnumHelperAndSystemEnum_ComparePerformanceAndAllocation()
        {
            const string largeName = "Value63";
            const string largeNumeric = "63";

            Assert.That(Enum.IsDefined(typeof(SmallEnum), SmallEnum.Value07), Is.True);
            Assert.That(SmallEnum.Value07.IsDefined(), Is.True);
            Assert.That(Enum.IsDefined(typeof(LargeEnum), LargeEnum.Value63), Is.True);
            Assert.That(LargeEnum.Value63.IsDefined(), Is.True);
            Assert.That(((LargeEnum[])Enum.GetValues(typeof(LargeEnum))).Length, Is.EqualTo(EnumHelper.GetValues<LargeEnum>().Length));
            Assert.That((LargeEnum)Enum.Parse(typeof(LargeEnum), largeName), Is.EqualTo(LargeEnum.Value63));
            Assert.That(largeName.Parse<LargeEnum>(), Is.EqualTo(LargeEnum.Value63));
            Assert.That((LargeEnum)Enum.Parse(typeof(LargeEnum), largeNumeric), Is.EqualTo(LargeEnum.Value63));
            Assert.That(largeNumeric.Parse<LargeEnum>(), Is.EqualTo(LargeEnum.Value63));
            Assert.That(Enum.TryParse(largeName, out LargeEnum systemNameValue), Is.True);
            Assert.That(largeName.TryParse(out LargeEnum helperNameValue), Is.True);
            Assert.That(systemNameValue, Is.EqualTo(helperNameValue));
            Assert.That(Enum.TryParse(largeNumeric, out LargeEnum systemNumericValue), Is.True);
            Assert.That(largeNumeric.TryParse(out LargeEnum helperNumericValue), Is.True);
            Assert.That(systemNumericValue, Is.EqualTo(helperNumericValue));

            Action systemIsDefinedSmall = () => _sink = Enum.IsDefined(typeof(SmallEnum), SmallEnum.Value07) ? 1 : 0;
            Action helperIsDefinedSmall = () => _sink = SmallEnum.Value07.IsDefined() ? 1 : 0;
            Action systemIsDefinedLarge = () => _sink = Enum.IsDefined(typeof(LargeEnum), LargeEnum.Value63) ? 1 : 0;
            Action helperIsDefinedLarge = () => _sink = LargeEnum.Value63.IsDefined() ? 1 : 0;
            Action systemGetValues = () => _sink = ((LargeEnum[])Enum.GetValues(typeof(LargeEnum))).Length;
            Action helperGetValues = () => _sink = EnumHelper.GetValues<LargeEnum>().Length;
            Action systemParseName = () => _sink = (int)(LargeEnum)Enum.Parse(typeof(LargeEnum), largeName);
            Action helperParseName = () => _sink = (int)largeName.Parse<LargeEnum>();
            Action systemParseNumeric = () => _sink = (int)(LargeEnum)Enum.Parse(typeof(LargeEnum), largeNumeric);
            Action helperParseNumeric = () => _sink = (int)largeNumeric.Parse<LargeEnum>();
            Action systemTryParseName = () =>
            {
                bool success = Enum.TryParse(largeName, out LargeEnum value);
                _sink = success ? (int)value : -1;
            };
            Action helperTryParseName = () =>
            {
                bool success = largeName.TryParse(out LargeEnum value);
                _sink = success ? (int)value : -1;
            };
            Action systemTryParseNumeric = () =>
            {
                bool success = Enum.TryParse(largeNumeric, out LargeEnum value);
                _sink = success ? (int)value : -1;
            };
            Action helperTryParseNumeric = () =>
            {
                bool success = largeNumeric.TryParse(out LargeEnum value);
                _sink = success ? (int)value : -1;
            };

            PerformanceTestUtility.Measure("System Enum.IsDefined 8 hit", systemIsDefinedSmall);
            Measurement helperIsDefinedSmallResult = PerformanceTestUtility.Measure("EnumHelper.IsDefined linear 8 hit", helperIsDefinedSmall);
            PerformanceTestUtility.Measure("System Enum.IsDefined 64 hit", systemIsDefinedLarge);
            Measurement helperIsDefinedLargeResult = PerformanceTestUtility.Measure("EnumHelper.IsDefined HashSet 64 hit", helperIsDefinedLarge);
            PerformanceTestUtility.Measure("System Enum.GetValues 64", systemGetValues);
            Measurement helperGetValuesResult = PerformanceTestUtility.Measure("EnumHelper.GetValues 64", helperGetValues);
            PerformanceTestUtility.Measure("System Enum.Parse name 64 hit", systemParseName);
            Measurement helperParseNameResult = PerformanceTestUtility.Measure("EnumHelper.Parse name 64 hit", helperParseName);
            PerformanceTestUtility.Measure("System Enum.Parse numeric 64 hit", systemParseNumeric);
            Measurement helperParseNumericResult = PerformanceTestUtility.Measure("EnumHelper.Parse numeric 64 hit", helperParseNumeric);
            PerformanceTestUtility.Measure("System Enum.TryParse name 64 hit", systemTryParseName);
            Measurement helperTryParseNameResult = PerformanceTestUtility.Measure("EnumHelper.TryParse name 64 hit", helperTryParseName);
            PerformanceTestUtility.Measure("System Enum.TryParse numeric 64 hit", systemTryParseNumeric);
            Measurement helperTryParseNumericResult = PerformanceTestUtility.Measure("EnumHelper.TryParse numeric 64 hit", helperTryParseNumeric);

            PerformanceTestUtility.AssertNoAllocationIfSupported(helperIsDefinedSmallResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(helperIsDefinedLargeResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(helperGetValuesResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(helperParseNameResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(helperParseNumericResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(helperTryParseNameResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(helperTryParseNumericResult);

            MeasureGcAllocationCalls("System Enum.IsDefined 8 hit", systemIsDefinedSmall);
            MeasureGcAllocationCalls("EnumHelper.IsDefined linear 8 hit", helperIsDefinedSmall);
            MeasureGcAllocationCalls("System Enum.IsDefined 64 hit", systemIsDefinedLarge);
            MeasureGcAllocationCalls("EnumHelper.IsDefined HashSet 64 hit", helperIsDefinedLarge);
            MeasureGcAllocationCalls("System Enum.GetValues 64", systemGetValues);
            MeasureGcAllocationCalls("EnumHelper.GetValues 64", helperGetValues);
            MeasureGcAllocationCalls("System Enum.Parse name 64 hit", systemParseName);
            MeasureGcAllocationCalls("EnumHelper.Parse name 64 hit", helperParseName);
            MeasureGcAllocationCalls("System Enum.Parse numeric 64 hit", systemParseNumeric);
            MeasureGcAllocationCalls("EnumHelper.Parse numeric 64 hit", helperParseNumeric);
            MeasureGcAllocationCalls("System Enum.TryParse name 64 hit", systemTryParseName);
            MeasureGcAllocationCalls("EnumHelper.TryParse name 64 hit", helperTryParseName);
            MeasureGcAllocationCalls("System Enum.TryParse numeric 64 hit", systemTryParseNumeric);
            MeasureGcAllocationCalls("EnumHelper.TryParse numeric 64 hit", helperTryParseNumeric);
        }

        private static void MeasureGcAllocationCalls(string name, Action operation)
        {
            const int measurementCount = 5;
            const int iterationsPerMeasurement = 1_000;
            string sampleGroupName = $"{name} profiler";
            Measure.Method(operation)
                .SampleGroup(sampleGroupName)
                .WarmupCount(100)
                .MeasurementCount(measurementCount)
                .IterationsPerMeasurement(iterationsPerMeasurement)
                .GC()
                .Run();

            var samples = PerformanceTest.GetSampleGroup($"{sampleGroupName}.GC()").Samples;
            var sorted = samples.ToArray();
            Array.Sort(sorted);
            double median = sorted[sorted.Length >> 1];
            TestContext.WriteLine(
                $"{name}: {median:F4} GC.Alloc calls/op " +
                $"({measurementCount} measurements x {iterationsPerMeasurement:N0} iterations)");
        }

        private static bool TryParseThresholdEnumLinear(ReadOnlySpan<char> span, out ThresholdEnum result)
        {
            for (int i = 0; i < _thresholdEnumNames.Length; ++i)
            {
                if (_thresholdEnumNames[i].Length == span.Length && span.Equals(_thresholdEnumNames[i], StringComparison.Ordinal))
                {
                    result = _thresholdEnumValues[i];
                    return true;
                }
            }
            result = default;
            return false;
        }

        private static bool TryParseLargeEnumLinear(ReadOnlySpan<char> span, out LargeEnum result)
        {
            for (int i = 0; i < _largeEnumNames.Length; ++i)
            {
                if (_largeEnumNames[i].Length == span.Length && span.Equals(_largeEnumNames[i], StringComparison.Ordinal))
                {
                    result = _largeEnumValues[i];
                    return true;
                }
            }
            result = default;
            return false;
        }

        [Test, Explicit("Run explicitly when collecting performance measurements.")]
        public void ArraySearchAndIdPools_MeasureSearchAndReuse()
        {
            var values = new int[1024];
            values[^1] = 1;
            Action arraySearch = () => _sink = ArrayHelper.FindIndex(values, static x => x == 1);

            var bitPool = new BitArrayIdPool<int, IntOp>(0, 1024);
            Action bitPoolCycle = () => { int id = bitPool.NextId(); bitPool.ReleaseId(id); _sink = id; };
            var recyclablePool = new RecyclableIdPool<int, IntOp>(0, 1024);
            Action recyclableCycle = () => { int id = recyclablePool.NextId(); recyclablePool.ReleaseId(id); _sink = id; };

            Measurement arrayResult = PerformanceTestUtility.Measure("ArrayHelper.FindIndex 1024", arraySearch);
            PerformanceTestUtility.Measure("BitArrayIdPool acquire/release", bitPoolCycle);
            PerformanceTestUtility.Measure("RecyclableIdPool acquire/release", recyclableCycle);
            PerformanceTestUtility.AssertNoAllocationIfSupported(arrayResult);
        }

        [Test, Explicit("Run explicitly when collecting performance measurements.")]
        public void TextParser_MeasuresPrimitiveCompositeAndTableParsing()
        {
            ReadOnlyMemory<char> primitive = "123456".AsMemory();
            ReadOnlyMemory<char> composite = "1;2;3;4;5;6;7;8".AsMemory();
            const string table = "id,name\n1,one\n2,two\n3,three\n4,four";
            string largeTable = CreateTable(4096);
            Action parsePrimitive = () => _sink = primitive.Span.ToInt();
            Action parseComposite = () => _sink = composite.Span.ToArray<int>().Length;
            Action parseTable = () => _sink = TextParser.TryParse(table, out List<PerformanceRow> rows, out _, skipFirstLine: true) ? rows.Count : -1;
            Action parseLargeTable = () => _sink = TextParser.TryParse(largeTable, out List<PerformanceRow> rows, out _, skipFirstLine: true) ? rows.Count : -1;

            Measurement primitiveResult = PerformanceTestUtility.Measure("Parse int Span", parsePrimitive);
            PerformanceTestUtility.Measure("Parse int[8]", parseComposite, iterations: 20_000);
            PerformanceTestUtility.Measure("Parse 4-row table", parseTable, iterations: 10_000, warmup: 1_000);
            PerformanceTestUtility.Measure("Parse 4096-row table", parseLargeTable, iterations: 100, warmup: 10);
            PerformanceTestUtility.AssertNoAllocationIfSupported(primitiveResult);
        }

        [Test, Explicit("Run explicitly when collecting performance measurements.")]
        public void AllocatingStringAndCollectionHelpers_ReportAllocationCost()
        {
            const string text = "HTTPServerResponseName";
            Action camelCase = () => _sink = text.ToCamelCase().Length;
            Action richText = () => _sink = FerryKit.Core.StringHelper.Color("message", 12, 34, 56).Length;
            var list = new List<int>(128);
            Action assign = () => { list.Assign(128, 7); _sink = list.Count; };

            PerformanceTestUtility.Measure("ToCamelCase", camelCase);
            PerformanceTestUtility.Measure("RGB rich-text formatting", richText);
            PerformanceTestUtility.Measure("List.Assign preallocated", assign);
        }

        public sealed class PerformanceRow : ITryParsable
        {
            public int Id;
            public string Name;
            public bool TryParse(ref LineReader reader)
                => reader.TryRead(out Id) && reader.TryRead(out Name);
        }

        private static string CreateTable(int rowCount)
        {
            var builder = new StringBuilder("id,name");
            for (int i = 0; i < rowCount; ++i)
                builder.Append('\n').Append(i).Append(",name");

            return builder.ToString();
        }
    }
}
