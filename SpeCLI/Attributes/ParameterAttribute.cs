using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SpeCLI.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class ParameterAttribute : Attribute, IParameterSelectorAttribute, IParameterNameAttribute
    {
        public string Name { get; }
        int Priority;
        Type Type;

        public ParameterAttribute(string Name = null, Type Type = null, int Priority = 0)
        {
            this.Priority = Priority;
            this.Name = Name;
            this.Type = Type;
        }

        public IParameter Create(PropertyInfo propertyInfo)
        {
            return new Parameter(Name ?? propertyInfo.Name, Type ?? propertyInfo.PropertyType, default, Priority);
        }

        public IParameter Create(ParameterInfo parameterInfo)
        {
            return new Parameter(Name ?? parameterInfo.Name, Type ?? parameterInfo.ParameterType, default, Priority);
        }
    }
}
