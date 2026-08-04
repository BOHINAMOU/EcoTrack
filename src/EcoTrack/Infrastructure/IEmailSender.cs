namespace EcoTrack.Infrastructure
{
    public interface IEmailSender
    {
        Task EnvoyerAsync(string destinataire, string sujet, string corpsHtml);
    }
}