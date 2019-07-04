﻿// <copyright file="IReporter.cs" company="OpenTelemetry Authors">
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
    using System.Threading;
    using System.Threading.Tasks;
    using Jaeger.Thrift;
    using OpenTelemetry.Trace;

    //
    // Summary:
    //     /// Jaeger.Reporters.IReporter is the interface Jaeger.Tracer uses to report
    //     finished spans to something that /// collects those spans. Default implementation
    //     is Jaeger.Reporters.RemoteReporter that sends spans out of process. ///
    internal interface IReporter
    {
        void Report(SpanData span);

        //
        // Summary:
        //     /// Release any resources used by the reporter. ///
        //
        // Remarks:
        //     /// We don't use System.IDisposable because the Jaeger.Tracer should /// be able
        //     to close the reporter. If we would use System.IDisposable then /// the Jaeger.Tracer
        //     would call Dispose on a member it did not create itself. ///
        Task CloseAsync(CancellationToken cancellationToken);
    }
}


