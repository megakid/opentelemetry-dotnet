using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry.Exporter.Jaeger.Jaeger.Senders;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Exporter.Jaeger
{
    /// <summary>
    /// <see cref="RemoteReporter"/> buffers spans in memory and sends them out of process using <see cref="ISender"/>.
    /// </summary>
    internal class RemoteReporter : IReporter
    {
        private readonly BlockingCollection<ICommand> _commandQueue;
        private readonly Task _queueProcessorTask;
        private readonly TimeSpan _flushInterval;
        private readonly Task _flushTask;
        private readonly ISender _sender;

        internal RemoteReporter(ISender sender, TimeSpan flushInterval, int maxQueueSize)
        {
            _sender = sender;
            _commandQueue = new BlockingCollection<ICommand>(maxQueueSize);

            // start a thread to append spans
            _queueProcessorTask = Task.Factory.StartNew(ProcessQueueLoop, TaskCreationOptions.LongRunning);

            _flushInterval = flushInterval;
            _flushTask = Task.Factory.StartNew(FlushLoop, TaskCreationOptions.LongRunning);
        }

        public void Report(SpanData span)
        {
            bool added = false;
            try
            {
                // It's better to drop spans, than to block here
                added = _commandQueue.TryAdd(new AppendCommand(this, span));
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
            _commandQueue.CompleteAdding();

            try
            {
                // Give processor some time to process any queued commands.

                var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    new CancellationTokenSource(10000).Token);
                var cancellationTask = Task.Delay(Timeout.Infinite, cts.Token);

                await Task.WhenAny(_queueProcessorTask, cancellationTask);
            }
            catch (OperationCanceledException ex)
            {
                //_logger.LogError(ex, "Dispose interrupted");
            }
            finally
            {
                try
                {
                    int n = await _sender.CloseAsync(cancellationToken).ConfigureAwait(false);
                    //_metrics.ReporterSuccess.Inc(n);
                }
                catch (SenderException ex)
                {
                    //_metrics.ReporterFailure.Inc(ex.DroppedSpanCount);
                }
            }
        }

        internal void Flush()
        {
            // to reduce the number of updateGauge stats, we only emit queue length on flush
            //_metrics.ReporterQueueLength.Update(_commandQueue.Count);

            try
            {
                // We can safely drop FlushCommand when the queue is full - sender should take care of flushing
                // in such case
                _commandQueue.TryAdd(new FlushCommand(this));
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
                await Task.Delay(_flushInterval).ConfigureAwait(false);
                Flush();
            }
            while (!_commandQueue.IsAddingCompleted);
        }

        private async Task ProcessQueueLoop()
        {
            // This blocks until a command is available or IsCompleted=true
            foreach (ICommand command in _commandQueue.GetConsumingEnumerable())
            {
                try
                {
                    await command.ExecuteAsync().ConfigureAwait(false);
                }
                catch (SenderException ex)
                {
                    //_metrics.ReporterFailure.Inc(ex.DroppedSpanCount);
                }
                catch (Exception ex)
                {
                    //_logger.LogError(ex, "QueueProcessor error");
                    // Do nothing, and try again on next command.
                }
            }
        }

        public override string ToString()
        {
            return $"{nameof(RemoteReporter)}(Sender={_sender})";
        }

        /*
         * The code below implements the command pattern. This pattern is useful for
         * situations where multiple threads would need to synchronize on a resource,
         * but are fine with executing sequentially. The advantage is simplified code where
         * tasks are put onto a blocking queue and processed sequentially by another thread.
         */
        public interface ICommand
        {
            Task ExecuteAsync();
        }

        class AppendCommand : ICommand
        {
            private readonly RemoteReporter _reporter;
            private readonly SpanData _span;

            public AppendCommand(RemoteReporter reporter, SpanData span)
            {
                _reporter = reporter;
                _span = span;
            }

            public Task ExecuteAsync()
            {
                return _reporter._sender.AppendAsync(_span, CancellationToken.None);
            }
        }

        class FlushCommand : ICommand
        {
            private readonly RemoteReporter _reporter;

            public FlushCommand(RemoteReporter reporter)
            {
                _reporter = reporter;
            }

            public async Task ExecuteAsync()
            {
                int n = await _reporter._sender.FlushAsync(CancellationToken.None).ConfigureAwait(false);
                //_reporter._metrics.ReporterSuccess.Inc(n);
            }
        }

        public sealed class Builder
        {
            private ISender _sender;
            //private IMetrics _metrics;
            //private ILoggerFactory _loggerFactory;
            private JaegerTraceExporterOptions _options;
            private readonly string _processName;

            public Builder(string processName)
            {
                this._processName = processName;
            }

            public Builder(JaegerTraceExporterOptions options)
                : this(options.ServiceName)
            {
                this._options = options;
            }

            public RemoteReporter Build()
            {
                switch (this._options.Transport)
                {
                    case AgentJaegerTraceTransportOptions agentOptions:
                        _sender = new UdpSender(this._options.ServiceName, agentOptions);
                        break;
                    case HttpJaegerTraceTransportOptions httpOptions:
                        _sender = new HttpSender(this._options.ServiceName, httpOptions);
                        break;
                }

                return new RemoteReporter(_sender, _options.FlushInterval, _options.MaxQueueSize);//, _metrics, _loggerFactory);
            }
        }
    }
}
