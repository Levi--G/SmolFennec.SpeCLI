using System;
using System.Collections.Generic;

namespace SpeCLI
{
    public interface IOutputProcessor
    {
        IEnumerable<object> ParseOutput(Execution execution, string stdout);

        IEnumerable<object> ParseError(Execution execution, string stderror);

        void PreExecutionStarted(Execution execution);

        void ExecutionStarted(Execution execution);

        void ExecutionEnded(Execution execution);
    }
}