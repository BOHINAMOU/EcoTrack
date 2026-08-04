using System.Security.Cryptography;

namespace EcoTrack.Infrastructure
{
    public static class GenerateurMotDePasse
    {
        public static string Generer(int longueur = 10)
        {
            const string majuscules = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string minuscules = "abcdefghijkmnpqrstuvwxyz";
            const string chiffres = "23456789";
            const string speciaux = "!@#$%*?";
            const string tous = majuscules + minuscules + chiffres + speciaux;

            var motDePasse = new List<char>
            {
                TirerCaractere(majuscules),
                TirerCaractere(minuscules),
                TirerCaractere(chiffres),
                TirerCaractere(speciaux)
            };

            while (motDePasse.Count < longueur)
            {
                motDePasse.Add(TirerCaractere(tous));
            }

            return new string(motDePasse.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
        }

        private static char TirerCaractere(string source) =>
            source[RandomNumberGenerator.GetInt32(source.Length)];
    }
}