﻿// <copyright file="TracerBase.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Trace
{
    using System;
    using OpenTelemetry.Context;
    using OpenTelemetry.Context.Propagation;
    using OpenTelemetry.Trace.Internal;

    /// <inheritdoc/>
    public abstract class TracerBase : ITracer
    {
        private static readonly NoopTracer NoopTracerInstance = new NoopTracer();

        /// <inheritdoc/>
        public ISpan CurrentSpan
        {
            get
            {
                var currentSpan = CurrentSpanUtils.CurrentSpan;
                return currentSpan ?? BlankSpan.Instance;
            }
        }

        /// <inheritdoc/>
        public abstract IBinaryFormat BinaryFormat { get; }

        /// <inheritdoc/>
        public abstract ITextFormat TextFormat { get; }

        /// <summary>
        /// No-op tracer.
        /// </summary>
        internal static NoopTracer NoopTracer
        {
            get
            {
                return NoopTracerInstance;
            }
        }

        /// <inheritdoc/>
        public IScope WithSpan(ISpan span)
        {
            if (span == null)
            {
                throw new ArgumentNullException(nameof(span));
            }

            return CurrentSpanUtils.WithSpan(span, false);
        }

        /// <inheritdoc/>
        public ISpanBuilder SpanBuilder(string spanName, SpanKind spanKind = SpanKind.Internal)
        {
            return this.SpanBuilderWithParent(spanName, spanKind, CurrentSpanUtils.CurrentSpan);
        }

        /// <inheritdoc/>
        public abstract ISpanBuilder SpanBuilderWithParent(string spanName, SpanKind spanKind = SpanKind.Internal, ISpan parent = null);

        /// <inheritdoc/>
        public abstract ISpanBuilder SpanBuilderWithParentContext(string spanName, SpanKind spanKind = SpanKind.Internal, SpanContext remoteParentSpanContext = null);

        /// <inheritdoc/>
        public abstract void RecordSpanData(SpanData span);
    }
}
