using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SpeCLI.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Method, AllowMultiple = true)]
    public class ParameterAttribute : Attribute, IParameterSelectorAttribute, IParameterNameAttribute, IParameterConfigureAttribute
    {
        public string Name { get; }
        int? priority;
        Type Type;
        object Default;
        bool? hideName;

        public int Priority { get => priority.GetValueOrDefault(); set => priority = value; }
        public bool HideName { get => hideName.GetValueOrDefault(); set => hideName = value; }

        public ParameterAttribute(string Name = null, Type Type = null, object Default = null)
        {
            this.Name = Name;
            this.Type = Type;
            this.Default = Default;
        }

        public IParameter Create(string defaultName, Command command, MemberInfo memberInfo, ParameterInfo parameterInfo)
        {
            var name = Name ?? defaultName;
            if (string.IsNullOrEmpty(name))
            {
                throw new Exception("Parameters not linked to a property or parameterer need a name");
            }
            return new Parameter(command, name, Type ?? memberInfo?.GetReturnType() ?? parameterInfo?.ParameterType);
        }

        public void Configure(IParameter parameter)
        {
            var p = parameter as Parameter;
            if (p != null)
            {
                if (Default != null)
                {
                    p.Default = Default;
                }
                if (priority != null)
                {
                    p.Priority = priority.Value;
                }
                if (hideName != null)
                {
                    p.HideName = hideName.Value;
                }
            }
        }
    }
}
