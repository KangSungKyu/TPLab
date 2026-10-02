using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CsvHelper;
using Cysharp.Threading.Tasks;
using MyLab.Core.DataTables;
using NUnit.Framework;

namespace MyLab.Core.Tests
{
    public sealed class DataTableManagerTests
    {
        private sealed class Row
        {
            internal readonly int Id;
            internal readonly string Name;
            internal readonly decimal Amount;
            internal Row(int id, string name, decimal amount = 0)
            {
                Id = id;
                Name = name;
                Amount = amount;
            }
        }

        private static Row ReadRow(CsvReader csv) => new Row(csv.GetField<int>("Id"), csv.GetField("Name"));

        private static void Register(DataTableManager manager, string name, Func<string> source, Action<Row> validate = null)
            => manager.Register(name, new[] { "Id", "Name" }, token => UniTask.FromResult(source()), ReadRow, row => row.Id, validate);

        [Test]
        public void LoadsMultipleTablesAndExposesReadOnlyContainers()
        {
            using (var manager = new DataTableManager())
            {
                Register(manager, "first", () => "Id,Name,Unused\n1,one,\"valid,extra\"\n2,two,unused");
                manager.Register("second", new[] { "Key", "Value" }, token => UniTask.FromResult("Key,Value\nentry,text"),
                    csv => csv.GetField("Value"), value => value);
                Assert.That(manager.Snapshot, Is.Null);
                var result = manager.LoadAsync().GetAwaiter().GetResult();
                Assert.That(manager.Snapshot, Is.SameAs(result));
                Assert.That(result.Count, Is.EqualTo(2));
                var rows = result.GetTable<int, Row>("first");
                Assert.That(rows[2].Name, Is.EqualTo("two"));
                Assert.Throws<NotSupportedException>(() => ((IDictionary<int, Row>)rows).Clear());
                Assert.That(result.GetTable<string, string>("second")["text"], Is.EqualTo("text"));
                Assert.Throws<KeyNotFoundException>(() => result.GetTable<int, Row>("missing"));
                Assert.Throws<InvalidOperationException>(() => result.GetTable<string, Row>("first"));
                Assert.Throws<ArgumentException>(() => result.GetTable<int, Row>(" "));
            }
        }

        [Test]
        public void CsvReaderHandlesBomQuotesMultilineAndInvariantDecimals()
        {
            var previousCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
                using (var manager = new DataTableManager())
                {
                    manager.Register("data", new[] { "Id", "Name", "Amount" },
                        token => UniTask.FromResult("\uFEFFId,Name,Amount\r\n1,\"a,\"\"quoted\"\"\r\nb\",1.25"),
                        csv => new Row(csv.GetField<int>("Id"), csv.GetField("Name"), csv.GetField<decimal>("Amount")), row => row.Id);
                    var row = manager.LoadAsync().GetAwaiter().GetResult().GetTable<int, Row>("data")[1];
                    Assert.That(row.Name, Is.EqualTo("a,\"quoted\"\r\nb"));
                    Assert.That(row.Amount, Is.EqualTo(1.25m));
                }
            }
            finally { Thread.CurrentThread.CurrentCulture = previousCulture; }
        }

        [TestCase("Id,Name\n1,one\n1,duplicate")]
        [TestCase("Id,Name\nwrong,one")]
        [TestCase("Id,Name\n1")]
        [TestCase("Id,Name\n1,one,extra")]
        [TestCase("Id,Name\n1,\"unterminated")]
        [TestCase("Id,Name,Unused\n1,one,\"unterminated")]
        [TestCase("Name\none")]
        [TestCase("Id,Name,Name\n1,one,two")]
        [TestCase("Id,,Name\n1,x,one")]
        [TestCase("Id")]
        [TestCase("")]
        [TestCase(null)]
        public void BadCsvKeepsEveryPreviouslyPublishedTable(string invalidCsv)
        {
            using (var manager = new DataTableManager())
            {
                string first = "Id,Name\n1,old-first";
                string second = "Id,Name\n2,old-second";
                Register(manager, "first", () => first);
                Register(manager, "second", () => second);
                var old = manager.LoadAsync().GetAwaiter().GetResult();
                first = "Id,Name\n3,new-first";
                second = invalidCsv;
                var error = Assert.Throws<InvalidDataException>(() => manager.LoadAsync().GetAwaiter().GetResult());
                Assert.That(error.Message, Does.Contain("second"));
                Assert.That(manager.Snapshot, Is.SameAs(old));
                Assert.That(old.GetTable<int, Row>("first")[1].Name, Is.EqualTo("old-first"));
                second = "Id,Name\n4,new-second";
                var next = manager.LoadAsync().GetAwaiter().GetResult();
                Assert.That(next, Is.Not.SameAs(old));
                Assert.That(next.GetTable<int, Row>("first")[3].Name, Is.EqualTo("new-first"));
                Assert.That(old.GetTable<int, Row>("second")[2].Name, Is.EqualTo("old-second"));
            }
        }

