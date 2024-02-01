using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using System.Diagnostics;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class Main
    {
        [Parameter] public List<SportSettingSport> SportSettings { get; set; }

        private int _counter;

        protected override Task OnParametersSetAsync()
        {
            Debug.WriteLine($"Main: {_counter++}");

            return base.OnParametersSetAsync();
        }
    }
}
