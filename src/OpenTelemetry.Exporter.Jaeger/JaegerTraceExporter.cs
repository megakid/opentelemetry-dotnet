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

namespace OpenTelemetry.Exporter.Jaeger
{
    using System.Net.Http;
    using global::Jaeger.Metrics;
    using global::Jaeger.Reporters;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using OpenTelemetry.Exporter.Jaeger.Implementation;
    using OpenTelemetry.Trace.Export;
    using static global::Jaeger.Configuration;

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

                this.handler = new TraceExporterHandler(this.options, reporter);

                this.exportComponent.SpanExporter.RegisterHandler(ExporterName, this.handler);
            }
        }

        private IReporter CreateJaegerReporter()
        {
            var senderConfig = new SenderConfiguration(NullLoggerFactory.Instance);

            switch (this.options.Transport)
            {
                case AgentJaegerTraceTransportOptions agentOptions:

                    senderConfig = senderConfig
                        .WithAgentHost(agentOptions.Host)
                        .WithAgentPort(agentOptions.Port);

                    break;
                case HttpJaegerTraceTransportOptions httpOptions:

                    senderConfig = senderConfig
                        .WithEndpoint(httpOptions.HttpEndpoint);

                    if (httpOptions.BearerToken != null)
                    {
                        senderConfig = senderConfig.WithAuthToken(httpOptions.BearerToken);
                    }
                    else
                    {
                        senderConfig = senderConfig
                            .WithAuthUsername(httpOptions.BasicUser)
                            .WithAuthPassword(httpOptions.BasicPassword);
                    }

                    break;
            }

            var reporter = new RemoteReporter.Builder()
                .WithFlushInterval(this.options.FlushInterval)
                .WithMaxQueueSize(this.options.MaxQueueSize)
                .WithSender(senderConfig.GetSender());

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

                this.handler = null;
            }
        }
    }
}
