﻿namespace Samples
{
    using OpenTelemetry.Exporter.Prometheus;
    using OpenTelemetry.Stats;
    using OpenTelemetry.Stats.Aggregations;
    using OpenTelemetry.Stats.Measures;
    using OpenTelemetry.Tags;
    using OpenTelemetry.Trace;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    internal class TestPrometheus
    {
        private static ITracer tracer = Tracing.Tracer;
        private static ITagger tagger = Tags.Tagger;

        private static IStatsRecorder statsRecorder = Stats.StatsRecorder;
        private static readonly IMeasureLong VideoSize = MeasureLong.Create("my.org/measure/video_size", "size of processed videos", "By");
        private static readonly TagKey FrontendKey = TagKey.Create("my.org/keys/frontend");

        private static long MiB = 1 << 20;

        private static readonly IViewName VideoSizeViewName = ViewName.Create("my.org/views/video_size");

        private static readonly IView VideoSizeView = View.Create(
            VideoSizeViewName,
            "processed video size over time",
            VideoSize,
            Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 16.0 * MiB, 256.0 * MiB })),
            new List<TagKey>() { FrontendKey });

        internal static object Run()
        {
            var exporter = new PrometheusExporter(
                new PrometheusExporterOptions()
                {
                    Url = "http://+:9184/metrics/"  // "+" is a wildcard used to listen to all hostnames
                },
                Stats.ViewManager);

            exporter.Start();

            try
            {
                var tagContextBuilder = tagger.CurrentBuilder.Put(FrontendKey, TagValue.Create("mobile-ios9.3.5"));

                Stats.ViewManager.RegisterView(VideoSizeView);

                var t = new Task(() =>
                {
                    var r = new Random();
                    var values = new byte[1];

                    while (true)
                    {
                        using (var scopedTags = tagContextBuilder.BuildScoped())
                        {
                            r.NextBytes(values);
                            statsRecorder.NewMeasureMap().Put(VideoSize, values[0] * MiB).Record();
                            Thread.Sleep(TimeSpan.FromSeconds(1));
                        }
                    }
                });
                t.Start();

                Console.WriteLine("Look at metrics in Prometetheus console!");
                Console.ReadLine();
            }
            finally
            {
                exporter.Stop();
            }

            return null;
        }
    }
}
