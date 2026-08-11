using EcoTrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Agence> Agences { get; set; }
        public DbSet<Departement> Departements { get; set; }
        public DbSet<Division> Divisions { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Unite> Unites { get; set; }

        public DbSet<Employe> Employes { get; set; }
        public DbSet<Actif> Actifs { get; set; }
        public DbSet<Affectation> Affectations { get; set; }
        public DbSet<CategorieActif> CategoriesActifs { get; set; }

        // À ajouter si la classe JournalAction existe dans ton projet
        public DbSet<JournalAction> JournalActions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Departement>()
                .HasOne(d => d.Agence)
                .WithMany(a => a.Departements)
                .HasForeignKey(d => d.AgenceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Division>()
                .HasOne(dv => dv.Departement)
                .WithMany(d => d.Divisions)
                .HasForeignKey(dv => dv.DepartementId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Service>()
                .HasOne(s => s.Division)
                .WithMany(dv => dv.Services)
                .HasForeignKey(s => s.DivisionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Unite>()
                .HasOne(u => u.Service)
                .WithMany(s => s.Unites)
                .HasForeignKey(u => u.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Actif>()
                .HasOne(a => a.Agence)
                .WithMany(ag => ag.ActifsPartages)
                .HasForeignKey(a => a.AgenceId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Actif>()
                .HasOne(a => a.Departement)
                .WithMany()
                .HasForeignKey(a => a.DepartementId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Actif>()
                .HasOne(a => a.Division)
                .WithMany()
                .HasForeignKey(a => a.DivisionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Actif>()
                .HasOne(a => a.Service)
                .WithMany()
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Actif>()
                .HasOne(a => a.Unite)
                .WithMany()
                .HasForeignKey(a => a.UniteId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Employe>()
                .HasOne(e => e.Unite)
                .WithMany(u => u.Employes)
                .HasForeignKey(e => e.UniteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Actif>()
                .HasOne(a => a.CategorieActif)
                .WithMany(c => c.Actifs)
                .HasForeignKey(a => a.CategorieActifId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Affectation>()
                .HasOne(a => a.Actif)
                .WithMany(a => a.Affectations)
                .HasForeignKey(a => a.ActifId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Affectation>()
                .HasOne(a => a.Employe)
                .WithMany(e => e.Affectations)
                .HasForeignKey(a => a.EmployeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Employe>()
                .HasOne(e => e.ApplicationUser)
                .WithOne()
                .HasForeignKey<Employe>(e => e.ApplicationUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}