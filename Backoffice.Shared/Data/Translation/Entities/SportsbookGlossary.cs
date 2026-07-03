using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backoffice.Shared.Data.Translation.Entities
{
    public class SportsbookGlossary
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public long Id { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("String_ID")]
        public string StringId { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("SOURCE_LANGUAGE_ID")]
        public string SourceLanguageId { get; set; }

        [Required]
        [Column("SOURCE_TEXT")]
        public string SourceText { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("SOURCE_HASH")]
        public string SourceHash { get; private set; }

        [Required]
        [Column("GLOSSARY_VERSION")]
        public int GlossaryVersion { get; set; }

        [Required]
        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; }

        [Column("UPDATED_AT")]
        public DateTime? UpdatedAt { get; set; }

        [Required]
        [Column("PROCESSED")]
        public bool Processed { get; set; }

        [Required]
        [Column("HAS_BET_PLACEMENT")]
        public bool HasBetPlacement { get; set; }
    }
}
