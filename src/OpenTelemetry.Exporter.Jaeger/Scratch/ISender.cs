using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry.Exporter.Jaeger.Implementation;

namespace OpenTelemetry.Exporter.Jaeger
{
    internal interface ISender
    {
        Task<int> AppendAsync(JaegerSpan span, CancellationToken cancellationToken);

        Task<int> FlushAsync(CancellationToken cancellationToken);

        Task<int> CloseAsync(CancellationToken cancellationToken);
    }

}
