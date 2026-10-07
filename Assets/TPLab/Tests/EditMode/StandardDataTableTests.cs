using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using Cysharp.Threading.Tasks;
using TPLab.Core.DataTables;
using TPLab.Examples.DataTables;
using NUnit.Framework;

namespace TPLab.Core.Tests
{
    public sealed class StandardDataTableTests
    {
        [TestCase("TextRow")]
        [TestCase("ResourceKeyRow")]
        [TestCase("TextDataTable")]
        [TestCase("ResourceKeyDataTable")]
        public void CoreExcludesProjectTableTemplates(string typeName)
        {
            Assert.That(typeof(DataRow).Assembly.GetType("TPLab.Core.DataTables." + typeName), Is.Null,
                "Project-specific schemas belong to examples, outside the core assembly.");
        }

        private static void RegisterText(DataTableManager manager, uint kind, string name, Func<string> csv)
            => manager.RegisterTable<TextRow, TextDataTable>(kind, name, _ => UniTask.FromResult(csv()), () => new TextDataTable());

        [Test]
        public void RoutesExactDtoAndPreservesManualArbitraryKeys()
        {
            using (var manager = new DataTableManager())
            {
                manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
                RegisterText(manager, 1, "text", () => "idx,text\n1001,first");
                RegisterText(manager, 2, "other", () => "idx,text\n2001,second");
                manager.Register("manual", new[] { "key" }, _ => UniTask.FromResult("key\n0"), csv => csv.GetField<uint>("key"), key => key);
                manager.Register("codes", new[] { "code" }, _ => UniTask.FromResult("code\nITEM_A"), csv => csv.GetField("code"), key => key);
                var snapshot = manager.LoadAsync().GetAwaiter().GetResult();
                Assert.That(manager.Get<TextRow>(1001).Text, Is.EqualTo("first"));
                Assert.That(manager.Get<TextRow>(2001).Text, Is.EqualTo("second"));
                Assert.That(manager.TryGet(1001, out TextRow row), Is.True);
                Assert.That(row, Is.SameAs(snapshot.Get<TextRow>(1001)).And.SameAs(snapshot.GetTable<uint, TextRow>("text")[1001]));
                Assert.That(snapshot.GetTable<uint, uint>("manual")[0], Is.Zero);
                Assert.That(snapshot.GetTable<string, string>("codes")["ITEM_A"], Is.EqualTo("ITEM_A"));
                Assert.That(snapshot.Count, Is.EqualTo(4));
            }
        }

        [Test]
        public void RowMissesAreDistinctFromStateAndConfigurationErrors()
        {
            var manager = new DataTableManager();
            Assert.Throws<InvalidOperationException>(() => manager.Get<TextRow>(1001));
            Assert.Throws<InvalidOperationException>(() => manager.TryGet(1001, out TextRow _));
            manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
            RegisterText(manager, 1, "text", () => "idx,text\n1001,one");
            var snapshot = manager.LoadAsync().GetAwaiter().GetResult();
            Assert.Throws<ArgumentException>(() => manager.Get<TextRow>(0));
            Assert.Throws<ArgumentException>(() => manager.Get<TextRow>(1000));
            Assert.Throws<KeyNotFoundException>(() => manager.Get<TextRow>(9001));
            Assert.Throws<KeyNotFoundException>(() => manager.Get<TextRow>(1002));
            Assert.Throws<InvalidOperationException>(() => manager.Get<ResourceKeyRow>(1001));
            Assert.Throws<InvalidOperationException>(() => manager.Get<DataRow>(1001));
            foreach (uint idx in new[] { 0u, 1000u, 9001u, 1002u })
            {
                Assert.That(manager.TryGet(idx, out TextRow missing), Is.False);
                Assert.That(missing, Is.Null);
            }
            Assert.That(manager.TryGet(1001, out ResourceKeyRow wrong), Is.False);
            Assert.That(wrong, Is.Null);
            manager.Dispose();
            Assert.Throws<ObjectDisposedException>(() => manager.Get<TextRow>(1001));
            Assert.Throws<ObjectDisposedException>(() => manager.TryGet(1001, out TextRow _));
            Assert.That(snapshot.Get<TextRow>(1001).Text, Is.EqualTo("one"));
            using (var manual = new DataTableManager())
            {
                manual.Register("manual", new[] { "key" }, _ => UniTask.FromResult("key\n0"), c => c.GetField<int>("key"), k => k);
                var result = manual.LoadAsync().GetAwaiter().GetResult();
                Assert.Throws<InvalidOperationException>(() => result.TryGet(1001, out TextRow _));
            }
        }

