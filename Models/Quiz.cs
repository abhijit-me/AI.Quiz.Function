using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI.Quiz.Function.Models
{
    [Table("Quiz")]
    public class Quiz
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(8)]
        [Column("category")]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(1024)]
        [Column("question")]
        public string Question { get; set; } = string.Empty;

        [MaxLength(256)]
        [Column("option_a")]
        public string? OptionA { get; set; }

        [MaxLength(256)]
        [Column("option_b")]
        public string? OptionB { get; set; }

        [MaxLength(256)]
        [Column("option_c")]
        public string? OptionC { get; set; }

        [MaxLength(256)]
        [Column("option_d")]
        public string? OptionD { get; set; }

        [MaxLength(256)]
        [Column("option_e")]
        public string? OptionE { get; set; }

        [Required]
        [Column("answer")]
        public string Answer { get; set; } = string.Empty;
    }
}