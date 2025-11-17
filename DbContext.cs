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
        public DbSet<QuizQuestion> Questions { get; set; }
        public DbSet<QuizCategory> Categories { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure QuizQuestion entity
            modelBuilder.Entity<QuizQuestion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                // Configure relationships if needed
                // Example: Foreign key relationship with Categories
                entity.HasOne<QuizCategory>()
                      .WithMany()
                      .HasForeignKey(q => q.Category)
                      .HasPrincipalKey(qc => qc.Category)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure QuizCategory entity
            modelBuilder.Entity<QuizCategory>(entity =>
            {
                entity.HasKey(e => e.Category);
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                
                // Add unique constraint for username
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