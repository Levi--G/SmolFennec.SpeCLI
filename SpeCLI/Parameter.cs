using SpeCLI.Extensions;
using System;
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

        public Parameter(string Name, Type Type = null, object Default = default, int Priority = 0)
        {
            this.Type = Type ?? typeof(object);
            this.Default = Default;
            this.Priority = Priority;
            Prefix = GetPrefix(ref Name) ?? (Name.Length > 1 ? "--" : "-");
            ValueSeparator = GetSeparator(ref Name) ?? " ";
            this.Name = Name;
        }

        public Parameter(Command command, string Name, Type Type = null, object Default = default, int Priority = 0)
        {
            this.Type = Type ?? typeof(object);
            this.Default = Default;
            this.Priority = Priority;
            Prefix = GetPrefix(ref Name) ?? command.DefaultParameterPrefix ?? (Name.Length > 1 ? "--" : "-");
            ValueSeparator = GetSeparator(ref Name) ?? command.DefaultParameterValueSeparator ?? " ";
            this.Name = Name;
            SpaceEncapsulation = command.DefaultParameterSpaceEncapsulation ?? SpaceEncapsulation;
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
}