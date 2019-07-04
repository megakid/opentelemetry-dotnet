// <copyright file="AgentJaegerTraceTransportOptions.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Exporter.Jaeger.Configuration
{
    using System;

    public sealed class AgentJaegerTraceTransportOptions : JaegerTraceTransportOptions
    {
        public const string DefaultAgentUdpHost = "localhost";
        public const int DefaultAgentUdpCompactPort = 6831;

        /// <summary>
        /// Gets or sets the Jaeger agent UDP endpoint address.
        /// Defaults to "localhost".
        /// </summary>
        public string Host { get; set; } = DefaultAgentUdpHost;

        /// <summary>
        /// Gets or sets the UDP port used for transmitting Jaeger spans to the Jaeger agent.
        /// Defaults to 6831 - the port used for accepting jaeger.thrift messages
        /// https://www.jaegertracing.io/docs/getting-started/.
        /// </summary>
        public int Port { get; set; } = DefaultAgentUdpCompactPort;
    }
}
