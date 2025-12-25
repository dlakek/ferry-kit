using FerryKit.Core;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace FerryKit
{
    public static class StringHelper
    {
        [MethodImpl(Opt.Inline)] public static string Color(this string str, Color color) => str.Color((Color32)color);
        [MethodImpl(Opt.Inline)] public static string Color(this string str, Color32 color) => str.Color(color.r, color.g, color.b);
    }

    public static class StringBuilderHelper
    {
        [MethodImpl(Opt.Inline)]
        public static StringBuilder AppendColor(this StringBuilder sb, string text, Color color)
            => sb.AppendColor(text, (Color32)color);

        [MethodImpl(Opt.Inline)]
        public static StringBuilder AppendColor(this StringBuilder sb, string text, Color32 color)
            => sb.AppendColor(text, color.r, color.g, color.b);
    }
}
