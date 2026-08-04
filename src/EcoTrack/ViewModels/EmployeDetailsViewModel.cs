using EcoTrack.Models;

namespace EcoTrack.ViewModels
{
    public class EmployeDetailsViewModel
    {
        public Employe Employe { get; set; } = null!;
        public List<HistoriqueLigneViewModel> AffectationsActives { get; set; } = new();
        public List<Actif> ActifsDisponibles { get; set; } = new();
        public List<HistoriqueLigneViewModel> HistoriqueComplet { get; set; } = new();
    }
}