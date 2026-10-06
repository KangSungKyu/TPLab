using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace MyLab.Core.Editor.DataTables
{
    /// <summary>Owns only manifest-tracked generated files. Preserves meta files and rejects user edits.</summary>
    public static class DataTableGeneratedFiles
    {
        [Serializable]
        internal sealed class Record
        {
            public string Owner;
            public string Contract;
            public string InputGuid;
            public string LastValidated;
            public Entry[] Files;
        }

        [Serializable]
        internal sealed class Entry
        {
            public string Name; public string Hash; public string LastGoodContent;
        }

        /// <summary>Validates an Assets path and rejects traversal, links and protected generation destinations.</summary>
        public static string AssetPath(string path, bool output = false)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Contains('\\') || path.Split('/').Any(p => p == ".." || p == "." || p.Length == 0) ||
                !(path == "Assets" || path.StartsWith("Assets/", StringComparison.Ordinal)) || path.IndexOfAny(new[] { ':', '\r', '\n', '\0' }) >= 0)
                throw new InvalidDataException("Use a normalized Assets path: " + path);
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string full = Path.GetFullPath(Path.Combine(root, path));
            for (var parent = new DirectoryInfo(full); parent != null && parent.FullName.Length >= root.Length; parent = parent.Parent)
                if (parent.Exists && (parent.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Linked asset paths are not supported: " + path);
            if (File.Exists(full) && (File.GetAttributes(full) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Linked asset is not supported: " + path);
            if (output && (path == "Assets" || path.Split('/').Any(p => p.Equals("Editor", StringComparison.OrdinalIgnoreCase)) ||
                new[] { "Assets/MyLab/Core", "Assets/MyLab/Tests", "Assets/MyLab/Validation", "Assets/MyLab/Editor" }.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase) || path.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase))))
                throw new InvalidDataException("Generation requires an unprotected runtime folder: " + path);
            return full;
        }
        /// <summary>Checks ownership without writing. Automatic updates cannot change an existing schema contract.</summary>
        public static void Check(string outputFolder, string ownerId, string contract, IReadOnlyDictionary<string, string> files, bool automatic = false)
        {
            AssetPath(outputFolder, true);
            DataTableGenerator.CheckIdentifier(ownerId);
            if (files == null || files.Count == 0 || string.IsNullOrEmpty(contract))
                throw new InvalidDataException("A generation contract and source files are required.");
            var record = ReadRecord(outputFolder, ownerId);
            if (record != null && (record.Owner != ownerId || record.Files == null ||
                !record.Files.Select(f => f.Name).OrderBy(n => n, StringComparer.Ordinal).SequenceEqual(files.Keys.OrderBy(n => n, StringComparer.Ordinal))))
                throw new InvalidDataException("Generated file names changed. Review and migrate references before renaming types.");
            if (automatic && record != null && record.Contract != contract)
                throw new InvalidDataException("Schema contract changed. Apply it explicitly after review.");
            foreach (var pair in files)
            {
                if (Path.GetFileName(pair.Key) != pair.Key || !pair.Key.EndsWith(".g.cs", StringComparison.Ordinal) || pair.Value == null)
                    throw new InvalidDataException("Only named generated C# files can be owned.");
                string assetPath = outputFolder + "/" + pair.Key;
                string full = AssetPath(assetPath, true);
                var entry = record?.Files.SingleOrDefault(f => f.Name == pair.Key);
                if (File.Exists(full) && (entry == null || entry.Hash != Hash(File.ReadAllText(full))))
                    throw new InvalidDataException("Unknown or user-modified generated file: " + assetPath);
                if (!File.Exists(full) && File.Exists(full + ".meta") && entry == null)
                    throw new InvalidDataException("Unowned meta file: " + assetPath);
            }
        }
        /// <summary>Applies an owned source pair and manifest with rollback on I/O failure; returns whether source changed.</summary>
        public static bool Apply(string outputFolder, string ownerId, string contract, IReadOnlyDictionary<string, string> files, bool automatic = false)
        {
            Check(outputFolder, ownerId, contract, files, automatic);
            string folder = AssetPath(outputFolder, true);
            Directory.CreateDirectory(folder);
            var old = ReadRecord(outputFolder, ownerId);
            var record = new Record
            {
                Owner = ownerId,
                Contract = contract,
                InputGuid = old?.InputGuid,
                LastValidated = old?.Contract == contract ? old.LastValidated : null,
                Files = files.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new Entry
                {
                    Name = p.Key,
                    Hash = Hash(p.Value),
                    LastGoodContent = old?.Files.FirstOrDefault(f => f.Name == p.Key)?.LastGoodContent
                }).ToArray()
            };
            var writes = new Dictionary<string, string>();
            foreach (var pair in files)
            {
                string path = AssetPath(outputFolder + "/" + pair.Key, true);
                if (!File.Exists(path) || File.ReadAllText(path) != pair.Value)
                    writes.Add(path, pair.Value);
            }
            bool changed = writes.Count != 0;
            string manifest = RecordPath(outputFolder, ownerId);
            string json = JsonConvert.SerializeObject(record, Formatting.Indented) + "\n";
            if (!File.Exists(manifest) || File.ReadAllText(manifest) != json)
                writes.Add(manifest, json);
            var originals = writes.Keys.ToDictionary(p => p, p => File.Exists(p) ? File.ReadAllBytes(p) : null);
            var staged = new Dictionary<string, string>();
            try
            {
                foreach (var pair in writes)
                {
                    string stage = pair.Key + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    File.WriteAllText(stage, pair.Value, new UTF8Encoding(false));
                    staged.Add(pair.Key, stage);
                }
                foreach (var pair in staged)
                {
                    if (File.Exists(pair.Key))
                        File.Replace(pair.Value, pair.Key, null);
                    else
                        File.Move(pair.Value, pair.Key);
                }
            }
            catch
            {
                foreach (var pair in originals)
                {
                    if (pair.Value == null)
                    {
                        if (File.Exists(pair.Key))
                            File.Delete(pair.Key);
                    }
                    else
                        File.WriteAllBytes(pair.Key, pair.Value);
                }
                throw;
            }
            finally { foreach (string stage in staged.Values) if (File.Exists(stage)) File.Delete(stage); }
            return changed;
        }

        internal static string Hash(string value)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
        }
        internal static string RecordPath(string folder, string id) => AssetPath(folder + "/" + id + ".tableimport.json", true);
        internal static Record ReadRecord(string folder, string id)
        {
            string path = RecordPath(folder, id);
            return File.Exists(path) ? JsonConvert.DeserializeObject<Record>(File.ReadAllText(path)) ?? throw new InvalidDataException("Invalid generation manifest.") : null;
        }
        internal static void SaveRecord(string folder, Record record)
        {
            File.WriteAllText(RecordPath(folder, record.Owner), JsonConvert.SerializeObject(record, Formatting.Indented) + "\n", new UTF8Encoding(false));
        }
    }
}
