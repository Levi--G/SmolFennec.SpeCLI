using System;
using System.Reflection;

namespace SpeCLI.Attributes
{
    public interface IParameterSelectorAttribute
    {
        IParameter Create(string defaultName, Command command, MemberInfo memberInfo, ParameterInfo parameterInfo);
    }
}