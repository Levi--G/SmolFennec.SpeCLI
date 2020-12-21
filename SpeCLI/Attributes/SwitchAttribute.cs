using System;
using System.Reflection;

namespace SpeCLI.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Method)]
    public class SwitchAttribute : Attribute, IParameterSelectorAttribute, IParameterNameAttribute, IParameterConfigureAttribute
    {
        public string Name { get; }
        private int? priority;
        private bool? @default;
        public int Priority { set => priority = value; }

        public SwitchAttribute(string Name = null)
        {
            this.Name = Name;
        }

        public SwitchAttribute(bool Default)
        {
            this.@default = Default;
        }

        public SwitchAttribute(string Name, bool Default)
        {
            this.Name = Name;
            this.@default = Default;
        }

        public IParameter Create(string defaultName, Command command, MemberInfo memberInfo, ParameterInfo parameterInfo)
        {
            var name = Name ?? defaultName;
            if (string.IsNullOrEmpty(name))
            {
                throw new Exception("Parameters not linked to a property or parameterer need a name");
            }
            return new Switch(command, name);
        }

        public void Configure(IParameter parameter)
        {
            var p = parameter as Switch;
            if (p != null)
            {
                if (@default.HasValue)
                {
                    p.Default = @default.Value;
                }
                if (priority.HasValue)
                {
                    p.Priority = priority.Value;
                }
            }
        }
    }
}