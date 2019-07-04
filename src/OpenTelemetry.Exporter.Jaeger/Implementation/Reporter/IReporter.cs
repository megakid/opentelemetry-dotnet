using System.Threading;
using System.Threading.Tasks;
using Jaeger.Thrift;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Exporter.Jaeger
{
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