        [Test]
        public void MissingRouterDoesNotDispatchOrFreezeAndRegistrationFailuresAreAtomic()
        {
            using (var manager = new DataTableManager())
            {
                int calls = 0;
                RegisterText(manager, 1, "text", () => { ++calls; return "idx,text"; });
                Assert.Throws<InvalidOperationException>(() => manager.LoadAsync());
                Assert.That(calls, Is.Zero);
                Assert.Throws<ArgumentNullException>(() => manager.RegisterIdxRouter(null));
                manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
                Assert.Throws<InvalidOperationException>(() => manager.RegisterIdxRouter(new DecimalIdxCodec(10)));
                Assert.Throws<ArgumentException>(() => RegisterText(manager, 1, "unique", () => "idx,text"));
                RegisterText(manager, 2, "unique", () => "idx,text");
                Assert.Throws<ArgumentException>(() => RegisterText(manager, 3, "text", () => "idx,text"));
                RegisterText(manager, 3, "third", () => "idx,text");
                Assert.Throws<ArgumentOutOfRangeException>(() => RegisterText(manager, 0, "zero", () => "idx,text"));
                Assert.Throws<ArgumentNullException>(() => manager.RegisterTable<TextRow, TextDataTable>(4, "null", null, () => new TextDataTable()));
                RegisterText(manager, 4, "null", () => "idx,text");
                var snapshot = manager.LoadAsync().GetAwaiter().GetResult();
                Assert.That(snapshot.Count, Is.EqualTo(4));
                Assert.That(snapshot.TryGet(2001, out TextRow _), Is.False);
                Assert.Throws<InvalidOperationException>(() => manager.RegisterIdxRouter(new DecimalIdxCodec(10)));
                Assert.Throws<InvalidOperationException>(() => RegisterText(manager, 5, "late", () => "idx,text"));
            }
        }

        [TestCase("idx,text\n0,zero")]
        [TestCase("idx,text\n1000,bad-local")]
        [TestCase("idx,text\n2001,wrong-kind")]
        [TestCase("idx,text\n1001,one\n1001,duplicate")]
        [TestCase("idx,text\n-1,negative")]
        [TestCase("idx,text\n4294967296,overflow")]
        [TestCase("idx,text\n1001")]
        [TestCase("idx,text,extra\n1001,one,\"unterminated")]
        [TestCase("idx,text,text")]
        [TestCase("idx,Text")]
        [TestCase("text")]
        public void InvalidStandardCsvPreservesPublishedGeneration(string invalid)
        {
            using (var manager = new DataTableManager())
            {
                manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
                string csv = "idx,text\n1001,old";
                RegisterText(manager, 1, "text", () => csv);
                var old = manager.LoadAsync().GetAwaiter().GetResult();
                csv = invalid;
                var error = Assert.Throws<InvalidDataException>(() => manager.LoadAsync().GetAwaiter().GetResult());
                Assert.That(error.Message, Does.Contain("text"));
                Assert.That(manager.Snapshot, Is.SameAs(old));
                Assert.That(manager.Get<TextRow>(1001).Text, Is.EqualTo("old"));
                csv = "idx,text\n1001,new";
                var next = manager.LoadAsync().GetAwaiter().GetResult();
                Assert.That(next.Get<TextRow>(1001).Text, Is.EqualTo("new"));
                Assert.That(old.Get<TextRow>(1001).Text, Is.EqualTo("old"));
            }
        }

        public sealed class MappedRow : DataRow
        {
            public decimal Amount { get; set; }
            [Optional]
            public string Note { get; set; }
        }

        public sealed class ConvertedRow : DataRow
        {
            [Name("amount")]
            [TypeConverter(typeof(SuffixedNumberConverter))]
            public uint Amount { get; set; }
            [Name("note"), Optional]
            public string Note { get; set; }
        }

