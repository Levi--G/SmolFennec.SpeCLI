using System;
using System.Collections.Generic;
using System.Text;

namespace SpeCLI
{
    public static class ParameterHelper
    {
        public static string GetPrefix(ref string Name)
        {
            if (Name.StartsWith("--"))
            {
                Name = Name.Substring(2);
                return "--";
            }
            else if (Name.StartsWith("-"))
            {
                Name = Name.Substring(1);
                return "-";
            }
            else if (Name.StartsWith("/"))
            {
                Name = Name.Substring(1);
                return "/";
            }
            return null;
        }

        public static string GetSeparator(ref string Name)
        {
            if (Name.EndsWith(" "))
            {
                Name = Name.Substring(0, Name.Length - 1);
                return " ";
            }
            else if (Name.EndsWith(":"))
            {
                Name = Name.Substring(0, Name.Length - 1);
                return ":";
            }
            else if (Name.EndsWith("="))
            {
                Name = Name.Substring(0, Name.Length - 1);
                return "=";
            }
            return null;
        }
    }
}
