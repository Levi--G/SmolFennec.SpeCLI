using System;
using System.Collections.Generic;
using System.Text;

namespace SpeCLI.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExecutableAttribute : Attribute
    {
        public string Path { get; set; }
        public string DefaultParameterValueSeparator { get; set; }
        public string DefaultParameterPrefix { get; set; }
        public string DefaultParameterSpaceEncapsulation { get; set; }
        public string DefaultParameterSeparator { get; set; }

        public ExecutableAttribute(string Path = null)
        {
            this.Path = Path;
        }
    }
}
