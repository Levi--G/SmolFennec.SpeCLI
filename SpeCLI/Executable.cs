using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SpeCLI
{
    public class Executable
    {
        public string Path { get; }

        public int Count => Commands.Count;

        public Command this[string name] { get => Commands[name]; set => Commands[name] = value; }

        Dictionary<string, Command> Commands = new Dictionary<string, Command>();

        public Executable(string Path)
        {
            this.Path = Path;
        }

        public Execution CreateExecution(string name, object arguments = null)
        {
            return CreateExecution(Commands[name], arguments);
        }

        public Execution CreateExecution(Command command, object arguments = null)
        {
            var p = new Process();
            p.StartInfo.FileName = Path;
            p.StartInfo.Arguments = command.ConstructArguments(arguments);
            return new Execution() { Process = p }.ProcessWith(command.Processor);
        }

        public Execution ExecuteCommand(string name, object arguments = null, EventHandler<object> onOutput = null)
        {
            var c = Commands[name];
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
            var c = new Command();
            //TODO: add default config
            Add(name, c);
            return c;
        }

        public void Add(string name, Command command)
        {
            Commands.Add(name, command);
        }

        public bool ContainsCommand(string name)
        {
            return Commands.ContainsKey(name);
        }

        public bool Remove(string name)
        {
            return Commands.Remove(name);
        }

        public bool TryGetCommand(string name, out Command command)
        {
            return Commands.TryGetValue(name, out command);
        }

        public void Clear()
        {
            Commands.Clear();
        }
    }
}
