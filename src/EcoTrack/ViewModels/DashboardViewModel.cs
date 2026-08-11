namespace EcoTrack.ViewModels
{
    public class DashboardViewModel
    {
        public string PrenomUtilisateur { get; set; } = string.Empty;
        public string NomUtilisateur { get; set; } = string.Empty;

        public int NombreEmployes { get; set; }
        public int NombreActifsDisponibles { get; set; }
        public int NombreActifsAttribues { get; set; }
        public int NombreActifsDeteriores { get; set; }
        public int NombreActifsEnPanne { get; set; }
        public int NombreAgences { get; set; }
        public int NombreDepartements { get; set; }
        public int NombreServices { get; set; }
    }
}