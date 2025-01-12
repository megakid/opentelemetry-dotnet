﻿// <copyright file="RemoteReporter.cs" company="OpenTelemetry Authors">
// Copyright 2018 (c) The Jaeger Authors.
// Copyright 2018 (c) Chatham Financial Corp.
// Copyright 2019, OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenTelemetry.Exporter.Jaeger.Implementation.Reporter
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using OpenTelemetry.Exporter.Jaeger.Configuration;
    using OpenTelemetry.Exporter.Jaeger.Implementation.Sender;
    using OpenTelemetry.Trace;

    /// <summary>
    /// <see cref="RemoteReporter"/> buffers spans in memory and sends them out of process using <see cref="ISender"/>.
    /// </summary>
    internal partial class RemoteReporter : IReporter
    {
        private readonly BlockingCollection<ICommand> commandQueue;
        private readonly Task queueProcessorTask;
        private readonly TimeSpan flushInterval;
        private readonly Task flushTask;
        private readonly ISender sender;

        internal RemoteReporter(ISender sender, TimeSpan flushInterval, int maxQueueSize)
        {
            this.sender = sender;
            this.commandQueue = new BlockingCollection<ICommand>(maxQueueSize);

            // start a thread to append spans
            this.queueProcessorTask = Task.Factory.StartNew(this.ProcessQueueLoop, TaskCreationOptions.LongRunning);

            this.flushInterval = flushInterval;
            this.flushTask = Task.Factory.StartNew(this.FlushLoop, TaskCreationOptions.LongRunning);
        }

        public void Report(SpanData span)
        {
            bool added = false;
            try
            {
                // It's better to drop spans, than to block here
                added = this.commandQueue.TryAdd(new AppendCommand(this, span));
            }
            catch (InvalidOperationException)
            {
                // The queue has been marked as IsAddingCompleted -> no-op.
            }
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            // Note: Java creates a CloseCommand but we have CompleteAdding() in C# so we don't need the command.
            // (This also stops the FlushLoop)
            this.commandQueue.CompleteAdding();

            try
            {
                // Give processor some time to process any queued commands.

                var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    new CancellationTokenSource(10000).Token);
                var cancellationTask = Task.Delay(Timeout.Infinite, cts.Token);

                await Task.WhenAny(this.queueProcessorTask, cancellationTask);
            }
            catch (OperationCanceledException ex)
            {
                // _logger.LogError(ex, "Dispose interrupted");
            }
            finally
            {
                try
                {
                    // _metrics.ReporterSuccess.Inc(n);
                    int n = await this.sender.CloseAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (SenderException ex)
                {
                    // _metrics.ReporterFailure.Inc(ex.DroppedSpanCount);
                }
            }
        }

        public override string ToString()
        {
            return $"{nameof(RemoteReporter)}(Sender={this.sender})";
        }

        private void Flush()
        {
            // to reduce the number of updateGauge stats, we only emit queue length on flush
            // _metrics.ReporterQueueLength.Update(_commandQueue.Count);

            try
            {
                // We can safely drop FlushCommand when the queue is full - sender should take care of flushing
                // in such case
                this.commandQueue.TryAdd(new FlushCommand(this));
            }
            catch (InvalidOperationException)
            {
                // The queue has been marked as IsAddingCompleted -> no-op.
            }
        }

        private async Task FlushLoop()
        {
            // First flush should happen later so we start with the delay
            do
            {
                await Task.Delay(this.flushInterval).ConfigureAwait(false);
                this.Flush();
            }
            while (!this.commandQueue.IsAddingCompleted);
        }

        private async Task ProcessQueueLoop()
        {
            // This blocks until a command is available or IsCompleted=true
            foreach (ICommand command in this.commandQueue.GetConsumingEnumerable())
            {
                try
                {
                    await command.ExecuteAsync().ConfigureAwait(false);
                }
                catch (SenderException ex)
                {
                    // _metrics.ReporterFailure.Inc(ex.DroppedSpanCount);
                }
                catch (Exception ex)
                {
                    // _logger.LogError(ex, "QueueProcessor error");
                    // Do nothing, and try again on next command.
                }
            }
        }

        public sealed class Builder
        {
            private JaegerTraceExporterOptions options;

            public Builder(JaegerTraceExporterOptions options)
            {
                this.options = options;
            }

            public RemoteReporter Build()
            {
                ISender sender;
                switch (this.options.Transport)
                {
                    case AgentJaegerTraceTransportOptions agentOptions:
                        sender = new UdpSender(this.options.ServiceName, agentOptions);
                        break;
                    case HttpJaegerTraceTransportOptions httpOptions:
                        sender = new HttpSender(this.options.ServiceName, httpOptions);
                        break;
                    default:
                        throw new InvalidOperationException("Unhandled Transport Options type");
                }

                return new RemoteReporter(sender, this.options.FlushInterval, this.options.MaxQueueSize); // , _metrics, _loggerFactory);
            }
        }
    }
}
