using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ConfigLogsRequest
    {
        public ConfigLogsRequest()
        {
            MetaData = new MetaData();
        }
        public int FixtureTypeId { get; set; }
        public Guid? ClientId { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public List<string> MarketTemplateIds { get; set; }
        public string MarketId { get; set; }
        public string Scope { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Filter { get; set; }
        public MetaData MetaData { get; set; }
        public int Id { get; set; }
        public string CursorDirection { get; set; }
    }
