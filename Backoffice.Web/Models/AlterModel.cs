namespace Backoffice.Web.Models
{
    public class AlterModel
    {
        public string ErrorMessage { get; set; }
        public string AlertStatus { get; set; }
        public decimal Opacity { get; set; } = 0.0m;
        protected bool ShowErrorMessage { get; set; } = false;
    }
}
