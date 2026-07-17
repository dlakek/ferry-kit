using System;

namespace FerryKit.Core
{
    /// <summary>
    /// Marks members that UnityLinker must retain without introducing a UnityEngine dependency.
    /// Unity recognizes custom attributes named PreserveAttribute during managed code stripping.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class PreserveAttribute : Attribute { }
}
