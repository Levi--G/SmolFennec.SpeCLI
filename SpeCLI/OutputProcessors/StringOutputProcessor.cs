using SpeCLI.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpeCLI.OutputProcessors
{
    public class StringOutputProcessor : IOutputProcessor
    {
        public bool ThrowOnStdError { get; set; } = false;
        public bool OutputEmpty { get; set; } = false;
        public bool CombineOutput { get; set; } = false;

        StringBuilder builder = new StringBuilder();

        public IEnumerable<object> ExecutionEnded(Execution execution)
        {
            if (CombineOutput)
            {
                string s = null;
                lock (builder)
                {
                    if (builder.Length > 0)
                    {
                        s = builder.ToString();
                        builder.Clear();
                    }
                }
                if (s != null)
                {
                    yield return s;
                }
            }
        }

        public void ExecutionStarted(Execution execution)
        {

        }

        public IEnumerable<object> ParseError(Execution execution, string stderror)
        {
            if (ThrowOnStdError)
            {
                throw new Exception($"StandardError recieved").WithData("Output", stderror);
            }
            return Add(stderror);
        }

        public IEnumerable<object> ParseOutput(Execution execution, string stdout)
        {
            return Add(stdout);
        }

        IEnumerable<object> Add(string s)
        {
            if (OutputEmpty || !string.IsNullOrEmpty(s))
            {
                if (CombineOutput)
                {
                    lock (builder)
                    {
                        builder.AppendLine(s);
                    }
                }
                else
                {
                    yield return s;
                }
            }
        }

        public void PreExecutionStarted(Execution execution)
        {
            lock (builder)
            {
                builder.Clear();
            }
        }
    }
}
