using SpeCLI.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpeCLI.OutputProcessors
{
    public class RegexSplitOutputProcessor : IOutputProcessor
    {
        private Dictionary<Regex, IOutputProcessor> Regexes = new Dictionary<Regex, IOutputProcessor>();
        public bool ThrowOnStdError { get; set; } = false;
        public bool ThrowOnNoMatch { get; set; } = false;

        public void PreExecutionStarted(Execution execution)
        {
            foreach (var item in Regexes)
            {
                item.Value.PreExecutionStarted(execution);
            }
        }

        public void ExecutionStarted(Execution execution)
        {
            foreach (var item in Regexes)
            {
                item.Value.ExecutionStarted(execution);
            }
        }

        public IEnumerable<object> ExecutionEnded(Execution execution)
        {
            return Regexes.SelectMany(r => r.Value.ExecutionEnded(execution));
        }

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

        private IEnumerable<object> Parse(Execution execution, string txt, bool stdout)
        {
            if (string.IsNullOrEmpty(txt))
            {
                return null;
            }
            var m = Regexes.FirstOrDefault(k => k.Key.IsMatch(txt));
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
            Regexes.Add(new Regex(regex), processor);
            return this;
        }

        public RegexSplitOutputProcessor AddRegex(Regex regex, IOutputProcessor processor)
        {
            Regexes.Add(regex, processor);
            return this;
        }
    }
}