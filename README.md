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