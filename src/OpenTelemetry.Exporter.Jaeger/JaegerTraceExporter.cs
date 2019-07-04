// <copyright file="JaegerTraceExporter.cs" company="OpenTelemetry Authors">
// Copyright 2018, OpenTelemetry Authors
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

using OpenTelemetry.Trace.Export;

namespace OpenTelemetry.Exporter.Jaeger
{
    /// <summary>
    /// Exporter of Open Census traces to Jaeger.
    /// </summary>
    public class JaegerTraceExporter
    {
        private const string ExporterName = "JaegerTraceExporter";

        private readonly JaegerTraceExporterOptions options;

        private readonly IExportComponent exportComponent;

        private readonly object lck = new object();

        private TraceExporterHandler handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="JaegerTraceExporter"/> class.
        /// This exporter sends Open Census traces to Jaeger.
        /// </summary>
        /// <param name="options">Jaeger exporter configuration options.</param>
        /// <param name="exportComponent">Exporter to get traces from.</param>
        public JaegerTraceExporter(JaegerTraceExporterOptions options, IExportComponent exportComponent)
        {
            this.options = options;

            this.exportComponent = exportComponent;
        }

        /// <summary>
        /// Start exporter.
        /// </summary>
        public void Start()
        {
            lock (this.lck)
            {
                if (this.handler != null)
                {
                    return;
                }

                var reporter = this.CreateJaegerReporter();

                this.handler = new TraceExporterHandler(reporter);

                this.exportComponent.SpanExporter.RegisterHandler(ExporterName, this.handler);
            }
        }

        private IReporter CreateJaegerReporter()
        {
            var reporter = new RemoteReporter.Builder(this.options)
                .Build();

            return reporter;
        }

        /// <summary>
        /// Stop exporter.
        /// </summary>
        public void Stop()
        {
            lock (this.lck)
            {
                if (this.handler == null)
                {
                    return;
                }

                this.exportComponent.SpanExporter.UnregisterHandler(ExporterName);

                this.handler.StopAsync().GetAwaiter().GetResult();
                this.handler = null;
            }
        }
    }
}
