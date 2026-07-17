using System;
using System.Diagnostics;
using NUnit.Framework;

namespace FerryKit.Core.Tests.Performance
{
    internal readonly struct Measurement
    {
        public readonly double NanosecondsPerOperation;
        public readonly double BytesPerOperation;

        public Measurement(double nanosecondsPerOperation, double bytesPerOperation)
        {
            NanosecondsPerOperation = nanosecondsPerOperation;
            BytesPerOperation = bytesPerOperation;
        }
    }

    internal static class PerformanceTestUtility
    {
        public static readonly bool AllocationMeasurementSupported = DetectAllocationMeasurementSupport();

        public static Measurement Measure(string name, Action operation, int iterations = 100_000, int warmup = 5_000)
        {
            for (int i = 0; i < warmup; ++i)
                operation();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var stopwatch = new Stopwatch();
            long allocatedBefore = AllocationMeasurementSupported ? GC.GetAllocatedBytesForCurrentThread() : 0;
            stopwatch.Start();
            for (int i = 0; i < iterations; ++i)
                operation();
            stopwatch.Stop();
            long allocated = AllocationMeasurementSupported ? GC.GetAllocatedBytesForCurrentThread() - allocatedBefore : 0;

            var result = new Measurement(
                stopwatch.Elapsed.TotalMilliseconds * 1_000_000d / iterations,
                AllocationMeasurementSupported ? (double)allocated / iterations : double.NaN);
            string allocationText = AllocationMeasurementSupported ? $"{result.BytesPerOperation:F4} bytes/op" : "allocation n/a";
            TestContext.WriteLine($"{name}: {result.NanosecondsPerOperation:F2} ns/op, {allocationText} ({iterations:N0} iterations)");
            return result;
        }

        public static void AssertNoAllocationIfSupported(Measurement measurement)
        {
            if (AllocationMeasurementSupported)
                Assert.That(measurement.BytesPerOperation, Is.Zero);
        }

        private static bool DetectAllocationMeasurementSupport()
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            var probe = new byte[1024];
            long after = GC.GetAllocatedBytesForCurrentThread();
            GC.KeepAlive(probe);
            return after > before;
        }
    }
}
