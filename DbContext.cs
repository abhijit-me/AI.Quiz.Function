using Microsoft.EntityFrameworkCore;
using AI.Quiz.Function.Models;

namespace AI.Quiz.Function.Data
{
    public class QuizDbContext : DbContext
    {
        public QuizDbContext(DbContextOptions<QuizDbContext> options) : base(options)
        {
        }

        // DbSets for each entity
        public DbSet<Models.Quiz> Quiz { get; set; }
        public DbSet<QuizCategories> QuizCategories { get; set; }
        public DbSet<Users> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Quiz entity
            modelBuilder.Entity<Models.Quiz>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                // Configure relationships if needed
                // Example: Foreign key relationship with QuizCategories
                entity.HasOne<QuizCategories>()
                      .WithMany()
                      .HasForeignKey(q => q.Category)
                      .HasPrincipalKey(qc => qc.Category)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure QuizCategories entity
            modelBuilder.Entity<QuizCategories>(entity =>
            {
                entity.HasKey(e => e.Category);
            });

            // Configure Users entity
            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                
                // Add unique constraint for email
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // This will be overridden by dependency injection in Azure Functions
                // but provides a fallback for development/testing
                var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
                if (!string.IsNullOrEmpty(connectionString))
                {
                    optionsBuilder.UseSqlServer(connectionString);
                }
            }
        }
    }
}