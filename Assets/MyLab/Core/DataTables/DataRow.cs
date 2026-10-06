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

    /// <summary>Optional display text DTO. Empty text is allowed; project rules may require a value.</summary>
    public sealed class TextRow : DataRow
    {
        /// <summary>CSV display text.</summary>
        [Name("text")]
        public string Text { get; set; }
    }

    /// <summary>Optional asset-key DTO. Does not own or load an asset handle.</summary>
    public sealed class ResourceKeyRow : DataRow
    {
        /// <summary>Project resource key, mapped from CSV key.</summary>
        [Name("key")]
        public string Key { get; set; }
    }

    /// <summary>Standard text table without additional text restrictions.</summary>
    public sealed class TextDataTable : CsvDataTable<TextRow>
    {
    }

    /// <summary>Standard resource-key table. Nonblank keys are required; asset existence is validated by the resource owner.</summary>
    public sealed class ResourceKeyDataTable : CsvDataTable<ResourceKeyRow>
    {
        /// <inheritdoc />
        protected override void ValidateRow(ResourceKeyRow row)
        {
            if (string.IsNullOrWhiteSpace(row.Key))
            {
                throw new System.IO.InvalidDataException("A nonblank resource key is required.");
            }
        }
    }
}
