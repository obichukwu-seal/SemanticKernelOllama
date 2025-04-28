using Pgvector;

namespace EfCoreMultiColumnVector.DataAccess.Entities
{
    // Create a auditable base entity
    public class AuditableEntity
    {
        public int Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class BankInfo: AuditableEntity
    {
        public string BankName { get; set; } = string.Empty;
        public string Slogan { get; set; } = string.Empty;

        public Vector NameEmbedding { get; set; }

        public Vector SloganEmbedding { get; set; }
    }
}
