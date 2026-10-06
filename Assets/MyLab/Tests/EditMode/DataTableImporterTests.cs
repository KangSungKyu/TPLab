using System;
using System.IO;
using System.Linq;
using CsvHelper.Configuration.Attributes;
using MyLab.Core.DataTables;
using MyLab.Core.Editor.DataTables;
using MyLab.Examples.DataTables;
using NUnit.Framework;
using UnityEngine;

namespace MyLab.Core.Tests
{
    public sealed class DataTableImporterTests
    {
        private const string SchemaJson = "{\"schemaVersion\":1,\"tableId\":\"texts\",\"input\":\"Assets/Game/Data/texts.csv\",\"format\":\"csv\",\"mode\":\"generated\",\"namespace\":\"Game.Data\",\"rowType\":\"TextRow\",\"tableType\":\"TextTable\",\"dataType\":1,\"columns\":[{\"name\":\"idx\",\"member\":\"Id\",\"type\":\"uint\",\"required\":true},{\"name\":\"text\",\"member\":\"Text\",\"type\":\"string\",\"required\":true}]}";

        [Test]
        public void SettingsDefaultToDisabledAndKeepManualPolicySeparate()
        {
            var settings = ScriptableObject.CreateInstance<DataTableImportSettings>();
            try
            {
                Assert.That(settings.AutomationMode, Is.EqualTo(DataTableAutomationMode.Disabled));
                Assert.That(settings.ValidationProfileId, Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }

        [Test]
        public void ExplicitSchemaProducesStablePartialRuntimeSources()
        {
            var schema = DataTableGenerator.ReadSchema(SchemaJson);
            var first = DataTableGenerator.Generate(schema, "Fallback");
            var second = DataTableGenerator.Generate(schema, "Fallback");
            Assert.That(first.Count, Is.EqualTo(2));
            Assert.That(first.Keys, Is.EquivalentTo(new[] { "TextRow.g.cs", "TextTable.g.cs" }));
            Assert.That(first["TextRow.g.cs"], Is.EqualTo(second["TextRow.g.cs"]));
            Assert.That(first["TextRow.g.cs"], Does.Contain("partial class TextRow : DataRow").And.Contain("namespace Game.Data"));
            Assert.That(first["TextRow.g.cs"], Does.Not.Contain("uint Id").And.Not.Contain("UnityEditor"));
            Assert.That(first["TextTable.g.cs"], Does.Contain("partial class TextTable : CsvDataTable<TextRow>"));
        }

        [TestCase("idx,text\n1001,hello")]
        [TestCase("idx,text\n")]
        [TestCase("idx,text\n1001,\"hello,\nworld\"")]
        public void CommonValidationAllowsValidDataAndHeaderOnly(string csv)
        {
            DataTableGenerator.ValidateCsv(DataTableGenerator.ReadSchema(SchemaJson), csv, new DecimalIdxCodec(1000));
        }

        [TestCase("idx,text\n1001,a\n1001,b")]
        [TestCase("idx,text\n0,a")]
        [TestCase("idx,text\n2001,a")]
        [TestCase("idx,text\n4294967296,a")]
        [TestCase("idx\n1001")]
        [TestCase("idx,text,text\n1001,a,b")]
        [TestCase("idx,text,unused\n1001,a,\"unclosed")]
        public void CommonValidationRejectsInvalidData(string csv)
        {
            var schema = DataTableGenerator.ReadSchema(SchemaJson);
            Assert.Throws<InvalidDataException>(() => DataTableGenerator.ValidateCsv(schema, csv, new DecimalIdxCodec(1000)));
        }

        [Test]
        public void MissingRouterCannotBeReportedAsValidated()
        {
            var schema = DataTableGenerator.ReadSchema(SchemaJson);
            Assert.Throws<ArgumentNullException>(() => DataTableGenerator.ValidateCsv(schema, "idx,text\n1001,a", null));
        }

        [TestCase("{\"schemaVersion\":1,\"schemaVersion\":1}")]
        [TestCase("{\"schemaVersion\":2}")]
        [TestCase("{\"schemaVersion\":1,\"unexpected\":true}")]
        public void AmbiguousOrUnsupportedSchemasAreRejected(string json)
        {
            Assert.Throws<InvalidDataException>(() => DataTableGenerator.ReadSchema(json));
        }

        [TestCase("class", "Text")]
        [TestCase("TextRow", "Id")]
        [TestCase("TextRow", "Bad-Name")]
        public void UnsafeIdentifiersCannotProduceCode(string rowType, string member)
        {
            var schema = DataTableGenerator.ReadSchema(SchemaJson);
            schema.RowType = rowType;
            schema.Columns[1].Member = member;
            Assert.Throws<InvalidDataException>(() => DataTableGenerator.Generate(schema, "Game.Data"));
        }

        [Test]
        public void OptionalTypedFieldsStillRejectBadConversions()
        {
            var schema = DataTableGenerator.ReadSchema(SchemaJson);
            schema.Columns[1].Type = "int?";
            schema.Columns[1].Required = false;
            DataTableGenerator.ValidateCsv(schema, "idx\n1001", new DecimalIdxCodec(1000));
            DataTableGenerator.ValidateCsv(schema, "idx,text\n1001,", new DecimalIdxCodec(1000));
            Assert.Throws<InvalidDataException>(() => DataTableGenerator.ValidateCsv(schema, "idx,text\n1001,no", new DecimalIdxCodec(1000)));
        }

        [TestCase("Assets/../escape")]
        [TestCase("Packages/output")]
        [TestCase("Assets/MyLab/Core/Generated")]
        [TestCase("Assets/MyLab/Tests/Generated")]
        [TestCase("Assets/Editor/Generated")]
        public void InvalidGenerationPathsCannotBeWritten(string path)
        {
            Assert.Throws<InvalidDataException>(() => DataTableGeneratedFiles.AssetPath(path, true));
        }

        [Test]
        public void OwnedWritesAreIdempotentPreserveMetaAndRejectUserEdits()
        {
            string folder = "Assets/ImporterWriterTest_" + Guid.NewGuid().ToString("N");
            try
            {
                var files = DataTableGenerator.Generate(DataTableGenerator.ReadSchema(SchemaJson), "Game.Data");
                Assert.That(DataTableGeneratedFiles.Apply(folder, "owner", "v1", files), Is.True);
                string path = folder + "/TextRow.g.cs";
                DateTime written = File.GetLastWriteTimeUtc(path);
                File.WriteAllText(path + ".meta", "keep GUID");
                Assert.That(DataTableGeneratedFiles.Apply(folder, "owner", "v1", files), Is.False);
                Assert.That(File.GetLastWriteTimeUtc(path), Is.EqualTo(written));
                Assert.That(File.ReadAllText(path + ".meta"), Is.EqualTo("keep GUID"));
                File.AppendAllText(path, "// user edit");
                Assert.Throws<InvalidDataException>(() => DataTableGeneratedFiles.Apply(folder, "owner", "v1", files));
                Assert.That(File.ReadAllText(path), Does.EndWith("// user edit"));
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }

        [Test]
        public void UnknownFilesAndAutomaticContractChangesAreProtected()
        {
            string folder = "Assets/ImporterWriterTest_" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(folder);
                var files = DataTableGenerator.Generate(DataTableGenerator.ReadSchema(SchemaJson), "Game.Data");
                File.WriteAllText(folder + "/TextRow.g.cs", "user file");
                Assert.Throws<InvalidDataException>(() => DataTableGeneratedFiles.Apply(folder, "owner", "v1", files));
                File.Delete(folder + "/TextRow.g.cs");
                DataTableGeneratedFiles.Apply(folder, "owner", "v1", files);
                Assert.Throws<InvalidDataException>(() => DataTableGeneratedFiles.Apply(folder, "owner", "v2", files, true));
                Assert.That(DataTableGeneratedFiles.Apply(folder, "owner", "v2", files), Is.False);
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }

        [Test]
        public void FailedManifestWriteRestoresSourcePairAndRemovesStagingFiles()
        {
            string folder = "Assets/ImporterWriterTest_" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(folder + "/owner.tableimport.json");
                var files = DataTableGenerator.Generate(DataTableGenerator.ReadSchema(SchemaJson), "Game.Data");
                Assert.Throws<IOException>(() => DataTableGeneratedFiles.Apply(folder, "owner", "v1", files));
                Assert.That(Directory.GetFiles(folder), Is.Empty);
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }

        [Test]
        public void MissingSettingsDisableAutomaticWork()
        {
            Assert.That(DataTableImporter.RunAsync(null, true, true).GetAwaiter().GetResult().Status, Is.EqualTo(DataTableImportStatus.Disabled));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RealTypedValidationUsesTheProjectRulesWithoutPublishingRuntimeData(bool reject)
        {
            WithProject((settings, folder, id) =>
            {
                DataTableImportProfiles.Register(id, () => new DecimalIdxCodec(1000), context =>
                {
                    context.RegisterTable<TextRow, TextDataTable>("texts", () => new TextDataTable());
                    context.Manager.AddValidator(snapshot =>
                    {
                        Assert.That(snapshot.Get<TextRow>(1001).Text, Is.EqualTo("hello"));
                        if (reject)
                            throw new InvalidDataException("project rejection");
                    });
                });
                var result = DataTableImporter.RunAsync(settings).GetAwaiter().GetResult();
                Assert.That(result.Status, Is.EqualTo(reject ? DataTableImportStatus.Failed : DataTableImportStatus.Validated));
                Assert.That(Directory.Exists(settings.OutputFolder), Is.False);
            });
        }

        [Test]
        public void MissingTypedRegistrationIsPendingInsteadOfSuccessful()
        {
            WithProject((settings, folder, id) =>
            {
                DataTableImportProfiles.Register(id, () => new DecimalIdxCodec(1000), context => { });
                Assert.That(DataTableImporter.RunAsync(settings).GetAwaiter().GetResult().Status, Is.EqualTo(DataTableImportStatus.AwaitingConfiguration));
            });
        }

        [Test]
        public void DisabledAutomationAndMissingProfileNeverWriteSources()
        {
            WithProject((settings, folder, id) =>
            {
                Assert.That(DataTableImporter.RunAsync(settings, true, true).GetAwaiter().GetResult().Status, Is.EqualTo(DataTableImportStatus.Disabled));
                settings.AutomationMode = DataTableAutomationMode.GenerateValidated;
                Assert.That(DataTableImporter.RunAsync(settings, true, true).GetAwaiter().GetResult().Status, Is.EqualTo(DataTableImportStatus.AwaitingConfiguration));
                Assert.That(Directory.Exists(settings.OutputFolder), Is.False);
            });
        }

        [TestCase("1001,hello,", false, "")]
        [TestCase("1001,hello,1999", true, "FK")]
        [TestCase("1001,hello,2001", true, "FK")]
        [TestCase("1001,forbidden,", true, "hook rejection")]
        public void ActualTableHookAndFkParticipateInTheSameCandidate(string row, bool reject, string reason)
        {
            WithProject((settings, folder, id) =>
            {
                string json = SchemaJson.Replace("Assets/Game/Data/texts.csv", settings.InputFolder + "/texts.csv")
                    .Replace("Game.Data", "MyLab.Core.Tests").Replace("TextRow", "ImportFkRow").Replace("TextTable", "ImportFkTable").Replace("generated", "existing")
                    .Replace("\"columns\":[", "\"columns\":[{\"name\":\"parent\",\"member\":\"ParentId\",\"type\":\"uint?\",\"required\":false},");
                File.WriteAllText(settings.SchemaFolder + "/texts.json", json);
                File.WriteAllText(settings.InputFolder + "/texts.csv", "idx,text,parent\n" + row);
                DataTableImportProfiles.Register(id, () => new DecimalIdxCodec(1000), context =>
                {
                    context.RegisterTable<ImportFkRow, ImportFkTable>("texts", () => new ImportFkTable());
                    context.Manager.RegisterForeignKey<ImportFkRow, ImportFkRow>("texts", "parent", r => r.ParentId, 1, false);
                });
                var result = DataTableImporter.RunAsync(settings).GetAwaiter().GetResult();
                Assert.That(result.Status, Is.EqualTo(reject ? DataTableImportStatus.Failed : DataTableImportStatus.Validated), result.Diagnostic);
                if (reject)
                    Assert.That(result.Diagnostic, Does.Contain(reason));
            });
        }

        [Test]
        public void ChangedInputCannotReuseTheValidatedCandidate()
        {
            WithProject((settings, folder, id) =>
            {
                DataTableImportProfiles.Register(id, () => new DecimalIdxCodec(1000), context =>
                {
                    context.RegisterTable<TextRow, TextDataTable>("texts", () => new TextDataTable());
                    context.Manager.AddValidator(_ => File.AppendAllText(settings.InputFolder + "/texts.csv", "\n1002,changed"));
                });
                Assert.That(DataTableImporter.RunAsync(settings).GetAwaiter().GetResult().Status, Is.EqualTo(DataTableImportStatus.Cancelled));
            });
        }

        [Test]
        public void ChangedProfileCannotReuseTheValidatedCandidate()
        {
            WithProject((settings, folder, id) =>
            {
                DataTableImportProfiles.Register(id, () => new DecimalIdxCodec(1000), context =>
                {
                    context.RegisterTable<TextRow, TextDataTable>("texts", () => new TextDataTable());
                    context.Manager.AddValidator(_ =>
                    {
                        DataTableImportProfiles.Unregister(id);
                        DataTableImportProfiles.Register(id, () => new DecimalIdxCodec(1000), c => { });
                    });
                });
                Assert.That(DataTableImporter.RunAsync(settings).GetAwaiter().GetResult().Status, Is.EqualTo(DataTableImportStatus.Cancelled));
            });
        }

        [Test]
        public void WorkerThreadCallFailsBeforeReadingUnitySettings()
        {
            var settings = ScriptableObject.CreateInstance<DataTableImportSettings>();
            try
            {
                var result = System.Threading.Tasks.Task.Run(() => DataTableImporter.RunAsync(settings).GetAwaiter().GetResult()).GetAwaiter().GetResult();
                Assert.That(result.Status, Is.EqualTo(DataTableImportStatus.Failed));
                Assert.That(result.Diagnostic, Does.Contain("main thread"));
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }

        private static void WithProject(Action<DataTableImportSettings, string, string> test)
        {
            string folder = "Assets/ImporterServiceTest_" + Guid.NewGuid().ToString("N");
            string id = "test_" + Guid.NewGuid().ToString("N");
            var settings = ScriptableObject.CreateInstance<DataTableImportSettings>();
            try
            {
                settings.InputFolder = folder + "/Input";
                settings.SchemaFolder = folder + "/Schemas";
                settings.OutputFolder = folder + "/Generated";
                settings.ValidationProfileId = id;
                Directory.CreateDirectory(settings.InputFolder);
                Directory.CreateDirectory(settings.SchemaFolder);
                File.WriteAllText(settings.InputFolder + "/texts.csv", "idx,text\n1001,hello");
                File.WriteAllText(settings.SchemaFolder + "/texts.json", SchemaJson.Replace("Assets/Game/Data/texts.csv", settings.InputFolder + "/texts.csv").Replace("Game.Data", "MyLab.Examples.DataTables").Replace("TextTable", "TextDataTable").Replace("generated", "existing"));
                test(settings, folder, id);
            }
            finally
            {
                DataTableImportProfiles.Unregister(id);
                UnityEngine.Object.DestroyImmediate(settings);
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
        }
    }

    public sealed class ImportFkRow : DataRow
    {
        [Name("text")]
        public string Text
        {
            get; set;
        }
        [Name("parent"), Optional]
        public uint? ParentId
        {
            get; set;
        }
    }

    public sealed class ImportFkTable : CsvDataTable<ImportFkRow>
    {
        protected override void ValidateRow(ImportFkRow row)
        {
            if (row.Text == "forbidden")
                throw new InvalidDataException("hook rejection");
        }
    }
}
