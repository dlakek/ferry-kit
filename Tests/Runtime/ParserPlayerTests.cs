using System;
using FerryKit.Core;
using NUnit.Framework;

namespace FerryKit.Tests
{
    public class ParserPlayerTests
    {
        [Test]
        public void BuiltinWrappers_WorkWithoutReflectionRegistry()
        {
            var reader = new LineReader("1,2;3,A".AsSpan());
            Assert.That(reader.TryRead(out int number), Is.True);
            Assert.That(reader.TryRead(out int[] values), Is.True);
            Assert.That(reader.TryRead(out SampleEnum option), Is.True);
            Assert.That(number, Is.EqualTo(1));
            Assert.That(values, Is.EqualTo(new[] { 2, 3 }));
            Assert.That(option, Is.EqualTo(SampleEnum.A));
            Assert.That(reader.HasRemaining, Is.False);
        }

        [Test]
        public void FixedArray_FailurePreservesPreviousValue()
        {
            var values = new FixedArray2<uint>();
            Assert.That(values.TryParse("1;2".AsSpan()), Is.True);
            Assert.That(values.TryParse("3;bad".AsSpan()), Is.False);
            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values[0], Is.EqualTo(1u));
            Assert.That(values[1], Is.EqualTo(2u));
        }

        private enum SampleEnum : byte { A }
    }
}
