using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Backoffice.API.Telemetry
{

    public class Instrumentation : IDisposable
    {

        internal const string ActivitySourceName = "Backoffice.API";
        internal const string MeterName = "Backoffice.API";

        private readonly Meter meter;


        public Instrumentation()
        {
            string? version = "latest";
            this.ActivitySource = new ActivitySource(ActivitySourceName, version);
            this.meter = new Meter(MeterName, version);
            this.PublishedMessagesCounter = this.meter.CreateCounter<long>("published.messages", "count", "Number of published messages.");
        }

        public ActivitySource ActivitySource { get; }
        public Counter<long> PublishedMessagesCounter { get; }

        public void Dispose()
        {
            this.ActivitySource.Dispose();
            this.meter.Dispose();
        }

    }

}
