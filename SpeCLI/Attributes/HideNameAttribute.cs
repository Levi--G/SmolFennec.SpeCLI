using System;

namespace SpeCLI.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class HideNameAttribute : Attribute, IParameterConfigureAttribute
    {
        public void Configure(IParameter parameter)
        {
            (parameter as Parameter)?.WithHideName(true);
        }
    }
}