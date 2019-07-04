using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Exporter.Jaeger
{
    internal interface ISender
    {
        Task<int> AppendAsync(SpanData span, CancellationToken cancellationToken);

        Task<int> FlushAsync(CancellationToken cancellationToken);

        Task<int> CloseAsync(CancellationToken cancellationToken);
    }

}
