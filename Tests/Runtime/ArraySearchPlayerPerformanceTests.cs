using System;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace FerryKit.Tests
{
    [TestFixture, Category("Performance")]
    public class ArraySearchPlayerPerformanceTests
    {
        private const int WarmupIterations = 100_000;
        private const int MeasurementIterations = 2_000_000;
        private const int SampleCount = 9;

        private static int _sink;

        [Test, Explicit("Run explicitly in an IL2CPP Player when comparing array-search implementations.")]
        public void IndexOf_Ulong16_CompareWithManualLinearSearch()
        {
            ulong[] values = CreateValues(16);

            Assert.That(Array.IndexOf(values, 15UL), Is.EqualTo(ManualIndexOf(values, 15UL)));
            Assert.That(Array.IndexOf(values, ulong.MaxValue), Is.EqualTo(ManualIndexOf(values, ulong.MaxValue)));

            SearchMeasurement hitLast = MeasurePair(values, 15UL);
            SearchMeasurement miss = MeasurePair(values, ulong.MaxValue);

            Log("ulong[16] hit-last", hitLast);
            Log("ulong[16] miss", miss);
        }

        [Test, Explicit("Run explicitly in an IL2CPP Player when comparing array-search implementations.")]
        public void BinarySearch_Ulong64_CompareWithManualBinarySearch()
        {
            ulong[] values = CreateValues(64);

            Assert.That(Array.BinarySearch(values, 63UL), Is.EqualTo(ManualBinarySearch(values, 63UL)));
            Assert.That(Array.BinarySearch(values, 65UL), Is.EqualTo(ManualBinarySearch(values, 65UL)));

            SearchMeasurement hitLast = MeasureBinaryPair(values, 63UL);
            SearchMeasurement miss = MeasureBinaryPair(values, 65UL);

            Log("ulong[64] hit-last binary", hitLast);
            Log("ulong[64] miss binary", miss);
        }

        private static SearchMeasurement MeasurePair(ulong[] values, ulong target)
        {
            RunIndexOf(values, target, WarmupIterations);
            RunManualIndexOf(values, target, WarmupIterations);

            var arraySamples = new double[SampleCount];
            var manualSamples = new double[SampleCount];
            for (int sample = 0; sample < SampleCount; ++sample)
            {
                // Alternate order so thermal or frequency drift does not consistently favor one implementation.
                if ((sample & 1) == 0)
                {
                    arraySamples[sample] = RunIndexOf(values, target, MeasurementIterations);
                    manualSamples[sample] = RunManualIndexOf(values, target, MeasurementIterations);
                }
                else
                {
                    manualSamples[sample] = RunManualIndexOf(values, target, MeasurementIterations);
                    arraySamples[sample] = RunIndexOf(values, target, MeasurementIterations);
                }
            }

            return new SearchMeasurement(Median(arraySamples), Median(manualSamples));
        }

        private static SearchMeasurement MeasureBinaryPair(ulong[] values, ulong target)
        {
            RunBinarySearch(values, target, WarmupIterations);
            RunManualBinarySearch(values, target, WarmupIterations);

            var arraySamples = new double[SampleCount];
            var manualSamples = new double[SampleCount];
            for (int sample = 0; sample < SampleCount; ++sample)
            {
                if ((sample & 1) == 0)
                {
                    arraySamples[sample] = RunBinarySearch(values, target, MeasurementIterations);
                    manualSamples[sample] = RunManualBinarySearch(values, target, MeasurementIterations);
                }
                else
                {
                    manualSamples[sample] = RunManualBinarySearch(values, target, MeasurementIterations);
                    arraySamples[sample] = RunBinarySearch(values, target, MeasurementIterations);
                }
            }

            return new SearchMeasurement(Median(arraySamples), Median(manualSamples));
        }

        private static double RunIndexOf(ulong[] values, ulong target, int iterations)
        {
            int sum = 0;
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < iterations; ++i)
                sum += Array.IndexOf(values, target);
            long elapsed = Stopwatch.GetTimestamp() - start;
            _sink = sum;
            return ToNanosecondsPerOperation(elapsed, iterations);
        }

        private static double RunManualIndexOf(ulong[] values, ulong target, int iterations)
        {
            int sum = 0;
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < iterations; ++i)
                sum += ManualIndexOf(values, target);
            long elapsed = Stopwatch.GetTimestamp() - start;
            _sink = sum;
            return ToNanosecondsPerOperation(elapsed, iterations);
        }

        private static double RunBinarySearch(ulong[] values, ulong target, int iterations)
        {
            int sum = 0;
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < iterations; ++i)
                sum += Array.BinarySearch(values, target);
            long elapsed = Stopwatch.GetTimestamp() - start;
            _sink = sum;
            return ToNanosecondsPerOperation(elapsed, iterations);
        }

        private static double RunManualBinarySearch(ulong[] values, ulong target, int iterations)
        {
            int sum = 0;
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < iterations; ++i)
                sum += ManualBinarySearch(values, target);
            long elapsed = Stopwatch.GetTimestamp() - start;
            _sink = sum;
            return ToNanosecondsPerOperation(elapsed, iterations);
        }

        private static int ManualIndexOf(ulong[] values, ulong target)
        {
            for (int i = 0; i < values.Length; ++i)
            {
                if (values[i] == target)
                    return i;
            }
            return -1;
        }

        private static int ManualBinarySearch(ulong[] values, ulong target)
        {
            int low = 0;
            int high = values.Length - 1;
            while (low <= high)
            {
                int middle = low + ((high - low) >> 1);
                ulong value = values[middle];
                if (value == target)
                    return middle;
                if (value < target)
                    low = middle + 1;
                else
                    high = middle - 1;
            }
            return ~low;
        }

        private static ulong[] CreateValues(int count)
        {
            var values = new ulong[count];
            for (int i = 0; i < count; ++i)
                values[i] = (ulong)i;
            return values;
        }

        private static double Median(double[] samples)
        {
            Array.Sort(samples);
            return samples[samples.Length / 2];
        }

        private static double ToNanosecondsPerOperation(long elapsedTicks, int iterations)
            => elapsedTicks * (1_000_000_000.0 / Stopwatch.Frequency) / iterations;

        private static void Log(string scenario, SearchMeasurement measurement)
        {
            string message =
                $"IL2CPP_SEARCH_BENCHMARK | {scenario} | Array={measurement.ArrayNanoseconds:F2} ns/op" +
                $" | Manual={measurement.ManualNanoseconds:F2} ns/op | Array/Manual={measurement.Ratio:F2}x" +
                $" | samples={SampleCount} | iterations={MeasurementIterations} | Unity={Application.unityVersion}";
            TestContext.Out.WriteLine(message);
            UnityEngine.Debug.Log(message);
        }

        private readonly struct SearchMeasurement
        {
            public readonly double ArrayNanoseconds;
            public readonly double ManualNanoseconds;
            public double Ratio => ArrayNanoseconds / ManualNanoseconds;

            public SearchMeasurement(double arrayNanoseconds, double manualNanoseconds)
            {
                ArrayNanoseconds = arrayNanoseconds;
                ManualNanoseconds = manualNanoseconds;
            }
        }
    }
}
