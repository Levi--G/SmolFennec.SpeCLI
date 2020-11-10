using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SpeCLI.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class SwitchAttribute : Attribute, IParameterSelectorAttribute, IParameterNameAttribute
    {
        public string Name { get; }
        int Priority;

        public SwitchAttribute(string Name = null, int Priority = 0)
        {
            this.Priority = Priority;
            this.Name = Name;
        }

        public IParameter Create(PropertyInfo propertyInfo)
        {
            return new Switch(Name ?? propertyInfo.Name, Priority);
        }

        public IParameter Create(ParameterInfo parameterInfo)
        {
            return new Switch(Name ?? parameterInfo.Name, Priority);
        }
    }
}
