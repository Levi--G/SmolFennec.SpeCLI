using System;
using System.Collections.Generic;
using System.Text;

namespace SpeCLI
{
    public interface IExecutableConfigurator
    {
        void OnConfiguring(Executable executable);
    }
}