        [Test]
        public void RequiredValuesAreProjectValidationAndFailureDoesNotPublish()
        {
            using (var manager = new DataTableManager())
            {
                Register(manager, "rows", () => "Id,Name\n1,", row =>
                {
                    if (string.IsNullOrWhiteSpace(row.Name)) throw new InvalidDataException("Name required");
                });
                var error = Assert.Throws<InvalidDataException>(() => manager.LoadAsync().GetAwaiter().GetResult());
                Assert.That(error.ToString(), Does.Contain("Name required"));
                Assert.That(manager.Snapshot, Is.Null);
            }
        }

        [Test]
        public void CrossTableValidationReadsCandidateAndFailureKeepsOldSnapshot()
        {
            using (var manager = new DataTableManager())
            {
                string parent = "Id,Name\n1,parent";
                string child = "Id,Name\n1,child";
                Register(manager, "parent", () => parent);
                Register(manager, "child", () => child);
                int validationCalls = 0;
                manager.AddValidator(candidate =>
                {
                    ++validationCalls;
                    Assert.That(manager.Snapshot, Is.Not.SameAs(candidate));
                    var parents = candidate.GetTable<int, Row>("parent");
                    foreach (var row in candidate.GetTable<int, Row>("child").Values)
                        if (!parents.ContainsKey(row.Id)) throw new InvalidDataException("missing parent");
                });
                var old = manager.LoadAsync().GetAwaiter().GetResult();
                parent = "Id,Name\n2,new-parent";
                Assert.Throws<InvalidDataException>(() => manager.LoadAsync().GetAwaiter().GetResult());
                Assert.That(manager.Snapshot, Is.SameAs(old));
                child = "Id,Name\n2,new-child";
                Assert.That(manager.LoadAsync().GetAwaiter().GetResult().GetTable<int, Row>("child")[2].Name, Is.EqualTo("new-child"));
                Assert.That(validationCalls, Is.EqualTo(3));
            }
        }

        [Test]
        public void HeaderOnlyTablesAreAllowedAndValidatorsCanRequireRows()
        {
            using (var manager = new DataTableManager())
            {
                Register(manager, "empty", () => "Id,Name\n");
                Assert.That(manager.LoadAsync().GetAwaiter().GetResult().GetTable<int, Row>("empty").Count, Is.Zero);
            }
            using (var manager = new DataTableManager())
            {
                Assert.Throws<InvalidOperationException>(() => manager.LoadAsync().GetAwaiter().GetResult());
                Register(manager, "required", () => "Id,Name\n");
                manager.AddValidator(candidate =>
                {
                    if (candidate.GetTable<int, Row>("required").Count == 0) throw new InvalidDataException("rows required");
                });
                Assert.Throws<InvalidDataException>(() => manager.LoadAsync().GetAwaiter().GetResult());
            }
        }

        [Test]
        public void RegistrationRejectsBadInputsAndFreezesAtFirstLoad()
        {
            using (var manager = new DataTableManager())
            {
                Assert.Throws<ArgumentException>(() => Register(manager, " ", () => "Id,Name"));
                Assert.Throws<ArgumentException>(() => manager.Register<int, Row>("bad", new string[0],
                    token => UniTask.FromResult(""), ReadRow, row => row.Id));
                Assert.Throws<ArgumentException>(() => manager.Register<int, Row>("bad", new[] { "Id", "Id" },
                    token => UniTask.FromResult(""), ReadRow, row => row.Id));
                Assert.Throws<ArgumentNullException>(() => manager.Register<int, Row>("bad", new[] { "Id" }, null, ReadRow, row => row.Id));
                Assert.Throws<ArgumentNullException>(() => manager.AddValidator(null));
                string[] headers = { "Id", "Name" };
                manager.Register("ok", headers, token => UniTask.FromResult("Id,Name\n1,one"), ReadRow, row => row.Id);
                headers[0] = "mutated";
                Assert.Throws<ArgumentException>(() => Register(manager, "ok", () => "Id,Name"));
                manager.LoadAsync().GetAwaiter().GetResult();
                Assert.Throws<InvalidOperationException>(() => Register(manager, "later", () => "Id,Name"));
                Assert.Throws<InvalidOperationException>(() => manager.AddValidator(_ => { }));
            }
        }

        [Test]
        public void NullRowsAndNullKeysAreRejected()
        {
            using (var manager = new DataTableManager())
            {
                manager.Register<int, Row>("null-row", new[] { "Id" }, token => UniTask.FromResult("Id\n1"), csv => null, row => row.Id);
                Assert.Throws<InvalidDataException>(() => manager.LoadAsync().GetAwaiter().GetResult());
            }
            using (var manager = new DataTableManager())
            {
                manager.Register<string, Row>("null-key", new[] { "Id" }, token => UniTask.FromResult("Id\n1"),
                    csv => new Row(1, "one"), row => null);
                Assert.Throws<InvalidDataException>(() => manager.LoadAsync().GetAwaiter().GetResult());
            }
        }

        [Test]
        public void ValidatorCannotPublishAfterDisposingOwner()
        {
            using (var manager = new DataTableManager())
            {
                Register(manager, "rows", () => "Id,Name\n1,one");
                manager.AddValidator(_ => manager.Dispose());
                Assert.Catch<OperationCanceledException>(() => manager.LoadAsync().GetAwaiter().GetResult());
                Assert.That(manager.Snapshot, Is.Null);
                Assert.That(manager.IsDisposed, Is.True);
            }
        }
    }
}
