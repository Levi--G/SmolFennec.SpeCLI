using SpeCLI.Attributes;
using SpeCLI.Extensions;
using SpeCLI.OutputProcessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SpeCLI
{
    public class Command
    {
        public string Name { get; }

        public string DefaultParameterValueSeparator { get; set; }
        public string DefaultParameterPrefix { get; set; }
        public string DefaultParameterSpaceEncapsulation { get; set; }
        public string ParameterSeparator { get; set; } = " ";


        public bool DefaultExecutionThrowOnErrorWhileParse { get; set; }

        public bool DefaultExecutionAbortOnErrorWhileParse { get; set; }

        private List<IParameter> Parameters = new List<IParameter>();

        private List<Tuple<string, string>> InputTypeMapping = new List<Tuple<string, string>>();

        public IOutputProcessor Processor { get; set; }

        public Command(string Name)
        {
            this.Name = Name;
        }

        public Command AddParameter<T>(string Name, T Default = default, int Priority = 0)
        {
            var p = new Parameter(this, Name, typeof(T), Default, Priority);
            Parameters.Add(p);
            CreateMemberMapping(Name, Name);
            return this;
        }

        public Command AddSwitch(string Name, bool Default = default, int Priority = 0)
        {
            var s = new Switch(this, Name, Default, Priority);
            Parameters.Add(s);
            CreateMemberMapping(Name, Name);
            return this;
        }

        public Command AddParameter(IParameter parameter)
        {
            Parameters.Add(parameter);
            CreateMemberMapping(parameter.Name, parameter.Name);
            return this;
        }

        public Command AddParametersFromType(Type type)
        {
            foreach (var member in type.GetProperties().Cast<MemberInfo>().Concat(type.GetFields().Cast<MemberInfo>()).Concat(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod).Where(x => !x.IsSpecialName && !x.IsConstructor && x.DeclaringType != typeof(object)).Cast<MemberInfo>())
                .Where(m => CanGetMemberValue(m)))
            {
                AddParameter(member);
            }
            return this;
        }

        public Command LoadFromMethod(MethodInfo method)
        {
            var commandatt = method.GetCustomAttribute<CommandAttribute>();
            if (commandatt != null)
            {
                if (commandatt.DefaultParameterPrefix != null)
                {
                    this.DefaultParameterPrefix = commandatt.DefaultParameterPrefix;
                }
                if (commandatt.DefaultParameterSpaceEncapsulation != null)
                {
                    this.DefaultParameterSpaceEncapsulation = commandatt.DefaultParameterSpaceEncapsulation;
                }
                if (commandatt.DefaultParameterValueSeparator != null)
                {
                    this.DefaultParameterValueSeparator = commandatt.DefaultParameterValueSeparator;
                }
                if (commandatt.ParameterSeparator != null)
                {
                    this.ParameterSeparator = commandatt.ParameterSeparator;
                }
            }

            var extraparameters = method.GetCustomAttributes<IParameterSelectorAttribute>(false).Select(p =>
            {
                var ip = p.Create(null, this, null, null);
                if (p is IParameterConfigureAttribute c)
                {
                    c.Configure(ip);
                }
                return ip;
            });
            Parameters.AddRange(extraparameters);

            var parameters = method.GetParameters();
            if (parameters.Length == 1 && parameters.First().GetCustomAttribute<IParameterSelectorAttribute>() == null)
            {
                if (parameters.First().ParameterType == typeof(object) || parameters.First().ParameterType == typeof(Dictionary<string, object>))
                {
                    foreach (var p in Parameters)
                    {
                        CreateMemberMapping(p.Name, p.Name);
                    }
                }
                else
                {
                    AddParametersFromType(parameters.First().ParameterType);
                }
            }
            else
            {
                foreach (var item in parameters)
                {
                    AddParameter(item);
                }
            }

            var ret = method.ReturnType;
            if (ret == typeof(string))
            {
                this.Processor ??= new StringOutputProcessor() { CombineOutput = true };
            }
            if (ret == typeof(List<string>) || ret == typeof(IAsyncEnumerable<string>))
            {
                this.Processor ??= new StringOutputProcessor();
            }

            return this;
        }

        public Command WithProcessor(IOutputProcessor Processor)
        {
            this.Processor = Processor;
            return this;
        }

        private void AddParameter(MemberInfo member)
        {
            var aname = member.GetCustomAttribute<IParameterNameAttribute>()?.Name ?? member.Name;
            var ip = GetOrCreateParameter(member, aname);
            CompleteParameter(ip, member);
            CreateMemberMapping(member.Name, aname);
        }

        private void AddParameter(ParameterInfo member)
        {
            var aname = member.GetCustomAttribute<IParameterNameAttribute>()?.Name ?? member.Name;
            var ip = GetOrCreateParameter(member, aname);
            CompleteParameter(ip, member);
            CreateMemberMapping(member.Name, aname);
        }

        private IParameter GetOrCreateParameter(ICustomAttributeProvider info, string aname)
        {
            var ip = Parameters.FirstOrDefault(predicate => predicate.Name == aname);
            if (ip != null)
            {
                return ip;
            }
            var selector = info.GetCustomAttribute<IParameterSelectorAttribute>(false) ?? new ParameterAttribute();
            ip = selector.Create(aname, this, info as MemberInfo, info as ParameterInfo);
            Parameters.Add(ip);
            return ip;
        }

        private void CompleteParameter(IParameter ip, ICustomAttributeProvider info)
        {
            foreach (var config in info.GetCustomAttributes<IParameterConfigureAttribute>(false))
            {
                config.Configure(ip);
            }
        }

        public string ConstructArguments(object input)
        {
            input = input ?? new Dictionary<string, object>();
            if (input is IDictionary<string, object> d)
            {
                return ConstructArgumentsInternal(DictionaryToDictionary(d));
            }
            else
            {
                var t = input.GetType();
                return ConstructArgumentsInternal(ObjectToDictionary(t, input));
            }
        }

        private Dictionary<string, object> DictionaryToDictionary(IDictionary<string, object> input)
        {
            return InputTypeMapping
                .ToDictionary(m => m.Item2, m => input.TryGetValue(m.Item2, out object val) || input.TryGetValue(m.Item1, out val) ? val : null);
        }

        private Dictionary<string, object> ObjectToDictionary(Type type, object input)
        {
            return InputTypeMapping
                .ToDictionary(m => m.Item2, m => GetMemberValue(type.GetMember(m.Item1).FirstOrDefault(mi => CanGetMemberValue(mi)), input));
        }

        private void CreateMemberMapping(string mname, string aname)
        {
            if (Parameters.Any(predicate => predicate.Name == aname))
            {
                InputTypeMapping.Add(Tuple.Create(mname, aname));
            }
        }

        private static bool CanGetMemberValue(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo f)
            {
                return true;
            }
            if (memberInfo is PropertyInfo p)
            {
                return p.CanRead;
            }
            if (memberInfo is MethodInfo m && m.GetParameters().All(pm => pm.IsOptional))
            {
                return true;
            }
            return false;
        }

        private static object GetMemberValue(MemberInfo memberInfo, object forObject)
        {
            if (memberInfo is FieldInfo f)
            {
                return f.GetValue(forObject);
            }
            if (memberInfo is PropertyInfo p)
            {
                return p.GetValue(forObject);
            }
            if (memberInfo is MethodInfo m && m.GetParameters().All(pm => pm.IsOptional))
            {
                return m.Invoke(forObject, Array.Empty<object>());
            }
            return null;
        }

        private string ConstructArgumentsInternal(IDictionary<string, object> input)
        {
            return string.Join(ParameterSeparator, Parameters.OrderBy(p => p.Priority).Select(p => p.GetObjectValue(input.TryGetValue(p.Name, out var o) ? o : null)).Where(p => p != null));
        }

        public Command WithParameterSeparator(string ParameterSeparator)
        {
            this.ParameterSeparator = ParameterSeparator;
            return this;
        }
    }
}