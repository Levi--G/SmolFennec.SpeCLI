﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SpeCLI
{
    public class Execution
    {
        public event EventHandler PreStarted;
        public event EventHandler Started;
        public event EventHandler Exited;
        public event DataReceivedEventHandler ErrorDataReceived;
        public event DataReceivedEventHandler OutputDataReceived;
        public event EventHandler<object> OnOutput;
        public event EventHandler<Exception> OnError;

        public IOutputProcessor OutputProcessor { get; private set; }

        public Process Process { get; set; }

        public void Start()
        {
            PreStarted?.Invoke(this, EventArgs.Empty);
            Process.Start();
            if (Process.StartInfo.RedirectStandardOutput == true)
            {
                Process.BeginOutputReadLine();
            }
            if (Process.StartInfo.RedirectStandardError == true)
            {
                Process.BeginErrorReadLine();
            }
            Started?.Invoke(this, EventArgs.Empty);
        }

        public void Kill()
        {
            Process.Kill();
        }

        public Execution WithRedirection()
        {
            Process.StartInfo.UseShellExecute = false;
            Process.StartInfo.RedirectStandardOutput = true;
            Process.StartInfo.RedirectStandardError = true;
            Process.StartInfo.RedirectStandardInput = true;
            Process.OutputDataReceived += Process_OutputDataReceived;
            Process.ErrorDataReceived += Process_ErrorDataReceived;
            Process.EnableRaisingEvents = true;
            Process.Exited += Process_Exited;
            return this;
        }

        public Execution ProcessWith(IOutputProcessor outputProcessor)
        {
            if (outputProcessor != null)
            {
                WithRedirection();
                OutputProcessor = outputProcessor;
                PreStarted += (s, e) => outputProcessor.PreExecutionStarted(this);
                Started += (s, e) => outputProcessor.ExecutionStarted(this);
                Exited += (s, e) => outputProcessor.ExecutionEnded(this);
            }
            return this;
        }

        public void SendInput(string tosend)
        {
            Process.StandardInput.Write(tosend);
        }

        public void SendInputLine(string tosend)
        {
            Process.StandardInput.WriteLine(tosend);
        }

        public void WaitForExit()
        {
            Process.WaitForExit();
        }

        public Task WaitForExitAsync()
        {
            var tcs = new TaskCompletionSource<object>();
            Exited += (s, e) => tcs.SetResult(null);
            return tcs.Task;
        }

        public List<T> ParseAsList<T>()
        {
            var l = new List<T>();
            Exception ex = null;
            OnOutput += (s, o) =>
            {
                if (o is T t)
                {
                    l.Add(t);
                }
            };
            OnError += (s, e) =>
            {
                ex ??= e;
            };
            Start();
            WaitForExit();
            if (ex != null)
            {
                throw ex;
            }
            return l;
        }

        public Task<List<T>> ParseAsListAsync<T>()
        {
            return Task.Factory.StartNew(() => ParseAsList<T>());
        }

        public IAsyncEnumerable<T> ParseAsIAsyncEnumerable<T>()
        {
            var buffer = Channel.CreateUnbounded<T>();
            OnOutput += async (_, o) =>
            {
                if (o is T t)
                {
                    await buffer.Writer.WriteAsync(t);
                }
            };
            Start();
            CompleteBufferWhenEventsAreDone();
            return buffer.Reader.ReadAllAsync();

            async void CompleteBufferWhenEventsAreDone()
            {
                await WaitForExitAsync();
                buffer.Writer.TryComplete();
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            Exited?.Invoke(sender, e);
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                ErrorDataReceived?.Invoke(sender, e);
                Output(OutputProcessor?.ParseError(this, e.Data));
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                OutputDataReceived?.Invoke(sender, e);
                Output(OutputProcessor?.ParseOutput(this, e.Data));
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
            }
        }

        void Output(IEnumerable<object> objects)
        {
            if (objects == null)
            {
                return;
            }
            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    OnOutput?.Invoke(this, obj);
                }
            }
        }
    }
}
