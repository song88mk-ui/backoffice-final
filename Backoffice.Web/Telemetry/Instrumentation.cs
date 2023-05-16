using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Backoffice.Web.Telemetry
{

  public class Instrumentation: IDisposable
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
    }

    public ActivitySource ActivitySource { get; }
    public Counter<long> LoginSuccessCounter { get; }
    public Counter<long> GetTableauUrlSuccessCounter { get; }
    public Counter<long> GetTableauUrlFailureCounter { get; }

    public void Dispose()
    {
      this.ActivitySource.Dispose();
      this.meter.Dispose();
    }

  }

}
