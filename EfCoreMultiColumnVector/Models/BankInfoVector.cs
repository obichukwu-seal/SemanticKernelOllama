using Microsoft.Extensions.VectorData;

namespace EfCoreMultiColumnVector.Models
{
    public class BankInfoVector
    {
        [VectorStoreRecordKey]
        public int Id { get; set; }

        [VectorStoreRecordData]
        public string BankName { get; set; } = string.Empty;

        [VectorStoreRecordData]
        public string Slogan { get; set; } = string.Empty;

        [VectorStoreRecordVector(Dimensions: 384, DistanceFunction: DistanceFunction.CosineSimilarity)]

        public ReadOnlyMemory<float> NameEmbedding
        {
            get; set;
        }

        [VectorStoreRecordVector(Dimensions: 384, DistanceFunction: DistanceFunction.CosineSimilarity)]

        public ReadOnlyMemory<float> SloganEmbedding
        {
            get; set;
        }
    }
}
