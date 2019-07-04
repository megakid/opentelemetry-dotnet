using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Export;

namespace OpenTelemetry.Exporter.Jaeger
{
    internal class TraceExporterHandler : IHandler
    {
        private IReporter reporter;

        public TraceExporterHandler(IReporter reporter)
        {
            this.reporter = reporter;
        }

        public Task ExportAsync(IEnumerable<SpanData> spanDataList)
        {
            foreach (var span in spanDataList)
                this.reporter.Report(span);

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            return this.reporter.CloseAsync(CancellationToken.None);
        }
    }
}
