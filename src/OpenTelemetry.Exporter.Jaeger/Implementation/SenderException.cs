﻿// <copyright file="SenderException.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Exporter.Jaeger.Implementation
{
    using System;

    public class SenderException : Exception
    {
        public SenderException(string message, int droppedSpans)
            : this(message, null, droppedSpans)
        {
            this.DroppedSpanCount = droppedSpans;
        }

        public SenderException(string message, Exception innerException, int droppedSpans)
            : base(message, innerException)
        {
            this.DroppedSpanCount = droppedSpans;
        }

        public int DroppedSpanCount { get; }
    }
}
