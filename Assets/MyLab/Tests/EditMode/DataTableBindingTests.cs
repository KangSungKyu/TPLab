using System;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Cysharp.Threading.Tasks;
using MyLab.Core.DataTables;
using NUnit.Framework;

namespace MyLab.Core.Tests
{
    public sealed class DataTableBindingTests
    {
        public interface ITextLookup { string GetText(uint idx); }
        public sealed class ProjectTextTable : CsvDataTable<TextRow>, ITextLookup
        {
            public string GetText(uint idx) => Rows[idx].Text;
        }

        private static void Register(DataTableManager manager, uint kind, string name, Func<string> csv,
            Func<ProjectTextTable> factory = null)
            => manager.RegisterTable<TextRow, ProjectTextTable>(kind, name, _ => UniTask.FromResult(csv()), factory ?? (() => new ProjectTextTable()));

        [Test]
        public void AllBindingsAndRowsShareInstancesAndOldGenerationSurvivesReloadAndDispose()
        {
            var manager = new DataTableManager();
            manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
            string csv = "idx,text\n1001,old";
            Register(manager, 1, "text", () => csv);
            Register(manager, 2, "other", () => "idx,text");
            manager.BindTable<ITextLookup>("text");
            var old = manager.LoadAsync().GetAwaiter().GetResult();
            var table = old.GetTable<ProjectTextTable>("text");
            Assert.That(old.GetTable<ITextLookup>(1001), Is.SameAs(table));
            Assert.That(old.GetTable<IDataTable<TextRow>>("text"), Is.SameAs(table));
            Assert.That(old.GetTable<uint, TextRow>("text"), Is.SameAs(table.Rows));
            Assert.That(table.TryGet(1001, out var row), Is.True);
            Assert.That(row, Is.SameAs(old.Get<TextRow>(1001)));
            Assert.That(old.GetTable<ProjectTextTable>(1009), Is.SameAs(table), "Table routing does not require an existing row");
            Assert.That(old.GetTable<ProjectTextTable>(2001).Count, Is.Zero);
            Assert.Throws<ArgumentException>(() => old.GetTable<ITextLookup>(0u));
            Assert.Throws<KeyNotFoundException>(() => old.GetTable<ITextLookup>(9001u));
            Assert.Throws<KeyNotFoundException>(() => old.GetTable<ITextLookup>("missing"));
            Assert.Throws<InvalidOperationException>(() => old.GetTable<ITextLookup>("other"));
            csv = "idx,text\n1001,new";
            var next = manager.LoadAsync().GetAwaiter().GetResult();
            Assert.That(next.GetTable<ProjectTextTable>("text"), Is.Not.SameAs(table));
            manager.Dispose();
            Assert.That(old.GetTable<ITextLookup>("text").GetText(1001), Is.EqualTo("old"));
            Assert.That(next.Get<TextRow>(1001).Text, Is.EqualTo("new"));
        }

        [Test]
        public void BindingValidatesDeclaredInterfacesAndLeavesNoPartialRegistration()
        {
            using (var manager = new DataTableManager())
            {
                manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
                Register(manager, 1, "text", () => "idx,text");
                Assert.Throws<ArgumentException>(() => manager.BindTable<ProjectTextTable>("text"));
                Assert.Throws<KeyNotFoundException>(() => manager.BindTable<ITextLookup>("missing"));
                Assert.Throws<ArgumentException>(() => manager.BindTable<IDisposable>("text"));
                Assert.Throws<ArgumentException>(() => manager.BindTable<IDataTable<TextRow>>("text"));
                manager.BindTable<ITextLookup>("text");
                Assert.Throws<ArgumentException>(() => manager.BindTable<ITextLookup>("text"));
                manager.LoadAsync().GetAwaiter().GetResult();
                Assert.Throws<InvalidOperationException>(() => manager.BindTable<ITextLookup>("text"));
            }
        }

