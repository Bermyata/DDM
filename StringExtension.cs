using System;
using System.Collections.Generic;
using System.Linq;

namespace DrawingDataManager
{
    internal static class StringExtension
    {
        public static Boolean ContainsOrdinalIgnoreCase( this String source, String toCheck )
        {
            return source?.IndexOf( toCheck, StringComparison.OrdinalIgnoreCase ) >= 0;
        }

        public static Boolean ContainsOrdinalIgnoreCase( this IEnumerable<String> source, String toCheck )
        {
            return source?.Any( x => x.EqualsOrdinalIgnoreCase( toCheck ) ) ?? false;
        }

        public static Boolean EqualsOrdinalIgnoreCase( this String source, String toCheck )
        {
            return source?.Equals( toCheck, StringComparison.OrdinalIgnoreCase ) ?? false;
        }
    }
}