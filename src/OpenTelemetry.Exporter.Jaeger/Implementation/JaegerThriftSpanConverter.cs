// <copyright file="JaegerThriftSpanConverter.cs" company="OpenTelemetry Authors">
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
    using System.Collections.Generic;
    using Jaeger.Thrift;
    using OpenTelemetry.Trace;
    using OpenTelemetry.Trace.Export;


    internal static class JaegerThriftSpanConverter
    {
        public static Span ConvertSpan(SpanData span)
        {
            var context = span.Context;
            var startTimeMicros = span.StartTimestamp.Nanos / 1000L;
            var endTimeMicros = span.EndTimestamp?.Nanos / 1000L;

            var durationMicros = (endTimeMicros - startTimeMicros) ?? 0L;

            var traceIdParts = SplitTraceId(context.TraceId);

            var thriftSpan = new Span(
                traceIdParts.lower,
                traceIdParts.higher,
                ToLong(context.SpanId),
                ToLong(span.ParentSpanId),
                span.Name,
                context.TraceOptions.Bytes[TraceOptions.Size], // Flags
                startTimeMicros,
                durationMicros)
            {
                References = BuildReferences(span.Links),
                Tags = BuildTags(span.Attributes),
            };

            return thriftSpan;
        }


        public static List<Tag> BuildTags(IAttributes attributes)
        {
            var tags = new List<Tag>();

            foreach (var attribute in attributes.AttributeMap)
            {
                tags.Add(attribute.Value.Match(
                    s => new Tag(attribute.Key, TagType.STRING) { VStr = s },
                    b => new Tag(attribute.Key, TagType.BOOL) { VBool = b },
                    l => new Tag(attribute.Key, TagType.LONG) { VLong = l },
                    d => new Tag(attribute.Key, TagType.DOUBLE) { VDouble = d },
                    o => new Tag(attribute.Key, TagType.STRING) { VStr = o.ToString() }));
            }

            return tags;
        }

        private static List<SpanRef> BuildReferences(ILinks links)
        {
            var refs = new List<SpanRef>();

            foreach (var link in links.Links)
            {
                var traceIdParts = SplitTraceId(link.Context.TraceId);

                refs.Add(new SpanRef(
                    SpanRefType.FOLLOWS_FROM, // Is this right?
                    traceIdParts.lower,
                    traceIdParts.higher,
                    BitConverter.ToInt64(link.Context.SpanId.Bytes, 0)));
            }

            return refs;
        }

        private static (long higher, long lower) SplitTraceId(TraceId traceId)
        {
            var traceIdBytes = traceId.Bytes;

            return (BitConverter.ToInt64(traceIdBytes, 0),
                    BitConverter.ToInt64(traceIdBytes, 8));

        }

        private static long ToLong(SpanId spanId)
        {
            return BitConverter.ToInt64(spanId.Bytes, 0);
        }
    }
}
