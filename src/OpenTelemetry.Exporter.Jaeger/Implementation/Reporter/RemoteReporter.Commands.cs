// <copyright file="RemoteReporter.Commands.cs" company="OpenTelemetry Authors">
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
    using System.Threading;
    using System.Threading.Tasks;
    using OpenTelemetry.Trace;

    /// <summary>
    /// Holds the internal classes
    /// </summary>
    internal partial class RemoteReporter
    {
        /*
         * The code below implements the command pattern. This pattern is useful for
         * situations where multiple threads would need to synchronize on a resource,
         * but are fine with executing sequentially. The advantage is simplified code where
         * tasks are put onto a blocking queue and processed sequentially by another thread.
         */
        private interface ICommand
        {
            Task ExecuteAsync();
        }

        private class AppendCommand : ICommand
        {
            private readonly RemoteReporter reporter;
            private readonly SpanData span;

            public AppendCommand(RemoteReporter reporter, SpanData span)
            {
                this.reporter = reporter;
                this.span = span;
            }

            public Task ExecuteAsync()
            {
                return this.reporter.sender.AppendAsync(this.span, CancellationToken.None);
            }
        }

        private class FlushCommand : ICommand
        {
            private readonly RemoteReporter reporter;

            public FlushCommand(RemoteReporter reporter)
            {
                this.reporter = reporter;
            }

            public async Task ExecuteAsync()
            {
                // _reporter._metrics.ReporterSuccess.Inc(n);
                int n = await this.reporter.sender.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
