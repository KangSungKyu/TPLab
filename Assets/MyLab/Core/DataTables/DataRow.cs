using CsvHelper.Configuration.Attributes;

namespace MyLab.Core.DataTables
{
    /// <summary>Standard uint row contract. Arbitrary-key manual tables need not implement this interface.</summary>
    public interface IDataRow
    {
        /// <summary>Final encoded PK, never regenerated during loading.</summary>
        uint Id { get; }
    }

    /// <summary>Optional DTO base mapping CSV idx to Id. Consumers must treat published DTOs as read-only.</summary>
    public abstract class DataRow : IDataRow
    {
        /// <summary>Final uint PK. The setter serves CSV mapping; mutation after publication is unsupported.</summary>
        [Name("idx")]
        public uint Id { get; set; }
    }
}
