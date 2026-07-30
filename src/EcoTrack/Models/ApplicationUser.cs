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
    }
}