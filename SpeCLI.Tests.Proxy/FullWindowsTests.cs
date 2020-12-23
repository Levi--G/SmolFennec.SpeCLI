using SpeCLI.Proxy;
using System;
using Xunit;
using SpeCLI.Attributes;
using System.Collections.Generic;
using SpeCLI.OutputProcessors;
using System.Text.RegularExpressions;

namespace SpeCLI.Tests.Proxy
{
    public class FullWindowsTests
    {
        [Fact]
        public void PingTest()
        {
            var pings = 2;
            var ping = SpeCLIProxy.Create<Ping>();
            var matches = 0;
            matches += ping.ping(new PingArguments() { Count = pings, Host = "127.0.0.1", Timeout = 50 }).Count;
            matches += ping.ping2(false, pings, 50, "127.0.0.1").Count;
            matches += ping.ping3(new { n = pings, Host = "127.0.0.1", w = 50 }).Count;
            matches += ping.ping4(new Dictionary<string, object>() { { "n", pings }, { "Host", "127.0.0.1" }, { "w", 50 } }).Count;
            Assert.Equal(pings * 4, matches);
            Assert.True(ping.SinglePing("127.0.0.1").Success);
            Assert.NotNull(ping.SinglePingExecution("127.0.0.1"));
        }

        [Executable("ping")]
        public abstract class Ping : IExecutable
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
        }

        public class PingArguments
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

        public class PingResult
        {
            public string ip { get; set; }
            public string fail { get; set; }
            public bool Success => !string.IsNullOrEmpty(ip) && string.IsNullOrEmpty(fail);
            public int bytes { get; set; }
            public TimeSpan? time { get; set; }
            public int ttl { get; set; }
        }
    }
}
