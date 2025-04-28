using EfCoreMultiColumnVector.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMultiColumnVector.DataAccess
{
    public class AppDbContext: DbContext
    {
        public DbSet<BankInfo> BankInfos { get; set; } = null!;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("vector");

            // Configure the BankInfo entity
            modelBuilder.Entity<BankInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BankName).IsRequired();
                entity.Property(e => e.Slogan).IsRequired();
                entity.Property(e => e.NameEmbedding)
                .HasColumnType("vector(384)")
                .IsRequired();
                entity.Property(e => e.SloganEmbedding)
                .HasColumnType("vector(384)")
                .IsRequired();
            });
        }
        public override int SaveChanges()
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                }
                entry.Entity.UpdatedAt = now;
            }
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                }
                entry.Entity.UpdatedAt = now;
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
