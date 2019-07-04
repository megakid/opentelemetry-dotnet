// <copyright file="JaegerTraceExporterOptions.cs" company="OpenTelemetry Authors">
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
    using System;

    /// <summary>
    /// Jaeger trace exporter options.
    /// </summary>
    public sealed class JaegerTraceExporterOptions
    {

        public const int DefaultMaxQueueSize = 100;
        public static readonly TimeSpan DefaultFlushInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the name of the service reporting telemetry.
        /// </summary>
        public string ServiceName { get; set; } = "Open Census Exporter";

        /// <summary>
        /// Gets or sets a value indicating whether short trace id should be used.
        /// </summary>
        public bool UseShortTraceIds { get; set; } = false;

        /// <summary>
        /// Gets or sets the transport options used to send the Jaeger spans.
        /// </summary>
        public JaegerTraceTransportOptions Transport { get; set; } = new AgentJaegerTraceTransportOptions();

        /// <summary>
        /// Gets or sets the flush interval for the jaeger spans.
        /// </summary>
        public TimeSpan FlushInterval { get; set; } = DefaultFlushInterval;

        /// <summary>
        /// Gets or sets the max queue size of the internal queue of spans yet to be flushed.
        /// </summary>
        public int MaxQueueSize { get; set; } = DefaultMaxQueueSize;
    }
}
