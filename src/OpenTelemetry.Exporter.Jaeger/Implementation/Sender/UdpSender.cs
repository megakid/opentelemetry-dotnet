using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jaeger.Thrift;
using Jaeger.Thrift.Agent;
using Jaeger.Thrift.Senders;
using Jaeger.Thrift.Senders.Internal;
using OpenTelemetry.Exporter.Jaeger.Jaeger.Senders;

namespace OpenTelemetry.Exporter.Jaeger
{
    /// <inheritdoc />
    /// <summary>
    /// JaegerUdpTransport provides an implementation to transport spans over UDP using
    /// Compact Thrift. It handles making sure payloads efficiently use the UDP packet
    /// size by filling up as much of a UDP message it can before sending.
    /// </summary>
    internal class UdpSender : ThriftSender
    {

        private readonly Agent.Client _agentClient;
        private readonly ThriftUdpClientTransport _udpTransport;

        public UdpSender(string processName, AgentJaegerTraceTransportOptions options) 
            : base(processName, ProtocolType.Compact, options.MaxPacketSizeBytes)
        {
            _udpTransport = new ThriftUdpClientTransport(options.Host, options.Port);
            _agentClient = new Agent.Client(ProtocolFactory.GetProtocol(_udpTransport));
        }

        protected override async Task SendAsync(Process process, List<Span> spans, CancellationToken cancellationToken)
        {
            try
            {
                var batch = new Batch(process, spans);
                await _agentClient.emitBatchAsync(batch, cancellationToken).ConfigureAwait(false);
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
                _udpTransport.Close();
            }
        }

        public override string ToString()
        {
            return $"{nameof(UdpSender)}(UdpTransport={_udpTransport})";
        }
    }
}
