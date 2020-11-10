using System;
using System.Collections.Generic;
using System.Text;
using static SpeCLI.ParameterHelper;

namespace SpeCLI
{
    public class Switch : IParameter
    {
        public string Prefix { get; set; }

        public int Priority { get; set; }

        public string Name { get; set; }

        public Switch()
        {
        }

        public Switch(string Name, int Priority = 0)
        {
            this.Priority = Priority;
            Prefix = GetPrefix(ref Name) ?? (Name.Length > 1 ? "--" : "-");
            this.Name = Name;
        }

        public string GetValue(bool Value)
        {
            if (!Value) { return null; }
            return $"{Prefix}{Name}";
        }

        public Switch WithName(string Name)
        {
            this.Name = Name;
            return this;
        }

        public Switch WithPriority(int Priority)
        {
            this.Priority = Priority;
            return this;
        }

        public Switch WithPrefix(string Prefix)
        {
            this.Prefix = Prefix;
            return this;
        }

        public string GetObjectValue(object Value)
        {
            return GetValue(Value is bool t ? t : false);
        }
    }
}
