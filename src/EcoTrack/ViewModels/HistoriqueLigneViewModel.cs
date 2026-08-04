using EcoTrack.Models;

namespace EcoTrack.ViewModels
{
    public class HistoriqueLigneViewModel
    {
        public Affectation Affectation { get; set; } = null!;
        public bool EstReattribution { get; set; }
        public string? DetenteurPrecedentNom { get; set; }
        public bool MemeEmployeQuAvant { get; set; }
    }
}