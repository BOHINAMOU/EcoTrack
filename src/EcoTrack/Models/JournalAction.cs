using System.ComponentModel.DataAnnotations;

namespace EcoTrack.Models
{
    /// <summary>
    /// Trace chaque action importante effectuée par un admin (principal ou secondaire),
    /// pour permettre à l'admin principal de suivre l'activité de chacun.
    /// </summary>
    public class JournalAction
    {
        public int Id { get; set; }

        [Required]
        public string UtilisateurId { get; set; } = string.Empty;
        public ApplicationUser? Utilisateur { get; set; }

        [Required]
        [StringLength(50)]
        public string TypeAction { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string Description { get; set; } = string.Empty;

        public DateTime DateAction { get; set; } = DateTime.UtcNow;
    }
}