using CsvHelper.Configuration.Attributes;
using MyLab.Core.DataTables;

namespace MyLab.Examples.DataTables
{
    /// <summary>Example display text DTO; copy and customize in the consuming project. Empty text is allowed; project rules may require a value.</summary>
    public sealed class TextRow : DataRow
    {
        /// <summary>CSV display text.</summary>
        [Name("text")]
        public string Text { get; set; }
    }

    /// <summary>Example asset-key DTO; copy and customize in the consuming project. Does not own or load an asset handle.</summary>
    public sealed class ResourceKeyRow : DataRow
    {
        /// <summary>Project resource key, mapped from CSV key.</summary>
        [Name("key")]
        public string Key { get; set; }
    }

    /// <summary>Example text table without additional text restrictions.</summary>
    public sealed class TextDataTable : CsvDataTable<TextRow>
    {
    }

    /// <summary>Example resource-key table. Nonblank keys are required; asset existence is validated by the resource owner.</summary>
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
