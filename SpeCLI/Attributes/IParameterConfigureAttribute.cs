using System;

namespace SpeCLI.Attributes
{
    public interface IParameterConfigureAttribute
    {
        void Configure(IParameter parameter);
    }
}