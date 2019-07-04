// <copyright file="HttpSender.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Exporter.Jaeger.Implementation.Sender
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Jaeger.Thrift;
    using OpenTelemetry.Exporter.Jaeger.Configuration;
    using Thrift.Protocols;
    using Thrift.Transports.Client;
    using static global::Jaeger.Thrift.Senders.ThriftSenderBase;

    internal class HttpSender : ThriftSender
    {
        private const string HttpCollectorJaegerThriftFormatParam = "format=jaeger.thrift";
        private const int OneMbInBytes = 1048576;

        private readonly TProtocol protocol;
        private readonly THttpClientTransport transport;

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
                Query = HttpCollectorJaegerThriftFormatParam,
            }.Uri;

            var customHeaders = new Dictionary<string, string>();
            if (builder.AuthenticationHeaderValue != null)
            {
                customHeaders.Add("Authorize", builder.AuthenticationHeaderValue.ToString());
            }

            this.transport = new THttpClientTransport(collectorUri, customHeaders);
            this.protocol = this.ProtocolFactory.GetProtocol(this.transport);
        }

        public override async Task<int> CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await base.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                this.transport.Close();
            }
        }

        public override string ToString()
        {
            return $"{nameof(HttpSender)}";
        }

        protected override async Task SendAsync(Process process, List<Span> spans, CancellationToken cancellationToken)
        {
            try
            {
                var batch = new Batch(process, spans);
                await batch.WriteAsync(this.protocol, cancellationToken).ConfigureAwait(false);
                await this.protocol.Transport.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new SenderException($"Could not send {spans.Count} spans", ex, spans.Count);
            }
        }

        public sealed class Builder
        {
            public Builder(string processName, string endpoint)
            {
                this.ProcessName = processName;
                this.Endpoint = endpoint;
            }

            public string ProcessName { get; }

            internal string Endpoint { get; }

            internal int MaxPacketSize { get; private set; } = OneMbInBytes;

            internal AuthenticationHeaderValue AuthenticationHeaderValue { get; private set; }

            public Builder WithMaxPacketSize(int maxPacketSizeBytes)
            {
                this.MaxPacketSize = maxPacketSizeBytes;
                return this;
            }

            public Builder WithAuth(string username, string password)
            {
                string value = $"{username}:{password}";
                string encodedValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
                this.AuthenticationHeaderValue = new AuthenticationHeaderValue("Basic", encodedValue);
                return this;
            }

            public Builder WithAuth(string authToken)
            {
                this.AuthenticationHeaderValue = new AuthenticationHeaderValue("Bearer", authToken);
                return this;
            }

            public HttpSender Build()
            {
                return new HttpSender(this);
            }
        }
    }
}
