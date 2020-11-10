using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SpeCLI.Attributes
{
    public interface IParameterConfigureAttribute
    {
        void Configure(IParameter parameter);
    }
}
