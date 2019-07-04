// <copyright file="HttpJaegerTraceTransportOptions.cs" company="OpenTelemetry Authors">
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

    public sealed class HttpJaegerTraceTransportOptions : JaegerTraceTransportOptions
    {
        /// <summary>
        /// Gets or sets Jaeger endpoint address.
        /// Typically something like http://jaeger-collector:14268/api/traces.
        /// </summary>
        public string HttpEndpoint { get; set; } = "http://jaeger-collector:14268/api/traces";

        /// <summary>
        /// Gets or sets the token to be used for the 'Bearer' when a HTTP call is made.  If non-null, BasicUser and BasicPassword will be ignored.
        /// </summary>
        public string BearerToken { get; set; }

        /// <summary>
        /// Gets or sets the user used for 'Basic Auth' when a HTTP call is made.  Ignored if BearerToken is non-null.
        /// </summary>
        public string BasicUser { get; set; }

        /// <summary>
        /// Gets or sets the password used for 'Basic Auth' when a HTTP call is made.  Ignored if BearerToken is non-null.
        /// </summary>
        public string BasicPassword { get; set; }
    }
}
