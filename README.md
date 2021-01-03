# SmolFennec.SpeCLI
**SmolFennec** is a collective name for small libraries and tools released by [Levi](https://github.com/Levi--G) licensed under MIT and hosted on github.
**SpeCLI** provides a way to describe CLI behaviour of external processes.

<img src="https://raw.githubusercontent.com/Levi--G/SmolFennec.SpeCLI/master/SpeCLI/SmolFennec.png" width="300" height="300">

[![NuGet version (SmolFennec.SpeCLI)](https://img.shields.io/nuget/v/SmolFennec.SpeCLI.svg)](https://www.nuget.org/packages/SmolFennec.SpeCLI/)

## Support

Supported platforms: .Net Standard 2.0+

When in trouble:
[Submit an issue](https://github.com/Levi--G/SmolFennec.SpeCLI/issues)

## Usage

### EZ mode:

(The ping command was used below for short instructional purposes, ofcourse it is not meant to be used like this, use [the ping class](https://docs.microsoft.com/en-us/dotnet/api/system.net.networkinformation.ping?view=netcore-3.1) instead!)

Create a class for input and a class for output.
Usable attributes:
- Parameter: Defines a parameter with name and value, like: `-n 50`
- Switch: Defines a switch with name only, like: `-6`
- HideName: Defines a parameter with value only, like: `Hostname`
- When nothing is supplied a parameter with the property name and a value is created

```cs
class PingArguments
{
    [Switch("t")]
    public bool Continuous { get; set; }
    [Parameter("n")]
    public int? Count { get; set; }
    [Parameter("i")]
    public int? TTL { get; set; }
    [Parameter("w")]
    public int? Timeout { get; set; }
    [Switch("4")]
    public bool IPV4 { get; set; }
    [Switch("6")]
    public bool IPV6 { get; set; }
    [HideName]
    public string Host { get; set; }
}

class PingResult
{
    public string ip { get; set; }
    public string fail { get; set; }
    public bool Success => !string.IsNullOrEmpty(ip) && string.IsNullOrEmpty(fail);
    public int bytes { get; set; }
    public TimeSpan time { get; set; }
    public int ttl { get; set; }
}
```

Create an Executable with Commands
Usable OutputProcessors at this time:
- JsonOutputProcessor: Parses the outputted lines as json
- RegexCaptureOutputProcessor: Populates an object with matched regex groups
- RegexSplitOutputProcessor: Splits the lines matching regexes over different OutputProcessors
- CloneOutputProcessor: Clones the output and sends it to other OutputProcessors
- Interface to write your own!

For the individual configuration please look at the configuration properties and methods.

```cs
 // The regex we will use later
const string regex = @"Reply from (?<ip>[^:]+): (?:bytes=(?<bytes>\d+) time[^\d]*(?<time>[^ ]+) TTL=(?<ttl>\d+)|(?<fail>.+))";
 // Create an executable to hold commands to be reused
var exe = new Executable("ping");
 // Add a new command
exe.Add("ping")
     // Add all parameters from this type
    .AddParametersFromType(typeof(PingArguments))
    .WithProcessor(
         // Use this processor to handle regex output
        new RegexCaptureOutputProcessor()
         // Map this regex to the output type we made, this can be done multiple times
        .AddRegex<PingResult>(new Regex(regex))
         // Add a manual mapping for the non-standard timespan conversion
        .AddPropertyMapping(s => TimeSpan.FromMilliseconds(int.Parse(s.TrimEnd('m', 's'))))
     );
```

Use the constructed Executable:

```cs
List<PingResult> matches = exe.ExecuteCommandAndParseList<PingResult>("ping", new PingArguments() {
    Count = 8,
    Host = "Hostname",
    Timeout = 50
});
```

Full sample can be found in the source.

### Manual/Advanced mode:

`IAsyncEnumerable`s can be obtained like this:
```cs
var iae = exe.ExecuteCommandAndParseIAsyncEnumerable<PingResult>("ping", new PingArguments() { Host = "Hostname" });
```

Parameters can also be declared manually. When invoking a nameless type, a `Dictionary<string, object>` or another type with similar properties can also be used. When you specify a `List<object>` any objects generated from whatever type will be returned

```cs
exe.Add("ping")
    .AddParameter<int?>("n")
    .AddParameter(new Parameter<string>("host").WithHideName());
    .AddParameter(new Switch("6"))

var objs = exe.ExecuteCommandAndParseList<object>("ping", new { Host = "Hostname" });
```

An Execution object can also be obtained, altho short-lived (pun intended) these can allow more low-level access to the process, events and Stdin.

```cs
var exec = exe.CreateExecution("ping", new PingArguments() { Host = "Hostname" });
exec.OutputDataReceived += (s, e) => { }; // raw stdout
exec.ErrorDataReceived += (s, e) => { }; // raw stderr
exec.OnOutput += (s, e) => { }; // Parsed objects
exec.OnError += (s, e) => { }; // Exceptions during parsing
exec.Start(); // Needs to be started manually;
exec.SendInputLine("Hello World!"); // Can send stdin
```

# SmolFennec.SpeCLI.Proxy

## Usage

Define Arguments and results exactly like normal SpeCLI configuration. Create a Executable class:
- Enter path to executable or the command in the Executable attribute
- Inherit from IExecutableConfigurator or IExecutionConfigurator to add an OnConfiguring method to apply further configuration during proxy or Execution generation (optional)
- Add any OutputProcessors or additional configuration in the OnConfiguring method

```cs
[Executable("ping")]
public abstract class Ping : IExecutableConfigurator
{
    public void OnConfiguring(Executable executable)
    {
        var processor = new RegexCaptureOutputProcessor()
            .AddRegex<PingResult>(new Regex(@"Reply from (?<ip>[^:]+): (?:bytes=(?<bytes>\d+) time[^\d]*(?<time>[^ ]+) TTL=(?<ttl>\d+)|(?<fail>.+))"))
            .AddPropertyMapping((string s) => !string.IsNullOrEmpty(s) ? TimeSpan.FromMilliseconds(int.Parse(s.TrimEnd('m', 's'))) : (TimeSpan?)null);
        foreach (var command in executable.Commands)
        {
            command.Processor = processor;
        }
    }
}
```

Configure commands in these steps:
- Add Command attribute to a public abstract method with or without custom name
- Add a supported return type: any object supported by selected OutputProcessors or a List<> or IAsyncEnumerable<> of that object, or an Execution object
- Add arguments for your parameters:
  - a single defined object containing parameter attributes (like ping/PingArguments)
  - a set of parameters with their attributes (like ping2)
  - an `object` or `Dictionary<string, object>` => need to add parameter attributes to the method (like ping3/4)
- Adding parameters can be done on the method and defaults can be assigned without adding them as parameter if you want to make a specific method for a certain CLI option (like SinglePing)

Examples:

```cs
[Command("ping1")]
public abstract List<PingResult> ping(PingArguments arguments);
[Command]
public abstract List<PingResult> ping2([Switch("t")] bool Continuous, [Parameter("n")] int? Count, [Parameter("w")] int? Timeout, [HideName] string Host);
[Command]
[Parameter("n")]
[Parameter("Host", HideName = true)]
[Parameter("w")]
public abstract List<PingResult> ping3(object arguments);
[Command]
[Parameter("n")]
[Parameter("Host", HideName = true)]
[Parameter("w")]
public abstract List<PingResult> ping4(Dictionary<string, object> arguments);
[Command]
[Parameter("n", typeof(int?), 1)]
public abstract PingResult SinglePing([HideName] string Host, [Parameter("w")] int? Timeout = null);
[Command]
[Parameter("n", typeof(int?), 1)]
public abstract Execution SinglePingExecution([HideName] string Host, [Parameter("w")] int? Timeout = null);
```

In your code make and use the proxy like this:

```cs
var ping = SpeCLIProxy.Create<Ping>();
var matches = ping.ping(new PingArguments() { Count = pings, Host = "127.0.0.1", Timeout = 50 });
```

This will run the CLI and parse the output all automatically, providing a strongly typed interface to a CLI you only need to configure once.