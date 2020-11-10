using SpeCLI.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SpeCLI
{
    public class Command
    {
        public string DefaultParameterValueSeparator { get; set; }
        public string DefaultParameterPrefix { get; set; }
        public string DefaultParameterSpaceEncapsulation { get; set; }
        public string ParameterSeparator { get; set; } = " ";
        public bool UseCachingMode { get; set; } = false;

        List<IParameter> Parameters = new List<IParameter>();

        List<Tuple<Type, List<Tuple<string, string>>>> InputTypeMappingCache;

        public IOutputProcessor Processor { get; set; }

        public Command()
        {
        }

        public Command AddParameter<T>(string Name, T Default = default, int Priority = 0)
        {
            var Prefix = ParameterHelper.GetPrefix(ref Name) ?? DefaultParameterPrefix ?? (Name.Length > 1 ? "--" : "-");
            var ValueSeparator = ParameterHelper.GetSeparator(ref Name) ?? DefaultParameterValueSeparator ?? " ";
            var SpaceEncapsulation = DefaultParameterSpaceEncapsulation ?? "\"";
            var p = new Parameter<T>()
                .WithName(Name)
                .WithPrefix(Prefix)
                .WithValueSeparator(ValueSeparator)
                .WithSpaceEncapsulation(SpaceEncapsulation)
                .WithDefault(Default)
                .WithPriority(Priority);
            Parameters.Add(p);
            return this;
        }

        public Command AddParameter(IParameter parameter)
        {
            Parameters.Add(parameter);
            return this;
        }

        public Command AddParametersFromType(Type type)
        {
            Parameters.AddRange(type.GetProperties()
                .Where(p => p.CanRead)
                .Select(p => CreateParameter(p))
                .Where(p => !Parameters.Any(pp => pp.Name == p.Name)));
            return this;
        }

        public Command WithProcessor(IOutputProcessor Processor)
        {
            this.Processor = Processor;
            return this;
        }

        IParameter CreateParameter(PropertyInfo propertyInfo)
        {
            var attributes = propertyInfo.GetCustomAttributes();
            var selector = (IParameterSelectorAttribute)(attributes.FirstOrDefault(a => typeof(IParameterSelectorAttribute).IsAssignableFrom(a.GetType())) ?? new ParameterAttribute());
            var ip = selector.Create(propertyInfo);
            foreach (var config in attributes.Where(a => typeof(IParameterConfigureAttribute).IsAssignableFrom(a.GetType())).Cast<IParameterConfigureAttribute>())
            {
                config.Configure(ip);
            }
            return ip;
        }

        public string ConstructArguments(object input)
        {
            input = input ?? new Dictionary<string, object>();
            if (input is IDictionary<string, object> d)
            {
                return ConstructArgumentsInternal(d);
            }
            else
            {
                var t = input.GetType();
                return ConstructArgumentsInternal(ObjectToDictionary(t, input));
            }
        }

        Dictionary<string, object> ObjectToDictionary(Type type, object input)
        {
            return GetOrCreateInputTypeMapping(type)
                .ToDictionary(m => m.Item2, m => GetMemberValue(type.GetMember(m.Item1).First(mi => CanGetMemberValue(mi)), input));
        }

        List<Tuple<string, string>> GetOrCreateInputTypeMapping(Type type)
        {
            var mapping = InputTypeMappingCache?.FirstOrDefault(m => m.Item1 == type)?.Item2;
            if (mapping == null)
            {
                mapping = CreateInputTypeMapping(type);
                if (UseCachingMode)
                {
                    InputTypeMappingCache ??= new List<Tuple<Type, List<Tuple<string, string>>>>();
                    InputTypeMappingCache.Add(Tuple.Create(type, mapping));
                }
            }
            return mapping;
        }

        List<Tuple<string, string>> CreateInputTypeMapping(Type type)
        {
            var l = new List<Tuple<string, string>>();
            var members = type.GetMembers()
                .Where(m => CanGetMemberValue(m))
                .ToList();
            l.AddRange(members
                .Select(m => (m.Name, Actual: m.GetCustomAttributes().Where(a => typeof(IParameterNameAttribute).IsAssignableFrom(a.GetType())).FirstOrDefault()))
                .Where(m => m.Actual != null)
                .Select(m => Tuple.Create(m.Name, (m.Actual as IParameterNameAttribute).Name)));
            l.AddRange(Parameters.Select(p => p.Name)
                .Where(p => l.All(t => t.Item2 != p))
                .Where(p => members.Any(m => p == m.Name))
                .Select(p => Tuple.Create(p, p)));
            return l;
        }

        static bool CanGetMemberValue(MemberInfo memberInfo)
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

        static object GetMemberValue(MemberInfo memberInfo, object forObject)
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

        string ConstructArgumentsInternal(IDictionary<string, object> input)
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
