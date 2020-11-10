using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SpeCLI.Attributes
{
    public interface IParameterNameAttribute
    {
        string Name { get; }
    }
}
