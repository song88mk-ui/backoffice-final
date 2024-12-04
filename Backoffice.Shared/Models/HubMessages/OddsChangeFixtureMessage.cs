using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;

namespace Backoffice.Shared.Models.HubMessages
{
    public class OddsChangeFixtureMessage
    {
        public int SpId { get; set; }
        public string SpNa { get; set; }
        public int SegId { get; set; }
        public string SegNa { get; set; }
        public int LeId { get; set; }
        public string LeNa { get; set; }
        public string PrFiId { get; set; }
        public string FiId { get; set; }
        public string FiNa { get; set; }
        public string HoNa { get; set; }
        public string AwNa { get; set; }
        public DateTime FiStDa { get; set; }
        public bool IsLi { get; set; }
        public int FiSId { get; set; }
        public string FiStNa { get; set; }
        public string Sc { get; set; }
        public string CuPe { get; set; }
        public int CuMi { get; set; }
        public int SoOr { get; set; }
        public int FiTyId { get; set; }
        public long UnTi { get; set; }
        public List<OddsChangeMarket> Ma { get; set; } = new List<OddsChangeMarket>();
    }
    public class OddsChangeMarket
    {
        public string MaTeId { get; set; }
        public string MaId { get; set; }
        public string PrMaId { get; set; }
        public string MaNa { get; set; }
        public string Li { get; set; }
        public int MaStId { get; set; }
        public string MaStNa { get; set; }
        public string MaSp { get; set; }
        public string ClCo { get; set; } // To Be Remove
        public string ClCoMa { get; set; } // To Be Remove
        public Dictionary<string, decimal> ClCoMaLi { get; set; }
        public int SoOr { get; set; }
        public long UnTi { get; set; }
        public List<OddsChangeClientSelections> ClSe { get; set; } = new List<OddsChangeClientSelections>();
        public List<OddsChangeSelection> Se { get; set; } = new List<OddsChangeSelection>();
    }
    public class OddsChangeClientSelections
    {
        public string ClCo { get; set; }
        public string ClMa { get; set; }

        public List<OddsChangeSelection> Se { get; set; } = new List<OddsChangeSelection>();
    }
    public class OddsChangeSelection
    {
        public string PrSeId { get; set; }
        public long SeId { get; set; }
        public string SeTeId { get; set; }
        public string SeNa { get; set; }
        //public string SelectionSpecifier { get; set; }
        public decimal DecOd { get; set; }
        public bool IsAc { get; set; }
        public int SoOr { get; set; }
        public long UnTi { get; set; }
    }
    public class OddsChangeClientOdds
    {
        public string ClCo { get; set; }
        public string ClCoMa { get; set; }
        public decimal Od { get; set; }
        public string SeTeId { get; set; }
        public string SeSp { get; set; }
        public string SeNa { get; set; }
        public long SeId { get; set; }
        public bool IsAc { get; set; }
        public int SoOr { get; set; }
    }
}
