using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoTrack.Models
{
    public class Employe
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public Service? Service { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100)]
        public string Prenom { get; set; } = string.Empty;
        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Le format du numéro de téléphone n'est pas valide.")]
        [StringLength(20)]
        [Display(Name = "Numéro de téléphone")]
        public string? Telephone { get; set; }

        [StringLength(100)]
        public string? Poste { get; set; }

        [Display(Name = "Employé actif")]
        public bool EstActif { get; set; } = true;

        [Required(ErrorMessage = "L'unité est obligatoire.")]
        [Display(Name = "Unité")]
        public int UniteId { get; set; }
        public Unite? Unite { get; set; }

        /// <summary>Compte de connexion de l'employé (créé automatiquement à la création de l'employé).</summary>
        public string? ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }

        [NotMapped]
        public Agence? Agence => Unite?.Service?.Division?.Departement?.Agence;

        [NotMapped]
        public Departement? DepartementOrg => Unite?.Service?.Division?.Departement;

        [NotMapped]
        public Division? DivisionOrg => Unite?.Service?.Division;

        [NotMapped]
        public Service? ServiceOrg => Unite?.Service;

        public ICollection<Affectation> Affectations { get; set; } = new List<Affectation>();
    }
}