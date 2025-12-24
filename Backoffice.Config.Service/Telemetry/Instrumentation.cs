using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Backoffice.Config.Service.Telemetry
{

  public class Instrumentation: IDisposable
  {

    internal const string ActivitySourceName = "Backoffice.Utility";
    internal const string MeterName = "Backoffice.Utility";

    private readonly Meter meter;


    public Instrumentation()
    {
      string? version = "latest";
      this.ActivitySource = new ActivitySource(ActivitySourceName, version);
      this.meter = new Meter(MeterName, version);
    }

    public ActivitySource ActivitySource { get; }

    public void Dispose()
    {
      this.ActivitySource.Dispose();
      this.meter.Dispose();
    }

  }

}
