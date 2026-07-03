using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class SportsbookGlossaryResponse
    {
        public long Id { get; set; }
        public string StringId { get; set; }
        public string SourceText { get; set; }
        public bool Processed { get; set; }
        public bool HasBetPlacement { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string TargetLanguage { get; set; }
        public string TargetText { get; set; }
    }

    public class SportsbookGlossaryListResponse
    {
        public List<SportsbookGlossaryResponse> Glossary { get; set; }
        public MetaData MetaData { get; set; }
    }
}
