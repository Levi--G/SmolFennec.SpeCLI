using System;
using System.Collections.Generic;
using System.Text;

namespace SpeCLI.Proxy
{
    public interface IExecutable
    {
        void OnConfiguring(Executable executable);
    }
}
