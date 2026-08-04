using EcoTrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Service> Services => Set<Service>();
        public DbSet<JournalAction> JournalActions => Set<JournalAction>();
        public DbSet<Departement> Departements => Set<Departement>();
        public DbSet<CategorieActif> CategoriesActifs => Set<CategorieActif>();
        public DbSet<Actif> Actifs => Set<Actif>();
        public DbSet<Employe> Employes => Set<Employe>();
        public DbSet<Affectation> Affectations => Set<Affectation>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Service>()
                .HasOne(s => s.Departement)
                .WithMany(d => d.Services)
                .HasForeignKey(s => s.DepartementId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<JournalAction>()
                .HasOne(j => j.Utilisateur)
                .WithMany()
                .HasForeignKey(j => j.UtilisateurId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Employe>()
                .HasOne(e => e.Service)
                .WithMany(s => s.Employes)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
            // Le numéro de série doit être unique dans tout le parc.
            builder.Entity<Actif>()
                .HasIndex(a => a.NumeroSerie)
                .IsUnique();

            builder.Entity<Affectation>()
                .HasOne(a => a.Actif)
                .WithMany(a => a.Affectations)
                .HasForeignKey(a => a.ActifId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Affectation>()
                .HasOne(a => a.Employe)
                .WithMany(e => e.Affectations)
                .HasForeignKey(a => a.EmployeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Actif>()
                .HasOne(a => a.Departement)
                .WithMany(d => d.Actifs)
                .HasForeignKey(a => a.DepartementId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Employe>()
                .HasOne(e => e.Departement)
                .WithMany(d => d.Employes)
                .HasForeignKey(e => e.DepartementId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Actif>()
                .HasOne(a => a.CategorieActif)
                .WithMany(c => c.Actifs)
                .HasForeignKey(a => a.CategorieActifId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}