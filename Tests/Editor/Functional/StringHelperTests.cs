using System;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using CoreString = FerryKit.Core.StringHelper;
using CoreBuilder = FerryKit.Core.StringBuilderHelper;

namespace FerryKit.Core.Tests.Functional
{
    public class StringHelperTests
    {
        [Test]
        public void RichTextHelpers_CreateExpectedTags()
        {
                Assert.That(CoreString.Bold("x"), Is.EqualTo("<b>x</b>"));
                Assert.That(CoreString.Italic("x"), Is.EqualTo("<i>x</i>"));
                Assert.That(CoreString.Strike("x"), Is.EqualTo("<s>x</s>"));
                Assert.That(CoreString.Underline("x"), Is.EqualTo("<u>x</u>"));
                Assert.That(CoreString.Sub("x"), Is.EqualTo("<sub>x</sub>"));
                Assert.That(CoreString.Sup("x"), Is.EqualTo("<sup>x</sup>"));
                Assert.That(CoreString.Size("x", -12), Is.EqualTo("<size=-12>x</size>"));
                Assert.That(CoreString.Link("x", "url"), Is.EqualTo("<link=url>x</link>"));
                Assert.That(CoreString.Color("x", "red"), Is.EqualTo("<color=red>x</color>"));
                Assert.That(CoreString.Color("x", 0, 127, 255), Is.EqualTo("<color=#007FFF>x</color>"));
            Assert.That(FerryKit.StringHelper.Color("x", new Color32(1, 2, 255, 9)), Is.EqualTo("<color=#0102FF>x</color>"));
        }

        [Test]
        public void SpanAndCaseHelpers_HandleBoundariesAndQuotedSeparators()
        {
                Assert.That(CoreString.TrimQuotes("\"a\"".AsSpan()).ToString(), Is.EqualTo("a"));
                Assert.That(CoreString.TrimEndOne("a;;".AsSpan(), ';').ToString(), Is.EqualTo("a;"));
                Assert.That(CoreString.TrimStartOne("..a".AsSpan(), '.').ToString(), Is.EqualTo(".a"));
                Assert.That(CoreString.IndexOfUnquoted("a,\"b,c\",d".AsSpan(), ',', CoreString.QuoteEscapeMode.Csv), Is.EqualTo(1));
                Assert.That(CoreString.IndexOfUnquoted("a,\"b,c\",d".AsSpan(), ',', 2, CoreString.QuoteEscapeMode.Csv), Is.EqualTo(7));
                Assert.That(CoreString.IndexOfUnquoted("\"a\\\"b,c\",d".AsSpan(), ',', CoreString.QuoteEscapeMode.Backslash), Is.EqualTo(8));
                Assert.That(CoreString.HasWhiteSpace((string)null), Is.False);
                Assert.That(CoreString.HasWhiteSpace("a b"), Is.True);
                Assert.That(CoreString.Truncate("abcdef", 5), Is.EqualTo("ab..."));
                Assert.That(CoreString.Truncate("abcdef".AsSpan(), 2), Is.EqualTo(".."));
                Assert.That(CoreString.ToCamelCase("URLValue"), Is.EqualTo("urlValue"));
            Assert.That(CoreString.ToPascalCase("value".AsSpan()), Is.EqualTo("Value"));
        }

        [Test]
        public void StringBuilderHelpers_AppendEveryTagShape()
        {
            var sb = new StringBuilder();
            CoreBuilder.AppendBold(sb, "b");
            CoreBuilder.AppendItalic(sb, "i");
            CoreBuilder.AppendStrike(sb, "s");
            CoreBuilder.AppendUnderline(sb, "u");
            CoreBuilder.AppendSub(sb, "sub");
            CoreBuilder.AppendSup(sb, "sup");
            CoreBuilder.AppendSize(sb, "z", 4);
            CoreBuilder.AppendLink(sb, "l", "u");
            CoreBuilder.AppendColor(sb, "c", "red");
            FerryKit.StringBuilderHelper.AppendColor(sb, "h", new Color32(10, 11, 12, 0));
            Assert.That(sb.ToString(), Is.EqualTo("<b>b</b><i>i</i><s>s</s><u>u</u><sub>sub</sub><sup>sup</sup><size=4>z</size><link=u>l</link><color=red>c</color><color=#0A0B0C>h</color>"));
        }
    }
}
