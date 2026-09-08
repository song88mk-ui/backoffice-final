using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Backoffice.Web.Telemetry
{

    public class Instrumentation : IDisposable
    {

        internal const string ActivitySourceName = "Backoffice.Web";
        internal const string MeterName = "Backoffice.Web";

        private readonly Meter meter;


        public Instrumentation()
        {
            string? version = "latest";
            this.ActivitySource = new ActivitySource(ActivitySourceName, version);
            this.meter = new Meter(MeterName, version);
            this.LoginSuccessCounter = this.meter.CreateCounter<long>("login.success", "count", "Number of success logins.");
            this.GetTableauUrlSuccessCounter = this.meter.CreateCounter<long>("tableau.geturl.success", "count", "Number of success get url requests.");
            this.GetTableauUrlFailureCounter = this.meter.CreateCounter<long>("tableau.geturl.failure", "count", "Number of failed get url requests.");
            this.TableauTrustedCallDurationHistogram = this.meter.CreateHistogram<double>("tableau.trusted.call.duration", "ms", "Call to trusted endpoint duration.");
            this.AuthLoginCallDurationHistogram = this.meter.CreateHistogram<double>("auth.login.call.duration", "ms", "Call to auth login api duration.");

        }

        public ActivitySource ActivitySource { get; }
        public Counter<long> LoginSuccessCounter { get; }
        public Counter<long> GetTableauUrlSuccessCounter { get; }
        public Counter<long> GetTableauUrlFailureCounter { get; }
        public Histogram<double> TableauTrustedCallDurationHistogram { get; }
        public Histogram<double> AuthLoginCallDurationHistogram { get; }

        public void Dispose()
        {
            this.ActivitySource.Dispose();
            this.meter.Dispose();
        }

    }

}
