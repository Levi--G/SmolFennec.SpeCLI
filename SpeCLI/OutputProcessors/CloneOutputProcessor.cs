using SpeCLI.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeCLI.OutputProcessors
{
    public class CloneOutputProcessor : IOutputProcessor
    {
        public bool ThrowOnStdError { get; set; } = false;
        private List<IOutputProcessor> Clones = new List<IOutputProcessor>();

        public CloneOutputProcessor(params IOutputProcessor[] clones)
        {
            Clones.AddRange(clones);
        }

        public void PreExecutionStarted(Execution execution)
        {
            Clones.ForEach(c => c.PreExecutionStarted(execution));
        }

        public void ExecutionStarted(Execution execution)
        {
            Clones.ForEach(c => c.ExecutionStarted(execution));
        }

        public IEnumerable<object> ExecutionEnded(Execution execution)
        {
            return Clones.SelectMany(c => c.ExecutionEnded(execution));
        }

        public IEnumerable<object> ParseOutput(Execution execution, string stdout)
        {
            return Clones.SelectMany(c => c.ParseOutput(execution, stdout));
        }

        public IEnumerable<object> ParseError(Execution execution, string stderror)
        {
            if (ThrowOnStdError)
            {
                throw new Exception($"StandardError recieved").WithData("Output", stderror);
            }
            return Clones.SelectMany(c => c.ParseError(execution, stderror));
        }

        public CloneOutputProcessor AddClone(IOutputProcessor processor)
        {
            Clones.Add(processor);
            return this;
        }
    }
}