// <copyright file="ThriftSender.cs" company="OpenTelemetry Authors">
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
    using global::Jaeger.Thrift;
    using global::Jaeger.Thrift.Senders;
    using OpenTelemetry.Trace;

    /// <summary>
    /// JaegerThriftTransport is the base class for transporting spans from C# into Jaeger.
    /// It provides the basic implementation of serializing spans but leaves the buffering
    /// and sending of the serialized spans to concrete implementations.
    /// </summary>
    internal abstract class ThriftSender : ThriftSenderBase, ISender
    {
        private readonly List<Span> spanBuffer = new List<Span>();
        private readonly string processName;

        private Process process;
        private int processBytesSize;
        private int byteBufferSize;

        protected ThriftSender(string processName, ProtocolType protocolType, int maxPacketSize)
            : base(protocolType, maxPacketSize)
        {
            this.processName = processName;
        }

        public async Task<int> AppendAsync(SpanData span, CancellationToken cancellationToken)
        {
            if (this.process == null)
            {
                this.process = new Process(this.processName);
                this.processBytesSize = this.CalculateProcessSize(this.process);
                this.byteBufferSize += this.processBytesSize;
            }

            var thriftSpan = JaegerThriftSpanConverter.ConvertSpan(span);
            var spanSize = this.CalculateSpanSize(thriftSpan);
            if (spanSize > this.MaxSpanBytes)
            {
                throw new SenderException($"ThriftSender received a span that was too large, size = {spanSize}, max = {this.MaxSpanBytes}", null, 1);
            }

            this.byteBufferSize += spanSize;
            if (this.byteBufferSize <= this.MaxSpanBytes)
            {
                this.spanBuffer.Add(thriftSpan);
                if (this.byteBufferSize < this.MaxSpanBytes)
                {
                    return 0;
                }
                return await this.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            int n;
            try
            {
                n = await this.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (SenderException ex)
            {
                // +1 for the span not submitted in the buffer above
                throw new SenderException(ex.Message, ex, ex.DroppedSpanCount + 1);
            }

            this.spanBuffer.Add(thriftSpan);
            this.byteBufferSize = this.processBytesSize + spanSize;
            return n;
        }

        protected int CalculateProcessSize(Process proc)
        {
            try
            {
                return this.GetSize(proc);
            }
            catch (Exception ex)
            {
                throw new SenderException("ThriftSender failed writing Process to memory buffer.", ex, 1);
            }
        }

        protected int CalculateSpanSize(Span span)
        {
            try
            {
                return this.GetSize(span);
            }
            catch (Exception ex)
            {
                throw new SenderException("ThriftSender failed writing Span to memory buffer.", ex, 1);
            }
        }

        protected abstract Task SendAsync(Process process, List<Span> spans, CancellationToken cancellationToken);

        public async Task<int> FlushAsync(CancellationToken cancellationToken)
        {
            if (this.spanBuffer.Count == 0)
            {
                return 0;
            }

            int n = this.spanBuffer.Count;
            try
            {
                await this.SendAsync(this.process, this.spanBuffer, cancellationToken).ConfigureAwait(false);
            }
            catch (SenderException ex)
            {
                throw new SenderException("Failed to flush spans.", ex, n);
            }
            finally
            {
                this.spanBuffer.Clear();
                this.byteBufferSize = this.processBytesSize;
            }
            return n;
        }

        public virtual Task<int> CloseAsync(CancellationToken cancellationToken)
        {
            return this.FlushAsync(cancellationToken);
        }

        public override string ToString()
        {
            return $"{nameof(ThriftSender)}(ProcessBytesSize={this.processBytesSize}, ByteBufferSize={this.byteBufferSize})";
        }
    }
}
