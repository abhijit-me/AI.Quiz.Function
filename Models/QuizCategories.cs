using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI.Quiz.Function.Models
{
    [Table("QuizCategories")]
    public class QuizCategories
    {

        [Key]
        [MaxLength(8)]
        [Column("category")]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        [Column("description")]
        public string Description { get; set; } = string.Empty;
    }
}