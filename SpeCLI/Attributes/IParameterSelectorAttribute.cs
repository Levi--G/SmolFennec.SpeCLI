using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SpeCLI.Attributes
{
    public interface IParameterSelectorAttribute
    {
        IParameter Create(string defaultName, Command command, MemberInfo memberInfo, ParameterInfo parameterInfo);
    }
}
