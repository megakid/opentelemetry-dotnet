// <copyright file="JaegerSpan.cs" company="OpenTelemetry Authors">
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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    internal class JaegerSpan
    {
        public string TraceId { get; set; }

        public string ParentId { get; set; }

        public string Id { get; set; }

        public JaegerSpanKind Kind { get; set; }

        public string Name { get; set; }

        public long Timestamp { get; set; }

        public long Duration { get; set; }

        public JaegerEndpoint LocalEndpoint { get; set; }

        public JaegerEndpoint RemoteEndpoint { get; set; }

        public List<JaegerAnnotation> Annotations { get; set; }

        public Dictionary<string, string> Tags { get; set; }

        public bool Debug { get; set; }

        public bool Shared { get; set; }
        public string ServiceName { get; internal set; }

        public static Builder NewBuilder()
        {
            return new Builder();
        }

        public class Builder
        {
            private readonly JaegerSpan result = new JaegerSpan();

            internal Builder TraceId(string val)
            {
                this.result.TraceId = val;
                return this;
            }

            internal Builder Id(string val)
            {
                this.result.Id = val;
                return this;
            }

            internal Builder ParentId(string val)
            {
                this.result.ParentId = val;
                return this;
            }

            internal Builder Kind(JaegerSpanKind val)
            {
                this.result.Kind = val;
                return this;
            }

            internal Builder Name(string val)
            {
                this.result.Name = val;
                return this;
            }

            internal Builder Timestamp(long val)
            {
                this.result.Timestamp = val;
                return this;
            }

            internal Builder Duration(long val)
            {
                this.result.Duration = val;
                return this;
            }

            internal Builder LocalEndpoint(JaegerEndpoint val)
            {
                this.result.LocalEndpoint = val;
                return this;
            }

            internal Builder RemoteEndpoint(JaegerEndpoint val)
            {
                this.result.RemoteEndpoint = val;
                return this;
            }

            internal Builder Debug(bool val)
            {
                this.result.Debug = val;
                return this;
            }

            internal Builder Shared(bool val)
            {
                this.result.Shared = val;
                return this;
            }

            internal Builder PutTag(string key, string value)
            {
                if (this.result.Tags == null)
                {
                    this.result.Tags = new Dictionary<string, string>();
                }

                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                this.result.Tags[key] = value ?? throw new ArgumentNullException(nameof(value));

                return this;
            }

            internal Builder AddAnnotation(long timestamp, string value)
            {
                if (this.result.Annotations == null)
                {
                    this.result.Annotations = new List<JaegerAnnotation>(2);
                }

                this.result.Annotations.Add(new JaegerAnnotation() { Timestamp = timestamp, Value = value });

                return this;
            }

            internal JaegerSpan Build()
            {
                if (this.result.TraceId == null)
                {
                    throw new ArgumentException("Trace ID should not be null");
                }

                if (this.result.Id == null)
                {
                    throw new ArgumentException("ID should not be null");
                }

                return this.result;
            }
        }
    }
}
