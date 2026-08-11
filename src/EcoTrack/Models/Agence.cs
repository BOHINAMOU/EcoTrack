using System.ComponentModel.DataAnnotations;

namespace EcoTrack.Models
{
    /// <summary>
    /// Niveau 1 : l'agence ou le site (ex: "Agence LOMÉ CENTRE").
    /// </summary>
    public class Agence
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(150)]
        [Display(Name = "Nom de l'agence")]
        public string Nom { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Code { get; set; }

        [StringLength(150)]
        public string? Localisation { get; set; }

        [Display(Name = "Actif")]
        public bool EstActif { get; set; } = true;

        public ICollection<Departement> Departements { get; set; } = new List<Departement>();

        /// <summary>Actifs attribués directement à l'agence (pas à un employé précis).</summary>
        public ICollection<Actif> ActifsPartages { get; set; } = new List<Actif>();
    }
}