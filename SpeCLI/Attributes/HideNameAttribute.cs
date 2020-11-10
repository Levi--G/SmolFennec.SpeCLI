using System;
using System.Collections.Generic;
using System.Text;

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