        [TestCase("null")]
        [TestCase("exception")]
        [TestCase("reuse")]
        public void FactoryFailurePreservesOldTable(string failure)
        {
            using (var manager = new DataTableManager())
            {
                manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
                var first = new ProjectTextTable();
                int calls = 0;
                Register(manager, 1, "text", () => "idx,text\n1001,one", () =>
                {
                    if (++calls == 1) return first;
                    if (failure == "null") return null;
                    if (failure == "exception") throw new IOException("Factory failure");
                    return first;
                });
                var old = manager.LoadAsync().GetAwaiter().GetResult();
                Assert.Throws<InvalidDataException>(() => manager.LoadAsync().GetAwaiter().GetResult());
                Assert.That(manager.Snapshot, Is.SameAs(old));
                Assert.That(old.GetTable<ProjectTextTable>("text"), Is.SameAs(first));
                Assert.That(first.Rows[1001].Text, Is.EqualTo("one"));
            }
        }

        [Test]
        public void FactoryCannotReuseFailedCandidateEvenWithAnotherManager()
        {
            var table = new ProjectTextTable();
            using (var first = new DataTableManager())
            {
                first.RegisterIdxRouter(new DecimalIdxCodec(1000));
                Register(first, 1, "text", () => "idx", () => table);
                Assert.Throws<InvalidDataException>(() => first.LoadAsync().GetAwaiter().GetResult());
            }
            using (var second = new DataTableManager())
            {
                second.RegisterIdxRouter(new DecimalIdxCodec(1000));
                Register(second, 1, "text", () => "idx,text\n1001,one", () => table);
                var error = Assert.Throws<InvalidDataException>(() => second.LoadAsync().GetAwaiter().GetResult());
                Assert.That(error.ToString(), Does.Contain("fresh table"));
                Assert.That(second.Snapshot, Is.Null);
            }
        }

        public sealed class LinkRow : DataRow
        {
            [Name("ref")]
            public uint? Reference { get; set; }
        }

        [TestCase(null, false, true)]
        [TestCase(null, true, false)]
        [TestCase("0", false, false)]
        [TestCase("2001", true, true)]
        [TestCase("2000", true, false)]
        [TestCase("1001", true, false)]
        [TestCase("3001", true, false)]
        [TestCase("2002", true, false)]
        public void ForeignKeyValidatesNullFullFormatKindAndActualCandidatePk(string value, bool required, bool valid)
        {
            using (var manager = new DataTableManager())
            {
                manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
                manager.RegisterTable<LinkRow, CsvDataTable<LinkRow>>(1, "links", _ => UniTask.FromResult("idx,ref\n1001," + value), () => new CsvDataTable<LinkRow>());
                Register(manager, 2, "text", () => "idx,text\n2001,one");
                manager.RegisterForeignKey<LinkRow, TextRow>("links", "ref", row => row.Reference, 2, required);
                if (valid)
                {
                    Assert.That(manager.LoadAsync().GetAwaiter().GetResult().Get<LinkRow>(1001).Reference,
                        Is.EqualTo(value == null ? (uint?)null : uint.Parse(value)));
                }
                else
                {
                    var error = Assert.Throws<InvalidDataException>(() => manager.LoadAsync().GetAwaiter().GetResult());
                    Assert.That(error.Message, Does.Contain("links").And.Contain("1001").And.Contain("ref").And.Contain("2"));
                    Assert.That(error.Message, Does.Contain(value ?? "null"));
                    Assert.That(manager.Snapshot, Is.Null);
                }
            }
        }

