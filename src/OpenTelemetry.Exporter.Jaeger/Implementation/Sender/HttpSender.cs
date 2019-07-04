using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jaeger.Thrift;
using Thrift.Protocols;
using Thrift.Transports.Client;

namespace OpenTelemetry.Exporter.Jaeger
{

    namespace Jaeger.Senders
    {
        internal class HttpSender : ThriftSender
        {
            private const string HttpCollectorJaegerThriftFormatParam = "format=jaeger.thrift";
            private const int OneMbInBytes = 1048576;

            private readonly TProtocol _protocol;
            private readonly THttpClientTransport _transport;

            public HttpSender(string processName, HttpJaegerTraceTransportOptions options)
                : this(options.BearerToken == null 
                      ? new Builder(processName, options.HttpEndpoint)
                        .WithMaxPacketSize(options.MaxPacketSizeBytes)
                        .WithAuth(options.BasicUser, options.BasicPassword)
                      : new Builder(processName, options.HttpEndpoint)
                        .WithMaxPacketSize(options.MaxPacketSizeBytes)
                        .WithAuth(options.BearerToken))
            {
            }

            private HttpSender(Builder builder)
                : base(builder.ProcessName, ProtocolType.Binary, builder.MaxPacketSize)
            {
                Uri collectorUri = new UriBuilder(builder.Endpoint)
                {
                    Query = HttpCollectorJaegerThriftFormatParam
                }.Uri;

                var customHeaders = new Dictionary<string, string>();
                if (builder.AuthenticationHeaderValue != null)
                {
                    customHeaders.Add("Authorize", builder.AuthenticationHeaderValue.ToString());
                }

                _transport = new THttpClientTransport(collectorUri, customHeaders);
                _protocol = ProtocolFactory.GetProtocol(_transport);
            }

            protected override async Task SendAsync(Process process, List<Span> spans, CancellationToken cancellationToken)
            {
                try
                {
                    var batch = new Batch(process, spans);
                    await batch.WriteAsync(_protocol, cancellationToken).ConfigureAwait(false);
                    await _protocol.Transport.FlushAsync(cancellationToken).ConfigureAwait(false);
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
                    _transport.Close();
                }
            }

            public override string ToString()
            {
                return $"{nameof(HttpSender)}";
            }

            public sealed class Builder
            {
                public string ProcessName { get; }
                internal string Endpoint { get; }
                internal int MaxPacketSize { get; private set; } = OneMbInBytes;
                internal AuthenticationHeaderValue AuthenticationHeaderValue { get; private set; }

                public Builder(string processName, string endpoint)
                {
                    ProcessName = processName;
                    Endpoint = endpoint;
                }

                public Builder WithMaxPacketSize(int maxPacketSizeBytes)
                {
                    MaxPacketSize = maxPacketSizeBytes;
                    return this;
                }

                public Builder WithAuth(string username, string password)
                {
                    string value = $"{username}:{password}";
                    string encodedValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
                    AuthenticationHeaderValue = new AuthenticationHeaderValue("Basic", encodedValue);
                    return this;
                }

                public Builder WithAuth(string authToken)
                {
                    AuthenticationHeaderValue = new AuthenticationHeaderValue("Bearer", authToken);
                    return this;
                }

                public HttpSender Build()
                {
                    return new HttpSender(this);
                }
            }
        }
    }

}



