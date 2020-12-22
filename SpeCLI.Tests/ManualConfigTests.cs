using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace SpeCLI.Tests
{
    public class ManualConfigTests
    {
        private const string ExePath = "ExePath";
        private const string CmdName = "CommandName";

        [Fact]
        public void ExecutablePath()
        {
            var e = new Executable(ExePath);
            e.Add(CmdName);
            Assert.Equal(ExePath, e.Path);
            Assert.Equal(ExePath, e.CreateExecution(CmdName).Process.StartInfo.FileName);
        }

        [Fact]
        public void CommandName()
        {
            var e = new Executable(ExePath);
            e.Add(CmdName);
            Assert.True(e.TryGetCommand(CmdName, out var c));
            Assert.NotNull(c);
            Assert.Equal(ExePath, e.CreateExecution(CmdName).Process.StartInfo.FileName);
        }

        [Fact]
        public void SwitchParameter()
        {
            var e = new Executable(ExePath);
            e.DefaultParameterPrefix = @"/";
            e.Add(CmdName).AddParameter(new Switch("a")).AddSwitch("b").AddSwitch("c", true);
            Assert.Equal("-a /c", e.CreateExecution(CmdName, new { a = true }).Process.StartInfo.Arguments);
            Assert.Equal("/b", e.CreateExecution(CmdName, new { a = false, b = true, c = false }).Process.StartInfo.Arguments);
        }

        [Fact]
        public void StringParameter()
        {
            var e = new Executable(ExePath);
            e.DefaultParameterPrefix = @"/";
            e.Add(CmdName).AddParameter(new Parameter("aa")).AddParameter("b", typeof(int)).AddParameter("c", 5d);
            Assert.Equal("--aa A /c 5", e.CreateExecution(CmdName, new { aa = "A", b = 0 }).Process.StartInfo.Arguments);
            Assert.Equal("/c 6", e.CreateExecution(CmdName, new { b = true, c = 6d }).Process.StartInfo.Arguments);
        }
    }
}
