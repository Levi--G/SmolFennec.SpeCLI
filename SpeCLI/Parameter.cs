using System;
using System.Collections.Generic;
using System.Text;
using static SpeCLI.ParameterHelper;

namespace SpeCLI
{
    public class Parameter : IParameter
    {
        public Type Type { get; set; }
        public string ValueSeparator { get; set; }
        public string Prefix { get; set; }
        public object Default { get; set; }
        public string SpaceEncapsulation { get; set; } = "\"";
        public bool HideName { get; set; }

        public Parameter()
        {

        }

        public Parameter(string Name, Type Type, object Default = default, int Priority = 0)
        {
            this.Type = Type;
            this.Default = Default;
            this.Priority = Priority;
            Prefix = GetPrefix(ref Name) ?? (Name.Length > 1 ? "--" : "-");
            ValueSeparator = GetSeparator(ref Name) ?? " ";
            this.Name = Name;
        }

        public Parameter WithName(string Name)
        {
            this.Name = Name;
            return this;
        }

        public Parameter WithPriority(int Priority)
        {
            this.Priority = Priority;
            return this;
        }

        public Parameter WithValueSeparator(string ValueSeparator)
        {
            this.ValueSeparator = ValueSeparator;
            return this;
        }

        public Parameter WithPrefix(string Prefix)
        {
            this.Prefix = Prefix;
            return this;
        }

        public Parameter WithDefault(object Default)
        {
            this.Default = Default;
            return this;
        }

        public Parameter WithHideName(bool HideName = true)
        {
            this.HideName = HideName;
            return this;
        }

        public Parameter WithSpaceEncapsulation(string SpaceEncapsulation)
        {
            this.SpaceEncapsulation = SpaceEncapsulation;
            return this;
        }
        public int Priority { get; set; }
        public string Name { get; set; }

        public string GetObjectValue(object Value)
        {
            if (Value != null && Type.IsAssignableFrom(Value.GetType()))
            {
                var def = Type.GetDefault();
                if (Value?.Equals(def) ?? true)
                {
                    Value = Default;
                    if (Value?.Equals(def) ?? true)
                    {
                        return null;
                    }
                }
                string StringValue = $"{Value}";
                if (StringValue.Contains(" "))
                {
                    StringValue = $"{SpaceEncapsulation}{StringValue}{SpaceEncapsulation}";
                }
                if (HideName)
                {
                    return StringValue;
                }
                return $"{Prefix}{Name}{ValueSeparator}{StringValue}";
            }
            return null;
        }
    }
    public class Parameter<T> : Parameter
    {
        public Parameter()
        {
            Type = typeof(T);
        }

        public Parameter(string Name, object Default = null, int Priority = 0) : base(Name, typeof(T), Default, Priority)
        {
        }
    }
}
