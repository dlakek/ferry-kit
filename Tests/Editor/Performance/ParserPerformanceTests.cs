using FerryKit.Core;
using NUnit.Framework;
using System;
using Unity.PerformanceTesting;

namespace FerryKit.Tests.Performance
{
    [TestFixture, Category("Performance")]
    public class ParserPerformanceTests
    {
        [Flags] private enum Options : byte { None = 0, A = 1, B = 2 }
        private static int _sink;

        [Test, Performance]
        public void ScalarEnumAndLineReader_ReportNoAllocationWhenSupported()
        {
            const string row = "123,B,value";
            Measure.Method(() => ParseRow(row))
                .WarmupCount(20)
                .MeasurementCount(20)
                .IterationsPerMeasurement(1_000)
                .GC()
                .Run();
            var allocation = Core.Tests.Performance.PerformanceTestUtility.Measure("LineReader scalar/enum GC", () => ParseRow(row));
            Core.Tests.Performance.PerformanceTestUtility.AssertNoAllocationIfSupported(allocation);
        }

        [Test, Performance]
        public void FixedArrayDestination_ReportsNoAllocationWhenSupported()
        {
            const string values = "1;2;3;4";
            Measure.Method(() => ParseFixed(values))
                .WarmupCount(20)
                .MeasurementCount(20)
                .IterationsPerMeasurement(1_000)
                .GC()
                .Run();
            var allocation = Core.Tests.Performance.PerformanceTestUtility.Measure("FixedArray destination GC", () => ParseFixed(values));
            Core.Tests.Performance.PerformanceTestUtility.AssertNoAllocationIfSupported(allocation);
        }

        [Test, Performance]
        public void DefaultAndExplicitParser_ReportComparableThroughput()
        {
            const string value = "123456";
            Measure.Method(() => value.AsSpan().TryTo(out _sink))
                .WarmupCount(20).MeasurementCount(20).IterationsPerMeasurement(10_000).GC().Run();
            Measure.Method(() => new Parser<int>().TryParse(value.AsSpan(), out _sink))
                .WarmupCount(20).MeasurementCount(20).IterationsPerMeasurement(10_000).GC().Run();
        }

        [Test, Performance]
        public void CollectionSuccessPath_ReportsTimeAndAllocation()
        {
            const string values = "1;2;3;4;5;6;7;8";
            Measure.Method(() => _sink = values.AsSpan().ToArray<int>().Length)
                .WarmupCount(10).MeasurementCount(20).IterationsPerMeasurement(1_000).GC().Run();
            Measure.Method(() => _sink = values.AsSpan().ToList<int>().Count)
                .WarmupCount(10).MeasurementCount(20).IterationsPerMeasurement(1_000).GC().Run();
        }

        private static void ParseRow(string row)
        {
            var reader = new LineReader(row.AsSpan());
            if (!reader.TryRead(out int number) || !reader.TryRead(out Options option) ||
                !reader.TryReadNext(out ReadOnlySpan<char> value))
                throw new InvalidOperationException();
            _sink = number + (int)option + value.Length;
        }

        private static void ParseFixed(string values)
        {
            var fixedArray = new FixedArray4<int>();
            if (!fixedArray.TryParse(values.AsSpan()))
                throw new InvalidOperationException();

            _sink = fixedArray.Count;
        }
    }
}
