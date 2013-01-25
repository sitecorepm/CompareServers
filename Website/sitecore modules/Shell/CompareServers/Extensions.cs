using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data.Fields;

namespace Sitecore.SharedSource.CompareServers
{
    public static class Extensions
    {
        public static string JoinToString(this IEnumerable<string> values)
        {
            return values.JoinToString(string.Empty);
        }
        public static string JoinToString(this IEnumerable<string> values, string separator)
        {
            return string.Join(separator, values.ToArray());
        }
        public static bool IsDefaultValue(this Field field)
        {
            if (field.Definition == null)
                return false;
            return field.Definition.DefaultValue == field.Value;
        }
    }
}