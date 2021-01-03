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

        public static T Create<T>() where T : class
        {
            var proxyGenerator = new ProxyGenerator();
            var proxy = new SpeCLIProxy();
            var proxied = proxyGenerator.CreateClassProxy<T>(proxy);
            proxy.Configure(proxied);
            return proxied;
        }

        internal SpeCLIProxy() { }

        internal void Configure(object executable)
        {
            var targettype = ProxyUtil.GetUnproxiedType(executable);
            Executable = new Executable().LoadFromObject(targettype);
            if (executable is IExecutableConfigurator execonfig)
            {
                Executable.ConfigureWith(execonfig);
            }
            if (executable is IExecutionConfigurator executionconfig)
            {
                Executable.ConfigureWith(executionconfig);
            }
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
            if (TargetType == typeof(Execution))
            {
                invocation.ReturnValue = exec;
                return;
            }
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
