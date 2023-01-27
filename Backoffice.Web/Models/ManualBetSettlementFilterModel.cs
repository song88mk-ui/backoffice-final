namespace Backoffice.Web.Models
{
    public class ManualBetSettlementFilterModel
    {
        #region Ctor
        public ManualBetSettlementFilterModel()
        {
            Clients = new List<SelectListItemWithCheckboxModel<string>>();
            Sports = new List<SelectListItemWithCheckboxModel<string>>();
            Currencies = new List<SelectListItemWithCheckboxModel<string>>();
            FromStatuses = new List<SelectListItemWithCheckboxModel<string>>();
            ToStatuses = new List<SelectListItemWithCheckboxModel<string>>();
        }
        #endregion
        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        public int SelectedClientCount { get; set; }
        public string SelectedClientName { get; set; }

        public List<SelectListItemWithCheckboxModel<string>> Sports { get; set; }
        public int SelectedSportCount { get; set; }
        public string SelectedSportName { get; set; }

        public List<SelectListItemWithCheckboxModel<string>> Currencies { get; set; }
        public int SelectedCurrencyCount { get; set; }
        public string SelectedCurrencyName { get; set; }

        public List<SelectListItemWithCheckboxModel<string>> FromStatuses { get; set; }
        public int FromSelectedStatusCount { get; set; }
        public string FromSelectedStatusName { get; set; }

        public List<SelectListItemWithCheckboxModel<string>> ToStatuses { get; set; }
        public int ToSelectedStatusCount { get; set; }
        public string ToSelectedStatusName { get; set; }

        public string BetId { get; set; }
        public string MemberName { get; set; }
        public string SettledBy { get; set; }

        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-7).Date;
        public DateTime DateTo { get; set; } = DateTime.Now.Date;
    }
}
