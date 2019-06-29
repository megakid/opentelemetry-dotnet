// <copyright file="TraceExporterHandler.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Exporter.Jaeger.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using OpenTelemetry.Common;
    using OpenTelemetry.Trace;
    using OpenTelemetry.Trace.Export;

    internal class TraceExporterHandler : IHandler
    {
        private const long MillisPerSecond = 1000L;
        private const long NanosPerMillisecond = 1000 * 1000;
        private const long NanosPerSecond = NanosPerMillisecond * MillisPerSecond;

        private static readonly string StatusCode = "census.status_code";
        private static readonly string StatusDescription = "census.status_description";

        private readonly JaegerTraceExporterOptions options;
        private readonly IReporter reporter;

        public TraceExporterHandler(JaegerTraceExporterOptions options, IReporter reporter)
        {
            this.options = options;
            this.reporter = reporter;
        }

        public async Task ExportAsync(IEnumerable<SpanData> spanDataList)
        {
            var zipkinSpans = new List<JaegerSpan>();

            foreach (var data in spanDataList)
            {
                var zipkinSpan = this.GenerateSpan(data, this.localEndpoint);
                this.reporter.Report(spanData);
                zipkinSpans.Add(zipkinSpan);
            }

            try
            {
                
                await this.SendSpansAsync(zipkinSpans);
            }
            catch (Exception)
            {
                // Ignored
            }
        }

        internal JaegerSpan GenerateSpan(SpanData spanData, JaegerEndpoint localEndpoint)
        {
            // create a jaeger span context
            var spanContext = new global::Jaeger.SpanContext(
                this.EncodeTraceId(spanData.Context.TraceId),
                this.EncodeSpanId(spanData.Context.SpanId),
                this.EncodeSpanId(spanData.ParentSpanId ?? Trace.SpanId.Invalid),
                spanData.Context.TraceOptions.IsSampled ? SpanContextFlags.Sampled : SpanContextFlags.None);


            // create a span
            var span = (Span)new object();

            spanData.

            Span span = this.tracer
                .BuildSpan(spanData.Name)
                .AsChildOf(spanData.ParentSpanId)

            var context = spanData.Context;
            var startTimestamp = this.ToEpochMicroseconds(spanData.StartTimestamp);
            var endTimestamp = this.ToEpochMicroseconds(spanData.EndTimestamp);

            var spanBuilder =
                JaegerSpan.NewBuilder()
                    .TraceId(this.EncodeTraceId(context.TraceId))
                    .Id(this.EncodeSpanId(context.SpanId))
                    .Kind(this.ToSpanKind(spanData))
                    .Name(spanData.Name)
                    .Timestamp(this.ToEpochMicroseconds(spanData.StartTimestamp))
                    .Duration(endTimestamp - startTimestamp)
                    .LocalEndpoint(localEndpoint);

            if (spanData.ParentSpanId != null && spanData.ParentSpanId.IsValid)
            {
                spanBuilder.ParentId(this.EncodeSpanId(spanData.ParentSpanId));
            }

            foreach (var label in spanData.Attributes.AttributeMap)
            {
                spanBuilder.PutTag(label.Key, this.AttributeValueToString(label.Value));
            }

            var status = spanData.Status;

            if (status != null)
            {
                spanBuilder.PutTag(StatusCode, status.CanonicalCode.ToString());

                if (status.Description != null)
                {
                    spanBuilder.PutTag(StatusDescription, status.Description);
                }
            }

            foreach (var annotation in spanData.Events.Events)
            {
                spanBuilder.AddAnnotation(this.ToEpochMicroseconds(annotation.Timestamp), annotation.Event.Name);
            }

            return spanBuilder.Build();
        }

        private long ToEpochMicroseconds(Timestamp timestamp)
        {
            var nanos = (timestamp.Seconds * NanosPerSecond) + timestamp.Nanos;
            var micros = nanos / 1000L;
            return micros;
        }

        private string AttributeValueToString(IAttributeValue attributeValue)
        {
            return attributeValue.Match(
                (arg) => { return arg; },
                (arg) => { return arg.ToString(); },
                (arg) => { return arg.ToString(); },
                (arg) => { return arg.ToString(); },
                (arg) => { return null; });
        }

        private global::Jaeger.TraceId EncodeTraceId(Trace.TraceId traceId)
        {
            // var id = traceId.ToLowerBase16();

            // if (id.Length > 16 && this.options.UseShortTraceIds)
            // {
            //     id = id.Substring(id.Length - 16, 16);
            // }

            // TODO: extract high long properly.
            return global::Jaeger.TraceId.FromString(traceId.ToLowerBase16());
        }

        private global::Jaeger.SpanId EncodeSpanId(Trace.SpanId spanId)
        {
            return global::Jaeger.SpanId.FromString(spanId.ToLowerBase16());
        }

        private JaegerSpanKind ToSpanKind(SpanData spanData)
        {
            if (spanData.Kind == SpanKind.Server)
            {
                return JaegerSpanKind.SERVER;
            }
            else if (spanData.Kind == SpanKind.Client)
            {
                return JaegerSpanKind.CLIENT;
            }

            return JaegerSpanKind.CLIENT;
        }

        private Task SendSpansAsync(IEnumerable<JaegerSpan> spans)
        {
            var requestUri = this.options.Endpoint;
            var request = this.GetHttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = this.GetRequestContent(spans);
            return this.DoPost(this.httpClient, request);
        }

        private async Task DoPost(HttpClient client, HttpRequestMessage request)
        {
            using (var response = await client.SendAsync(request))
            {
                if (response.StatusCode != HttpStatusCode.OK &&
                    response.StatusCode != HttpStatusCode.Accepted)
                {
                    var statusCode = (int)response.StatusCode;
                }
            }
        }

        private HttpRequestMessage GetHttpRequestMessage(HttpMethod method, Uri requestUri)
        {
            var request = new HttpRequestMessage(method, requestUri);

            return request;
        }

        private HttpContent GetRequestContent(IEnumerable<JaegerSpan> toSerialize)
        {
            var content = string.Empty;
            try
            {
                content = JsonConvert.SerializeObject(toSerialize);
            }
            catch (Exception)
            {
                // Ignored
            }

            return new StringContent(content, Encoding.UTF8, "application/json");
        }

        private JaegerEndpoint GetLocalJaegerEndpoint()
        {
            var result = new JaegerEndpoint()
            {
                ServiceName = this.options.ServiceName,
            };

            var hostName = this.ResolveHostName();

            if (!string.IsNullOrEmpty(hostName))
            {
                result.Ipv4 = this.ResolveHostAddress(hostName, AddressFamily.InterNetwork);

                result.Ipv6 = this.ResolveHostAddress(hostName, AddressFamily.InterNetworkV6);
            }

            return result;
        }

        private string ResolveHostAddress(string hostName, AddressFamily family)
        {
            string result = null;

            try
            {
                var results = Dns.GetHostAddresses(hostName);

                if (results != null && results.Length > 0)
                {
                    foreach (var addr in results)
                    {
                        if (addr.AddressFamily.Equals(family))
                        {
                            var sanitizedAddress = new IPAddress(addr.GetAddressBytes()); // Construct address sans ScopeID
                            result = sanitizedAddress.ToString();

                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            return result;
        }

        private string ResolveHostName()
        {
            string result = null;

            try
            {
                result = Dns.GetHostName();

                if (!string.IsNullOrEmpty(result))
                {
                    var response = Dns.GetHostEntry(result);

                    if (response != null)
                    {
                        return response.HostName;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            return result;
        }
    }
}
