// <copyright file="UdpSender.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Exporter.Jaeger
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Jaeger.Thrift;
    using Jaeger.Thrift.Agent;
    using Jaeger.Thrift.Senders;
    using Jaeger.Thrift.Senders.Internal;
    using OpenTelemetry.Exporter.Jaeger.Jaeger.Senders;

    /// <inheritdoc />
    /// <summary>
    /// JaegerUdpTransport provides an implementation to transport spans over UDP using
    /// Compact Thrift. It handles making sure payloads efficiently use the UDP packet
    /// size by filling up as much of a UDP message it can before sending.
    /// </summary>
    internal class UdpSender : ThriftSender
    {

        private readonly Agent.Client agentClient;
        private readonly ThriftUdpClientTransport udpTransport;

        public UdpSender(string processName, AgentJaegerTraceTransportOptions options) 
            : base(processName, ProtocolType.Compact, options.MaxPacketSizeBytes)
        {
            this.udpTransport = new ThriftUdpClientTransport(options.Host, options.Port);
            this.agentClient = new Agent.Client(this.ProtocolFactory.GetProtocol(this.udpTransport));
        }

        protected override async Task SendAsync(Process process, List<Span> spans, CancellationToken cancellationToken)
        {
            try
            {
                var batch = new Batch(process, spans);
                await this.agentClient.emitBatchAsync(batch, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new SenderException($"Could not send {spans.Count} spans", ex, spans.Count);
            }
        }

        public override async Task<int> CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await base.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                this.udpTransport.Close();
            }
        }

        public override string ToString()
        {
            return $"{nameof(UdpSender)}(UdpTransport={this.udpTransport})";
        }
    }
}
