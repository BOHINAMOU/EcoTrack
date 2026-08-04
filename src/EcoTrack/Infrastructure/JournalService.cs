using EcoTrack.Data;
using EcoTrack.Models;

namespace EcoTrack.Infrastructure
{
    public class JournalService : IJournalService
    {
        private readonly ApplicationDbContext _context;

        public JournalService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task EnregistrerAsync(string utilisateurId, string typeAction, string description)
        {
            _context.JournalActions.Add(new JournalAction
            {
                UtilisateurId = utilisateurId,
                TypeAction = typeAction,
                Description = description,
                DateAction = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }
}