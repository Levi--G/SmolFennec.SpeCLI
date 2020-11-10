using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpeCLI.OutputProcessors
{
    public class RegexSplitOutputProcessor : IOutputProcessor
    {
        Dictionary<string, IOutputProcessor> Regexes = new Dictionary<string, IOutputProcessor>();
        public bool ThrowOnStdError { get; set; } = false;
        public bool ThrowOnNoMatch { get; set; } = false;

        public void PreExecutionStarted(Execution execution) { }

        public void ExecutionStarted(Execution execution) { }

        public void ExecutionEnded(Execution execution) { }

        public IEnumerable<object> ParseError(Execution execution, string stderror)
        {
            if (ThrowOnStdError)
            {
                throw new Exception($"StandardError recieved").WithData("Output", stderror);
            }
            return Parse(execution, stderror, false);
        }

        public IEnumerable<object> ParseOutput(Execution execution, string stdout)
        {
            return Parse(execution, stdout, true);
        }

        IEnumerable<object> Parse(Execution execution, string txt, bool stdout)
        {
            var m = Regexes.FirstOrDefault(k => Regex.IsMatch(txt, k.Key));
            if (m.Key != null)
            {
                if (stdout)
                {
                    return m.Value.ParseOutput(execution, txt);
                }
                else
                {
                    return m.Value.ParseError(execution, txt);
                }
            }
            else if (ThrowOnNoMatch)
            {
                throw new Exception($"No Regex match found").WithData("Output", txt).WithData("Source", stdout ? "StdOut" : "StdError");
            }
            return Enumerable.Empty<object>();
        }

        public RegexSplitOutputProcessor AddRegex(string regex, IOutputProcessor processor)
        {
            Regexes.Add(regex, processor);
            return this;
        }
    }
}