        public sealed class SuffixedNumberConverter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                if (!text.EndsWith("u", StringComparison.Ordinal)) throw new FormatException("Expected unit suffix");
                return uint.Parse(text.Substring(0, text.Length - 1), System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        [Test]
        public void AttributesOptionalAndProjectConverterAreUsedForHeaderOnlyAndRows()
        {
            using (var manager = new DataTableManager())
            {
                manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
                string csv = "idx,amount\n1001,37u";
                manager.RegisterTable<ConvertedRow, CsvDataTable<ConvertedRow>>(1, "converted", _ => UniTask.FromResult(csv), () => new CsvDataTable<ConvertedRow>());
                Assert.That(manager.LoadAsync().GetAwaiter().GetResult().Get<ConvertedRow>(1001).Amount, Is.EqualTo(37));
                var old = manager.Snapshot;
                csv = "idx,amount\n1001,37";
                Assert.That(Assert.Throws<InvalidDataException>(() => manager.LoadAsync().GetAwaiter().GetResult()).ToString(), Does.Contain("Expected unit suffix"));
                Assert.That(manager.Snapshot, Is.SameAs(old));
                csv = "idx,amount";
                Assert.That(manager.LoadAsync().GetAwaiter().GetResult().GetTable<uint, ConvertedRow>("converted").Count, Is.Zero);
            }
        }

        public sealed class MappedTable : CsvDataTable<MappedRow>
        {
            public sealed class Map : ClassMap<MappedRow>
            {
                public Map()
                {
                    Map(row => row.Id).Name("pk");
                    Map(row => row.Amount).Name("value");
                    Map(row => row.Note).Name("note").Optional();
                }
            }
            protected override void ConfigureMapping(CsvContext context) => context.RegisterClassMap<Map>();
            protected override void ValidateRow(MappedRow row)
            {
                if (row.Amount < 0) throw new InvalidDataException("Negative amount");
            }
            protected override void ValidateTable(IReadOnlyDictionary<uint, MappedRow> rows)
            {
                if (rows.Count > 1) throw new InvalidDataException("Only one row");
            }
        }

        [Test]
        public void ClassMapOptionalConversionAndHooksUseSameCsvPipeline()
        {
            using (var manager = new DataTableManager())
            {
                manager.RegisterIdxRouter(new DecimalIdxCodec(1000));
                string csv = "pk,value\n1001,1.25";
                manager.RegisterTable<MappedRow, MappedTable>(1, "mapped", _ => UniTask.FromResult(csv), () => new MappedTable());
                Assert.That(manager.LoadAsync().GetAwaiter().GetResult().Get<MappedRow>(1001).Amount, Is.EqualTo(1.25m));
                foreach (string bad in new[] { "pk", "pk,value\n1001,-1", "pk,value\n1001,1\n1002,2", "pk,value\n1001,invalid" })
                {
                    csv = bad;
                    Assert.Throws<InvalidDataException>(() => manager.LoadAsync().GetAwaiter().GetResult());
                }
                csv = "pk,value";
                Assert.That(manager.LoadAsync().GetAwaiter().GetResult().GetTable<uint, MappedRow>("mapped").Count, Is.Zero);
            }
        }

        private sealed class ThrowingRouter : IIdxRouter
        {
            public bool Broken;
            public bool TryGetDataType(uint idx, out uint dataType)
            {
                if (Broken) throw new ArithmeticException("Project router bug");
                dataType = idx / 1000;
                return true;
            }
        }

        [Test]
        public void RouterImplementationErrorsAreNotReportedAsMisses()
        {
            using (var manager = new DataTableManager())
            {
                // Deliberately mutable faulty router, solely to exercise query error propagation after a valid load.
                var router = new ThrowingRouter();
                manager.RegisterIdxRouter(router);
                RegisterText(manager, 1, "text", () => "idx,text\n1001,one");
                manager.LoadAsync().GetAwaiter().GetResult();
                router.Broken = true;
                Assert.Throws<ArithmeticException>(() => manager.TryGet(1001, out TextRow _));
            }
        }

        [Test]
        public void ThreePartProjectRouterKeepsLocalCategoriesInOneTable()
        {
            using (var manager = new DataTableManager())
            {
                manager.RegisterIdxRouter(new IdxCodecTests.ProjectCodec());
                RegisterText(manager, 2, "text", () => "idx,text\n2070037,worker7\n2080037,worker8");
                manager.LoadAsync().GetAwaiter().GetResult();
                Assert.That(manager.Get<TextRow>(2070037).Text, Is.EqualTo("worker7"));
                Assert.That(manager.Get<TextRow>(2080037).Text, Is.EqualTo("worker8"));
                Assert.That(manager.TryGet(2070000, out TextRow _), Is.False);
            }
        }
    }
}
