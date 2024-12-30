using SpeCLI.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpeCLI.OutputProcessors
{
    public class RegexCaptureOutputProcessor : IOutputProcessor
    {
        private List<Tuple<Regex, Type>> Regexes = new List<Tuple<Regex, Type>>();
        public bool ThrowOnStdError { get; set; } = false;
        public bool ThrowOnNoMatch { get; set; } = false;
        public bool ContinuousMode { get; set; } = false;
        public bool UseCachingMode { get; set; } = false;

        private string stdoutcache;
        private string stderrorcache;

        private List<Tuple<Type, Func<string[], object>>> PropertyMappings = new List<Tuple<Type, Func<string[], object>>>();
        private List<Tuple<Type, Func<Regex, Match, object>>> TypeMappings = new List<Tuple<Type, Func<Regex, Match, object>>>();
        private List<Tuple<Type, Func<Dictionary<string, string[]>, object>>> ConstructorMappings = new List<Tuple<Type, Func<Dictionary<string, string[]>, object>>>();

        public RegexCaptureOutputProcessor()
        {
        }

        public RegexCaptureOutputProcessor(Regex regex, Type type, Func<Regex, Match, object> typeMapping = null)
        {
            AddRegex(regex, type, typeMapping);
        }

        public RegexCaptureOutputProcessor(Regex regex, Type type, Func<Dictionary<string, string[]>, object> typeMapping = null)
        {
            AddRegex(regex, type, typeMapping);
        }

        public void PreExecutionStarted(Execution execution)
        {
        }

        public void ExecutionStarted(Execution execution)
        {
        }

        public IEnumerable<object> ExecutionEnded(Execution execution)
        {
            return Enumerable.Empty<object>();
        }

        public IEnumerable<object> ParseError(Execution execution, string stderror)
        {
            if (ThrowOnStdError)
            {
                throw new Exception($"StandardError recieved").WithData("Output", stderror);
            }
            return ParseMode(stderror, false);
        }

        public IEnumerable<object> ParseOutput(Execution execution, string stdout)
        {
            return ParseMode(stdout, true);
        }

        private IEnumerable<object> ParseMode(string txt, bool stdout)
        {
            if (ContinuousMode && (stdout ? stdoutcache != null : stderrorcache != null))
            {
                txt = (stdout ? stdoutcache : stderrorcache) + Environment.NewLine + txt;
            }
            if (string.IsNullOrEmpty(txt))
            {
                yield break;
            }
            int i = 0;
            while (txt.Length > 0 && Parse(txt, out int index, out int length, out var match))
            {
                if (match != null)
                {
                    yield return match;
                }
                txt = txt.Remove(index, length);
                i++;
            }
            if (i == 0 && ThrowOnNoMatch)
            {
                throw new Exception($"No Regex match found").WithData("Output", txt);
            }
            if (ContinuousMode)
            {
                txt = txt.Trim('\r', '\n');
                if (txt.Length > 0)
                {
                    if (stdout)
                    {
                        stdoutcache = txt;
                    }
                    else
                    {
                        stderrorcache = txt;
                    }
                }
            }
        }

        private bool Parse(string txt, out int index, out int length, out object match)
        {
            var m = Regexes.Select(k => (Match: k.Item1.Match(txt), Type: k.Item2, Regex: k.Item1)).FirstOrDefault(k => k.Match.Success);
            if (m.Match != null)
            {
                match = GetOrCreateTypeMapping(m.Type)(m.Regex, m.Match);
                index = m.Match.Index;
                length = m.Match.Length;
                return true;
            }
            match = null;
            index = 0;
            length = 0;
            return false;
        }

        public RegexCaptureOutputProcessor AddRegex(Regex regex, Type type, Func<Regex, Match, object> typeMapping = null)
        {
            Regexes.Add(Tuple.Create(regex, type));
            return AddTypeMapping(type, typeMapping);
        }

        public RegexCaptureOutputProcessor AddRegex(Regex regex, Type type, Func<Dictionary<string, string[]>, object> typeMapping)
        {
            return AddRegex(regex, type, (r, m) => typeMapping(MatchToDictionary(r, m)));
        }

        public RegexCaptureOutputProcessor AddRegex<T>(Regex regex, Func<Regex, Match, T> typeMapping = null)
        {
            return AddRegex(regex, typeof(T), typeMapping == null ? null : (Func<Regex, Match, object>)((r, m) => typeMapping(r, m)));
        }

        public RegexCaptureOutputProcessor AddRegex<T>(Regex regex, Func<Dictionary<string, string[]>, T> typeMapping)
        {
            return AddRegex(regex, typeof(T), d => typeMapping(d));
        }

        public RegexCaptureOutputProcessor AddTypeMapping(Type type, Func<Regex, Match, object> typeMapping)
        {
            if (typeMapping != null)
            {
                TypeMappings.Add(Tuple.Create(type, typeMapping));
            }
            return this;
        }

        public RegexCaptureOutputProcessor AddTypeMapping(Type type, Func<Dictionary<string, string[]>, object> typeMapping)
        {
            return AddTypeMapping(type, (r, m) => typeMapping(MatchToDictionary(r, m)));
        }

        public RegexCaptureOutputProcessor AddTypeMapping<T>(Func<Regex, Match, T> typeMapping)
        {
            return AddTypeMapping(typeof(T), (r, m) => typeMapping(r, m));
        }

        public RegexCaptureOutputProcessor AddTypeMapping<T>(Func<Dictionary<string, string[]>, T> typeMapping)
        {
            return AddTypeMapping(typeof(T), d => typeMapping(d));
        }

        public RegexCaptureOutputProcessor AddPropertyMapping(Type type, Func<string[], object> propertyMapping)
        {
            PropertyMappings.Add(Tuple.Create(type, propertyMapping));
            return this;
        }

        public RegexCaptureOutputProcessor AddPropertyMapping<T>(Func<string[], T> propertyMapping)
        {
            return AddPropertyMapping(typeof(T), s => propertyMapping(s));
        }

        public RegexCaptureOutputProcessor AddPropertyMapping(Type type, Func<string, object> propertyMapping)
        {
            PropertyMappings.Add(Tuple.Create(type, (Func<string[], object>)(s => propertyMapping(s.LastOrDefault()))));
            return this;
        }

        public RegexCaptureOutputProcessor AddPropertyMapping<T>(Func<string, T> propertyMapping)
        {
            return AddPropertyMapping(typeof(T), s => propertyMapping(s));
        }

        public RegexCaptureOutputProcessor AddConstructorMapping(Type type, Func<Dictionary<string, string[]>, object> constructorMapping)
        {
            ConstructorMappings.Add(Tuple.Create(type, constructorMapping));
            return this;
        }

        public RegexCaptureOutputProcessor AddConstructorMapping<T>(Func<Dictionary<string, string[]>, T> constructorMapping)
        {
            return AddConstructorMapping(typeof(T), d => constructorMapping(d));
        }

        private static Dictionary<string, string[]> MatchToDictionary(Regex regex, Match match)
        {
            return match.Groups.OfType<Group>().Select((g, i) => (name: regex.GroupNameFromNumber(i), value: g.Captures.OfType<Capture>().Select(c => c.Value).ToArray())).ToDictionary(g => g.name, g => g.value);
        }

        private Func<Regex, Match, object> GetOrCreateTypeMapping(Type type)
        {
            var mapping = TypeMappings.FirstOrDefault(m => m.Item1 == type)?.Item2;
            if (mapping == null)
            {
                mapping = CreateTypeMapping(type);
                if (UseCachingMode)
                {
                    TypeMappings.Add(Tuple.Create(type, mapping));
                }
            }
            return mapping;
        }

        private Func<Regex, Match, object> CreateTypeMapping(Type type)
        {
            if (type == typeof(Match))
            {
                return (r, m) => m;
            }
            if (type == typeof(string))
            {
                return (r, m) => m.Value;
            }
            if (type == typeof(string[]))
            {
                return (r, m) => m.Captures.OfType<Capture>().Select(c => c.Value).ToArray();
            }
            if (type == typeof(Dictionary<string, string[]>))
            {
                return (r, m) => MatchToDictionary(r, m);
            }
            var constructor = GetOrCreateConstructorMapping(type);
            var mapping = type.GetProperties().Where(p => p.CanWrite).Select(p => (p.Name, mapping: GetOrCreatePropertyMapping(p.PropertyType), setter: (Action<object, object>)p.SetValue)).ToList();
            return (regex, match) =>
            {
                var dict = MatchToDictionary(regex, match);
                var obj = constructor(dict);
                foreach (var item in dict)
                {
                    var map = mapping.FirstOrDefault(m => m.Name == item.Key);
                    map.setter?.Invoke(obj, map.mapping?.Invoke(item.Value));
                }
                return obj;
            };
        }

        private Func<Dictionary<string, string[]>, object> GetOrCreateConstructorMapping(Type type)
        {
            var mapping = ConstructorMappings.FirstOrDefault(m => m.Item1 == type)?.Item2;
            if (mapping == null)
            {
                mapping = CreateConstructorMapping(type);
                if (UseCachingMode)
                {
                    ConstructorMappings.Add(Tuple.Create(type, mapping));
                }
            }
            return mapping;
        }

        private Func<Dictionary<string, string[]>, object> CreateConstructorMapping(Type type)
        {
            var c = type.GetConstructors().FirstOrDefault(co => co.GetParameters().Length == 0);
            if (c != null)
            {
                return d => c.Invoke(Array.Empty<object>());
            }
            return d =>
            {
                var co = type.GetConstructors().FirstOrDefault(con => con.GetParameters().All(p => d.ContainsKey(p.Name)));
                if (co == null) { return default; }
                return co.Invoke(co.GetParameters().Select(p => GetOrCreatePropertyMapping(p.ParameterType).Invoke(d[p.Name])).ToArray());
            };
        }

        private Func<string[], object> GetOrCreatePropertyMapping(Type type)
        {
            var mapping = PropertyMappings.FirstOrDefault(m => m.Item1 == type)?.Item2;
            if (mapping == null)
            {
                mapping = CreatePropertyMapping(type);
                if (UseCachingMode)
                {
                    PropertyMappings.Add(Tuple.Create(type, mapping));
                }
            }
            return mapping;
        }

        private Func<string[], object> CreatePropertyMapping(Type type)
        {
            if (type == typeof(string[]))
            {
                return o => o;
            }
            if (type == typeof(string))
            {
                return o => o?.LastOrDefault();
            }
            if (type.IsArray)
            {
                var et = type.GetElementType();
                var it = GetOrCreatePropertyMapping(et);
                return o =>
                {
                    var a = Array.CreateInstance(et, o.Length);
                    for (int i = 0; i < o.Length; i++)
                    {
                        a.SetValue(it.Invoke(new[] { o[i] }), i);
                    }
                    return a;
                };
            }
            if (typeof(IConvertible).IsAssignableFrom(type))
            {
                return o =>
                {
                    var s = o?.LastOrDefault();
                    return s != null ? Convert.ChangeType(s, type) : type.GetDefault();
                };
            }
            var converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertFrom(typeof(string)))
            {
                return o =>
                {
                    var s = o?.LastOrDefault();
                    return s != null ? converter.ConvertFrom(s) : type.GetDefault();
                };
            }
            return o => type.GetDefault();
        }
    }
}