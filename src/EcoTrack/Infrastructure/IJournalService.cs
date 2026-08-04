namespace EcoTrack.Infrastructure
{
    public interface IJournalService
    {
        Task EnregistrerAsync(string utilisateurId, string typeAction, string description);
    }
}