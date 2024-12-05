using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.HubMessages
{
    public class OddsChangeMonitoringMessage
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
        public int FiStId { get; set; }
        public string FiStNa { get; set; }
        public string Sc { get; set; }
        public string CuPe { get; set; }
        public int CuMi { get; set; }
        public int SoOr { get; set; }
        public int FiTyId { get; set; }
        public long UnTi { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public List<OddsChangeMonitoringClient> Cl { get; set; }
    }
    public class OddsChangeMonitoringClient
    {
        public string ClCo { get; set; }
        public long UnTi { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public List<OddsChangeMonitoringMarket> Ma { get; set; }
    }
    public class OddsChangeMonitoringMarket
    {
        public string ClCo { get; set; }
        public decimal ClCoMa { get; set; }
        public string PrFiId { get; set; }
        public string MaTeId { get; set; }
        public string PrMaId { get; set; }
        public string MaId { get; set; }
        public string MaNa { get; set; }
        public string Li { get; set; }
        public int MaStId { get; set; }
        public string MaStNa { get; set; }
        public string MaSp { get; set; }
        public bool IsBo { get; set; }
        public long UnTi { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public List<OddsChangeMonitoringSelection> Se { get; set; }
    }
    public class OddsChangeMonitoringSelection
    {
        public string PrSeId { get; set; }
        public string SeId { get; set; }
        public string SeSp { get; set; }
        public string SeTeId { get; set; }
        public string SeNa { get; set; }
        public string DeOd { get; set; }
        public bool IsAc { get; set; }
        public int SoOr { get; set; }
        public long UnTi { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
