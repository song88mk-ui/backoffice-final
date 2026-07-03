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
        [Column("TARGET_LANGUAGE")]
        public string TargetLanguage { get; set; }

        [Column("TARGET_TEXT")]
        public string TargetText { get; set; }

        [Column("CREATED_AT")]
        public DateTime? CreatedAt { get; set; }

        [Column("GLOSSARY_VERSION")]
        public int? GlossaryVersion { get; set; }

        [Column("UPDATED_AT")]
        public DateTime? UpdatedAt { get; set; }
    }
}
