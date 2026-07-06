using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backoffice.Shared.Data.Translation.Entities
{
    public class TranslationCache2
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public long Id { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("STRING_ID")]
        public string StringId { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("SOURCE_LANGUAGE")]
        public string SourceLanguage { get; set; }

        [Required]
        [MaxLength(250)]
        [Column("SOURCE_TEXT")]
        public string SourceText { get; set; }

        [Required]
        [MaxLength(250)]
        [Column("TARGET_TEXT_ZH")]
        public string TargetTextZh { get; set; }

        [Required]
        [MaxLength(250)]
        [Column("TARGET_TEXT_ES")]
        public string TargetTextEs { get; set; }

        [Required]
        [MaxLength(250)]
        [Column("TARGET_TEXT_FR")]
        public string TargetTextFr { get; set; }

        [Required]
        [Column("HAS_BET_PLACEMENT")]
        public bool HasBetPlacement { get; set; }

        [Column("CREATED_AT")]
        public DateTime? CreatedAt { get; set; }

        // The external translation pipeline's worklist flag: 0 = row needs a pipeline (DeepL)
        // pass, 1 = handled (set by the pipeline; reset to 0 by OE.Translation.Service on a
        // genuine source-name change). Replaces the retired GLOSSARY_VERSION column — schema
        // authority is odds-engine-2 OE.Shared\Data\Translation\Scripts\TranslationCache2.sql.
        [Required]
        [Column("PROCESSED")]
        public bool Processed { get; set; }

        [Column("UPDATED_AT")]
        public DateTime? UpdatedAt { get; set; }
    }
}
