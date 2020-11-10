using System;
using Xunit;
using SpeCLI;
using SpeCLI.OutputProcessors;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using SpeCLI.Attributes;

namespace SpeCLI.Tests
{
    public class FullWindowsTests
    {
        [Fact]
        public void Ping()
        {
            var pings = 2;
            var exe = new Executable("ping");

            //alternative
            //exe.Add("ping")
            //    .AddParameter<int?>("n")
            //    .AddParameter(new Parameter<string>("host").WithHideName());
            //    ...
            exe.Add("ping")
                .AddParametersFromType(typeof(PingArguments))
                .WithProcessor(
                    new RegexCaptureOutputProcessor()
                    .AddRegex<PingResult>(new Regex(@"Reply from (?<ip>[^:]+): (?:bytes=(?<bytes>\d+) time[^\d]*(?<time>[^ ]+) TTL=(?<ttl>\d+)|(?<fail>.+))"))
                    .AddPropertyMapping(s => TimeSpan.FromMilliseconds(int.Parse(s.TrimEnd('m', 's'))))
                 );

            var matches = exe.ExecuteCommandAndParseList<PingResult>("ping", new PingArguments() { Count = pings, Host = "Hostname", TTL = 2, Timeout = 50 });
            //var iae = exe.ExecuteCommandAndParseIAsyncEnumerable<PingResult>("ping", new PingArguments() { Host = "Hostname" });
            //var exec = exe.CreateExecution("ping", new PingArguments() { Host = "Hostname" });
            //exec.OutputDataReceived += (s, e) => { }; // raw stdout
            //exec.ErrorDataReceived += (s, e) => { }; // raw stderr
            //exec.OnOutput += (s, e) => { }; // Parsed objects
            //exec.OnError += (s, e) => { }; // Exceptions during parsing
            //exec.Start(); // Needs to be started manually;
            //exec.SendInputLine("Hello World!"); // Can send stdin
            Assert.Equal(pings, matches.Count);
        }

        class PingArguments
        {
            /// <summary>
            /// Ping the specified host until stopped
            /// </summary>
            [Switch("t")]
            public bool Continuous { get; set; }
            /// <summary>
            /// Number of echo requests to send
            /// </summary>
            [Parameter("n")]
            public int? Count { get; set; }
            /// <summary>
            /// Time To Live
            /// </summary>
            [Parameter("i")]
            public int? TTL { get; set; }
            /// <summary>
            /// Timeout in milliseconds to wait for each reply
            /// </summary>
            [Parameter("w")]
            public int? Timeout { get; set; }
            /// <summary>
            /// Force using IPv4
            /// </summary>
            [Switch("4")]
            public bool IPV4 { get; set; }
            /// <summary>
            /// Force using IPv6
            /// </summary>
            [Switch("6")]
            public bool IPV6 { get; set; }
            /// <summary>
            /// The hostname or ip to ping
            /// </summary>
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
    }
}