        [Test]
        public void ForeignKeysUseNewCandidateAndFailureKeepsAllPriorBindings()
        {
            using (var manager = new DataTableManager())
            {
                manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
                string links = "idx,ref\n1001,2001";
                string texts = "idx,text\n2001,old";
                manager.RegisterTable<LinkRow, CsvDataTable<LinkRow>>(1, "links", _ => UniTask.FromResult(links), () => new CsvDataTable<LinkRow>());
                Register(manager, 2, "text", () => texts);
                manager.BindTable<ITextLookup>("text");
                manager.RegisterForeignKey<LinkRow, TextRow>("links", "ref", row => row.Reference, 2, true);
                var old = manager.LoadAsync().GetAwaiter().GetResult();
                texts = "idx,text\n2002,new";
                Assert.Throws<InvalidDataException>(() => manager.LoadAsync().GetAwaiter().GetResult());
                Assert.That(manager.Snapshot, Is.SameAs(old));
                Assert.That(old.GetTable<ITextLookup>("text").GetText(2001), Is.EqualTo("old"));
                links = "idx,ref\n1001,2002";
                Assert.That(manager.LoadAsync().GetAwaiter().GetResult().Get<TextRow>(2002).Text, Is.EqualTo("new"));
            }
        }

        [Test]
        public void ForeignKeyConfigurationChecksExactRegisteredTypesBeforeLoad()
        {
            using (var manager = new DataTableManager())
            {
                manager.RegisterTable<LinkRow, CsvDataTable<LinkRow>>(1, "links", _ => UniTask.FromResult("idx,ref"), () => new CsvDataTable<LinkRow>());
                Register(manager, 2, "text", () => "idx,text");
                Assert.Throws<KeyNotFoundException>(() => manager.RegisterForeignKey<LinkRow, TextRow>("missing", "ref", r => r.Reference, 2, true));
                Assert.Throws<InvalidOperationException>(() => manager.RegisterForeignKey<TextRow, TextRow>("links", "ref", r => r.Id, 2, true));
                Assert.Throws<KeyNotFoundException>(() => manager.RegisterForeignKey<LinkRow, TextRow>("links", "ref", r => r.Reference, 9, true));
                Assert.Throws<InvalidOperationException>(() => manager.RegisterForeignKey<LinkRow, ResourceKeyRow>("links", "ref", r => r.Reference, 2, true));
                Assert.Throws<ArgumentException>(() => manager.RegisterForeignKey<LinkRow, TextRow>("links", " ", r => r.Reference, 2, true));
                Assert.Throws<ArgumentNullException>(() => manager.RegisterForeignKey<LinkRow, TextRow>("links", "ref", null, 2, true));
                manager.RegisterForeignKey<LinkRow, TextRow>("links", "ref", r => r.Reference, 2, true);
                manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
                Assert.That(manager.LoadAsync().GetAwaiter().GetResult().Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void SelfAndCyclicReferencesValidateExistenceWithoutTraversal()
        {
            using (var manager = new DataTableManager())
            {
                manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
                manager.RegisterTable<LinkRow, CsvDataTable<LinkRow>>(1, "links", _ => UniTask.FromResult("idx,ref\n1001,1001\n1002,1003\n1003,1002"), () => new CsvDataTable<LinkRow>());
                manager.RegisterForeignKey<LinkRow, LinkRow>("links", "ref", r => r.Reference, 1, true);
                Assert.That(manager.LoadAsync().GetAwaiter().GetResult().GetTable<IDataTable<LinkRow>>("links").Count, Is.EqualTo(3));
            }
        }

        [Test]
        public void DefaultResourceTableRejectsBlankKeysButTextAllowsEmpty()
        {
            using (var manager = new DataTableManager())
            {
                manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
                string csv = "idx,key\n2001, ";
                manager.RegisterTable<TextRow, TextDataTable>(1, "text", _ => UniTask.FromResult("idx,text\n1001,"), () => new TextDataTable());
                manager.RegisterTable<ResourceKeyRow, ResourceKeyDataTable>(2, "resource", _ => UniTask.FromResult(csv), () => new ResourceKeyDataTable());
                Assert.Throws<InvalidDataException>(() => manager.LoadAsync().GetAwaiter().GetResult());
                csv = "idx,key\n2001,not-yet-loaded";
                Assert.That(manager.LoadAsync().GetAwaiter().GetResult().Get<TextRow>(1001).Text, Is.Empty);
                Assert.That(manager.Get<ResourceKeyRow>(2001).Key, Is.EqualTo("not-yet-loaded"));
            }
        }
    }
}
