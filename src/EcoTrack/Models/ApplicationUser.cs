using Microsoft.AspNetCore.Identity;

namespace EcoTrack.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public string? CreeParId { get; set; }

        public bool DoitChangerMotDePasse { get; set; } = false;

        /// <summary>Si renseignée, l'accès admin (rôle AdminTemporaire) de ce compte est retiré automatiquement après cette date.</summary>
        public DateTime? DateExpirationAcces { get; set; }
    }
}