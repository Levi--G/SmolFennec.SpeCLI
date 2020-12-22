using Castle.DynamicProxy;
using SpeCLI.Attributes;
using SpeCLI.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SpeCLI.Proxy
{
    public class SpeCLIProxy : IInterceptor
    {
        Executable Executable;

        public static T Create<T>() where T : class, IExecutable
        {
            var proxyGenerator = new ProxyGenerator();
            var proxy = new SpeCLIProxy();
            var proxied = proxyGenerator.CreateClassProxy<T>(proxy);
            proxy.Configure(proxied);
            return proxied;
        }

        internal SpeCLIProxy() { }

        internal void Configure(IExecutable executable)
        {
            var targettype = ProxyUtil.GetUnproxiedType(executable);
            Executable = new Executable().LoadFromObject(targettype);
            executable.OnConfiguring(Executable);
        }

        public void Intercept(IInvocation invocation)
        {
            var arguments = GetArguments(invocation);
            var method = invocation.Method;
            var TargetType = method.ReturnType;

            var commandname = method.GetCustomAttribute<CommandAttribute>()?.Name ?? method.Name;

            if (TargetType == typeof(void))
            {
                Executable.ExecuteCommand(commandname, arguments).WaitForExit();
                return;
            }
            var exec = Executable.CreateExecution(commandname, arguments);
            if (TargetType.IsGenericType)
            {
                var gt = TargetType.GetGenericTypeDefinition();
                var ga = TargetType.GetGenericArguments().FirstOrDefault();
                invocation.ReturnValue = GetGenericT(ga, gt, exec);
                return;
            }
            var list = GetGenericT(TargetType, typeof(List<>), exec) as IEnumerable;
            invocation.ReturnValue = list.Cast<object>().FirstOrDefault();
        }

        object GetArguments(IInvocation invocation)
        {
            var method = invocation.Method;
            var parameters = method.GetParameters();
            if (parameters.Length == 1 && parameters.First().GetCustomAttribute<IParameterSelectorAttribute>() == null)
            {
                return invocation.Arguments.First();
            }
            else
            {
                var d = new Dictionary<string, object>();
                int i = 0;
                foreach (var item in invocation.Arguments)
                {
                    d.Add(parameters[i].Name, item);
                    i++;
                }
                return d;
            }
        }

        object GetGenericT(Type T, Type TargetType, Execution exe)
        {
            return this.GetType()
                .GetMethod(nameof(GetGeneric), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(T)
                .Invoke(this, new object[] { TargetType, exe });
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Accessed by reflection")]
        object GetGeneric<T>(Type TargetType, Execution exe)
        {
            if (TargetType == typeof(List<>))
            {
                return exe.ParseAsList<T>();
            }
            if (TargetType == typeof(IAsyncEnumerable<>))
            {
                return exe.ParseAsIAsyncEnumerable<T>();
            }
            throw new Exception($"Object of type {TargetType} is not supported at this time");
        }
    }
}
