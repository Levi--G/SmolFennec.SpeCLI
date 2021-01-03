using System;
using System.Collections.Generic;
using System.Text;

namespace SpeCLI
{
    public interface IExecutionConfigurator
    {
        void OnConfiguring(Command command, object arguments, Execution execution);
    }
}
