using EcoTrack.Models;

namespace EcoTrack.ViewModels
{
    public class RepartitionCategorie
    {
        public string NomCategorie { get; set; } = string.Empty;
        public int Nombre { get; set; }
        public double Pourcentage { get; set; }
        public string CouleurHex { get; set; } = "#0d3b66";
    }

    public class DepartementDetailsViewModel
    {
        public Departement Departement { get; set; } = null!;
        public List<RepartitionCategorie> RepartitionParCategorie { get; set; } = new();
        public List<Affectation> DernieresAffectations { get; set; } = new();
    }
}