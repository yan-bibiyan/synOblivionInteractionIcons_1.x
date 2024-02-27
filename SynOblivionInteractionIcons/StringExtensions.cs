using System;
using System.Linq;

namespace SynOblivionInteractionIcons
{
    internal static class StringExtensions
    {
        public static bool ToUpperContains(this string? source, string value)
        {
            return source != null && source.ToUpper().Contains(value.ToUpper());
        }

        public static bool ToUpperContainsAny(this string? source, params string[] stringValues)
        {
            return source != null && stringValues.Any(s => source.ToUpper().Contains(s.ToUpper()));
        }

        public static bool ToUpperEquals(this string? source, string value)
        {
            return source != null && source.ToUpper().Equals(value.ToUpper());
        }

        public static bool ToUpperEqualsAny(this string? source, params string[] stringValues)
        {
            return source != null && stringValues.Contains(source.ToUpper());
        }
    }
}
