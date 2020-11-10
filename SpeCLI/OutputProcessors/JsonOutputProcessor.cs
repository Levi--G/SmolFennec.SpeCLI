using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeCLI.OutputProcessors
{
    public class JsonOutputProcessor : IOutputProcessor
    {
        List<Tuple<Type, Func<JObject, bool>>> Handlers = new List<Tuple<Type, Func<JObject, bool>>>();
        public bool ThrowOnStdError { get; set; } = false;

        public void PreExecutionStarted(Execution execution) { }

        public void ExecutionStarted(Execution execution) { }

        public void ExecutionEnded(Execution execution) { }

        public JsonOutputProcessor AddType(Type type, Func<JObject, bool> filter = null)
        {
            Handlers.Add(new Tuple<Type, Func<JObject, bool>>(type, filter));
            return this;
        }

        public JsonOutputProcessor AddType<T>(Func<JObject, bool> filter = null)
        {
            return AddType(typeof(T), filter);
        }

        public IEnumerable<object> ParseOutput(Execution execution, string stdout)
        {
            return Parse(stdout);
        }

        public IEnumerable<object> ParseError(Execution execution, string stderror)
        {
            if (ThrowOnStdError)
            {
                throw new Exception($"StandardError recieved").WithData("Output", stderror);
            }
            return Parse(stderror);
        }

        IEnumerable<object> Parse(string txt)
        {
            var j = JObject.Parse(txt);
            var t = Handlers.FirstOrDefault(h => h.Item2 != null && h.Item2(j)) ?? Handlers.FirstOrDefault(h => h.Item2 == null);
            var r = j.ToObject(t.Item1);
            yield return r;
        }
    }
}
