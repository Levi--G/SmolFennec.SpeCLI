using SpeCLI.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace SpeCLI
{
    public class Executable
    {
        public string Path { get; private set; }

        public int Count => commands.Count;

        public string DefaultParameterValueSeparator { get; set; }
        public string DefaultParameterPrefix { get; set; }
        public string DefaultParameterSpaceEncapsulation { get; set; }
        public string DefaultParameterSeparator { get; set; }
        public IExecutionConfigurator ExecutionConfigurator { get; set; }

        public Command this[string name] { get => commands[name]; set => commands[name] = value; }

        public IEnumerable<Command> Commands => commands.Values;

        private Dictionary<string, Command> commands = new Dictionary<string, Command>();

        public Executable(string Path)
        {
            this.Path = Path;
        }

        public Executable()
        {
        }

        public Executable LoadFromObject(Type type)
        {
            var exec = type.GetCustomAttribute<ExecutableAttribute>(false);
            if (exec != null)
            {
                this.Path = exec.Path;
                DefaultParameterValueSeparator = exec.DefaultParameterValueSeparator;
                DefaultParameterPrefix = exec.DefaultParameterPrefix;
                DefaultParameterSpaceEncapsulation = exec.DefaultParameterSpaceEncapsulation;
                DefaultParameterSeparator = exec.DefaultParameterSeparator;
            }
            foreach (var method in type.GetMethods())
            {
                CommandAttribute command = method.GetCustomAttribute<CommandAttribute>(false);
                if (command != null)
                {
                    var name = command.Name ?? method.Name;
                    Add(name).LoadFromMethod(method);
                }
            }
            return this;
        }

        public Executable ConfigureWith(IExecutableConfigurator configurator)
        {
            configurator.OnConfiguring(this);
            return this;
        }

        public Executable ConfigureWith(IExecutionConfigurator configurator)
        {
            ExecutionConfigurator = configurator;
            return this;
        }

        public Executable LoadFromObject<T>()
        {
            return LoadFromObject(typeof(T));
        }

        public Execution CreateExecution(string name, object arguments = null)
        {
            return CreateExecution(commands[name], arguments);
        }

        public Execution CreateExecution(Command command, object arguments = null)
        {
            var execution = new Execution(Path, command.ConstructArguments(arguments), command);
            ExecutionConfigurator?.OnConfiguring(command, arguments, execution);
            return execution;
        }

        public Execution ExecuteCommand(string name, object arguments = null, EventHandler<object> onOutput = null)
        {
            var c = commands[name];
            var e = CreateExecution(c, arguments);
            if (onOutput != null)
            {
                e.OnOutput += onOutput;
            }
            e.Start();
            return e;
        }

        public List<T> ExecuteCommandAndParseList<T>(string name, object arguments = null)
        {
            return CreateExecution(name, arguments).ParseAsList<T>();
        }

        public Task<List<T>> ExecuteCommandAndParseListAsync<T>(string name, object arguments = null)
        {
            return CreateExecution(name, arguments).ParseAsListAsync<T>();
        }

        public IAsyncEnumerable<T> ExecuteCommandAndParseIAsyncEnumerable<T>(string name, object arguments = null)
        {
            return CreateExecution(name, arguments).ParseAsIAsyncEnumerable<T>();
        }

        public Command Add(string name)
        {
            var c = new Command(name);
            if (DefaultParameterValueSeparator != null)
            {
                c.DefaultParameterValueSeparator = DefaultParameterValueSeparator;
            }
            if (DefaultParameterPrefix != null)
            {
                c.DefaultParameterPrefix = DefaultParameterPrefix;
            }
            if (DefaultParameterSpaceEncapsulation != null)
            {
                c.DefaultParameterSpaceEncapsulation = DefaultParameterSpaceEncapsulation;
            }
            if (DefaultParameterSeparator != null)
            {
                c.ParameterSeparator = DefaultParameterSeparator;
            }
            Add(c);
            return c;
        }

        public void Add(Command command)
        {
            commands.Add(command.Name, command);
        }

        public bool ContainsCommand(string name)
        {
            return commands.ContainsKey(name);
        }

        public bool Remove(string name)
        {
            return commands.Remove(name);
        }

        public bool TryGetCommand(string name, out Command command)
        {
            return commands.TryGetValue(name, out command);
        }

        public void Clear()
        {
            commands.Clear();
        }
    }
}