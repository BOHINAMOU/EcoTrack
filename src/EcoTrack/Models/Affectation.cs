using System.ComponentModel.DataAnnotations;

namespace EcoTrack.Models
{
    public class Affectation
    {
        public int Id { get; set; }

        [Required]
        public int ActifId { get; set; }
        public Actif? Actif { get; set; }

        [Required]
        public int EmployeId { get; set; }
        public Employe? Employe { get; set; }

        [Display(Name = "Date d'affectation")]
        public DateTime DateAffectation { get; set; } = DateTime.UtcNow;
        [Display(Name = "Date de retrait")]
        public DateTime? DateRetrait { get; set; }

        [StringLength(250)]
        public string? Motif { get; set; }
    }
}