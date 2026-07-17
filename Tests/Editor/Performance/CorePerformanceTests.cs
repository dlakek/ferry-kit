using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace FerryKit.Core.Tests.Performance
{
    [TestFixture, Category("Performance")]
    public class CorePerformanceTests
    {
        [Flags] private enum Options : byte { None = 0, A = 1, B = 2 }
        private static int _sink;

        [Test, Explicit("Run explicitly when collecting performance measurements.")]
        public void SpanSearchAndEnumParsing_MeasureHotPathAndVerifyNoAllocation()
        {
            ReadOnlyMemory<char> csv = "alpha,\"bravo,charlie\",delta".AsMemory();
            ReadOnlyMemory<char> option = "B".AsMemory();
            Action search = () => _sink = csv.Span.IndexOfUnquoted(',', FerryKit.Core.StringHelper.QuoteEscapeMode.Csv);
            Action parse = () => { option.Span.TryToEnum(out Options value); _sink = (int)value; };

            Measurement searchResult = PerformanceTestUtility.Measure("IndexOfUnquoted CSV", search);
            Measurement parseResult = PerformanceTestUtility.Measure("TryToEnum Span", parse);
            PerformanceTestUtility.AssertNoAllocationIfSupported(searchResult);
            PerformanceTestUtility.AssertNoAllocationIfSupported(parseResult);
        }

        [Test, Explicit("Run explicitly when collecting performance measurements.")]
        public void ArrayAndCollectionHelpers_MeasureSearchIterationAndPoolReuse()
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
            Action parsePrimitive = () => _sink = primitive.Span.ToInt();
            Action parseComposite = () => _sink = composite.Span.ToArray<int>().Length;
            Action parseTable = () => _sink = TextParser.TryParse<PerformanceRow>(table, out List<PerformanceRow> rows, out _, isSkipFirstLine: true) ? rows.Count : -1;

            Measurement primitiveResult = PerformanceTestUtility.Measure("Parse int Span", parsePrimitive);
            PerformanceTestUtility.Measure("Parse int[8]", parseComposite, iterations: 20_000);
            PerformanceTestUtility.Measure("Parse 4-row table", parseTable, iterations: 10_000, warmup: 1_000);
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
            public bool TryParse<P>(ref LineReader<P> reader) where P : struct, IParsePolicy
                => reader.TryRead(out Id) && reader.TryRead(out Name);
        }
    }
}
