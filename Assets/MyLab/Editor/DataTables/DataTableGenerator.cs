using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MyLab.Core.DataTables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyLab.Core.Editor.DataTables
{
    /// <summary>One explicit project field. Required means column presence, not nonempty values.</summary>
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class DataTableColumn
    {
        [JsonProperty("name", Required = Newtonsoft.Json.Required.Always)] public string Name;
        [JsonProperty("member", Required = Newtonsoft.Json.Required.Always)] public string Member;
        [JsonProperty("type", Required = Newtonsoft.Json.Required.Always)] public string Type;
        [JsonProperty("required", Required = Newtonsoft.Json.Required.Always)] public bool Required;
    }

    /// <summary>Project table schema. This initial implementation supports CSV and the standard uint idx contract.</summary>
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class DataTableSchema
    {
        [JsonProperty("schemaVersion", Required = Newtonsoft.Json.Required.Always)] public int SchemaVersion;
        [JsonProperty("tableId", Required = Newtonsoft.Json.Required.Always)] public string TableId;
        [JsonProperty("input", Required = Newtonsoft.Json.Required.Always)] public string Input;
        [JsonProperty("format", Required = Newtonsoft.Json.Required.Always)] public string Format;
        [JsonProperty("mode", Required = Newtonsoft.Json.Required.Always)] public string Mode;
        [JsonProperty("namespace")] public string Namespace;
        [JsonProperty("rowType", Required = Newtonsoft.Json.Required.Always)] public string RowType;
        [JsonProperty("tableType", Required = Newtonsoft.Json.Required.Always)] public string TableType;
        [JsonProperty("dataType", Required = Newtonsoft.Json.Required.Always)] public uint DataType;
        [JsonProperty("columns", Required = Newtonsoft.Json.Required.Always)] public DataTableColumn[] Columns;
    }

    /// <summary>Pure preflight and deterministic generation. Does not write files or claim compiled validation.</summary>
    public static class DataTableGenerator
    {
        private static readonly Dictionary<string, Type> Types = new Dictionary<string, Type>
        {
            ["string"] = typeof(string),
            ["bool"] = typeof(bool),
            ["int"] = typeof(int),
            ["uint"] = typeof(uint),
            ["long"] = typeof(long),
            ["ulong"] = typeof(ulong),
            ["float"] = typeof(float),
            ["double"] = typeof(double),
            ["decimal"] = typeof(decimal)
        };
        private static readonly HashSet<string> Keywords = new HashSet<string>(("abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while record required file").Split(' '));

        /// <summary>Reads schema JSON, rejecting duplicate/unknown fields and unsupported versions.</summary>
        public static DataTableSchema ReadSchema(string json)
        {
            try
            {
                using (var reader = new JsonTextReader(new StringReader(json ?? throw new InvalidDataException("Schema is null."))))
                {
                    var obj = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                    if (reader.Read())
                        throw new InvalidDataException("Unexpected content after schema.");
                    if (obj["schemaVersion"]?.Type != JTokenType.Integer || obj["dataType"]?.Type != JTokenType.Integer)
                        throw new InvalidDataException("Schema version and dataType must be integers.");
                    foreach (string field in new[] { "tableId", "input", "format", "mode", "rowType", "tableType" })
                        if (obj[field]?.Type != JTokenType.String)
                            throw new InvalidDataException("Schema strings must be explicit: " + field);
                    if (!(obj["columns"] is JArray columns))
                        throw new InvalidDataException("Columns must be an array.");
                    foreach (var column in columns)
                        if (!(column is JObject) || column["required"]?.Type != JTokenType.Boolean || new[] { "name", "member", "type" }.Any(f => column[f]?.Type != JTokenType.String))
                            throw new InvalidDataException("Columns require explicit string names/types and a boolean required flag.");
                    var schema = obj.ToObject<DataTableSchema>(JsonSerializer.Create(new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error }));
                    CheckSchema(schema);
                    return schema;
                }
            }
            catch (JsonException error) { throw new InvalidDataException("Invalid table schema: " + error.Message, error); }
        }
        /// <summary>Checks all CSV fields, complete PKs and schema types using the shared core validator.</summary>
        public static void ValidateCsv(DataTableSchema schema, string csv, IIdxRouter router)
        {
            CheckSchema(schema);
            if (router == null)
                throw new ArgumentNullException(nameof(router));
            DataTableCsvValidator.Read<uint, uint>(csv, schema.Columns.Where(c => c.Required).Select(c => c.Name).ToArray(), reader =>
            {
                foreach (var column in schema.Columns)
                {
                    if (reader.HeaderRecord.Contains(column.Name, StringComparer.Ordinal))
                        reader.GetField(FieldType(column.Type), column.Name);
                }
                return reader.GetField<uint>("idx");
            }, idx => idx, idx => DataTableCsvValidator.ValidateIdx(idx, schema.DataType, router), CancellationToken.None);
        }
        /// <summary>Returns two generated source files in stable order; ownership and compilation remain caller responsibilities.</summary>
        public static IReadOnlyDictionary<string, string> Generate(DataTableSchema schema, string defaultNamespace)
        {
            CheckSchema(schema);
            string ns = string.IsNullOrEmpty(schema.Namespace) ? defaultNamespace : schema.Namespace;
            CheckNamespace(ns);
            var row = new StringBuilder("// Generated by MyLab. Edit the schema or a separate partial file.\nusing CsvHelper.Configuration.Attributes;\nusing MyLab.Core.DataTables;\n\nnamespace " + ns + "\n{\n    /// <summary>Project CSV row. Treat published instances as read-only.</summary>\n    public partial class " + schema.RowType + " : DataRow\n    {\n");
            foreach (var column in schema.Columns.Where(c => c.Name != "idx").OrderBy(c => c.Member, StringComparer.Ordinal))
            {
                row.Append("        /// <summary>Project-defined CSV value.</summary>\n        [Name(").Append(JsonConvert.SerializeObject(column.Name, new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeNonAscii })).Append(")]");
                if (!column.Required)
                    row.Append("\n        [Optional]");
                row.Append("\n        public ").Append(column.Type).Append(' ').Append(column.Member).Append(" { get; set; }\n");
            }
            row.Append("    }\n}\n");
            string table = "// Generated by MyLab. Put project validation and interfaces in a separate partial file.\nusing MyLab.Core.DataTables;\n\nnamespace " + ns + "\n{\n    /// <summary>Project CSV table. Manager owns loading and publication.</summary>\n    public partial class " + schema.TableType + " : CsvDataTable<" + schema.RowType + ">\n    {\n    }\n}\n";
            return new Dictionary<string, string> { [schema.RowType + ".g.cs"] = row.ToString(), [schema.TableType + ".g.cs"] = table };
        }

        internal static void CheckSchema(DataTableSchema schema)
        {
            if (schema == null || schema.SchemaVersion != 1 || schema.Format != "csv" ||
                (schema.Mode != "generated" && schema.Mode != "existing") || string.IsNullOrWhiteSpace(schema.TableId) ||
                string.IsNullOrWhiteSpace(schema.Input) || schema.DataType == 0 || schema.Columns == null || schema.Columns.Length == 0)
                throw new InvalidDataException("Schema requires version 1, csv, generated/existing, a name, input, kind and columns.");
            CheckIdentifier(schema.RowType);
            CheckIdentifier(schema.TableType);
            if (schema.RowType == schema.TableType)
                throw new InvalidDataException("Row and table type names must differ.");
            if (!string.IsNullOrEmpty(schema.Namespace))
                CheckNamespace(schema.Namespace, schema.Mode == "existing");
            var names = new HashSet<string>(StringComparer.Ordinal);
            var members = new HashSet<string>(StringComparer.Ordinal);
            foreach (var column in schema.Columns)
            {
                if (column == null || string.IsNullOrWhiteSpace(column.Name) || !names.Add(column.Name))
                    throw new InvalidDataException("Column names must be unique and nonempty.");
                CheckIdentifier(column.Member);
                if (!members.Add(column.Member))
                    throw new InvalidDataException("Member names must be unique.");
                FieldType(column.Type);
                if (column.Member == schema.RowType || column.Name != "idx" && column.Member == "Id")
                    throw new InvalidDataException("Member conflicts with the row type or inherited Id.");
            }
            var id = schema.Columns.SingleOrDefault(c => c.Name == "idx");
            if (id == null || id.Member != "Id" || id.Type != "uint" || !id.Required)
                throw new InvalidDataException("Standard idx must map to required uint Id.");
        }

        internal static void CheckIdentifier(string name)
        {
            if (name == null || !Regex.IsMatch(name, "^[A-Za-z_][A-Za-z0-9_]*$") || Keywords.Contains(name))
                throw new InvalidDataException("Unsupported C# identifier: " + name);
        }

        internal static void CheckNamespace(string ns, bool existing = false)
        {
            if (string.IsNullOrWhiteSpace(ns))
                throw new InvalidDataException("A project namespace is required.");
            foreach (string part in ns.Split('.'))
                CheckIdentifier(part);
            if (!existing && (ns == "MyLab.Core" || ns.StartsWith("MyLab.Core.", StringComparison.Ordinal)))
                throw new InvalidDataException("Generated project types cannot use the core namespace.");
        }

        internal static Type FieldType(string name)
        {
            bool nullable = name != null && name.EndsWith("?", StringComparison.Ordinal);
            string alias = nullable ? name.Substring(0, name.Length - 1) : name;
            if (alias == null || !Types.TryGetValue(alias, out Type type) || nullable && type == typeof(string))
                throw new InvalidDataException("Unsupported field type: " + name);
            return nullable ? typeof(Nullable<>).MakeGenericType(type) : type;
        }
    }
}
