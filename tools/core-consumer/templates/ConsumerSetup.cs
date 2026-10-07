using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace TPLabConsumer
{
    /// <summary>One preparation launch: import optional samples, then generate owned importer DTOs.</summary>
    public static class ConsumerSetup
    {
        public static void Perform() => PerformAsync().Forget();

        private static async UniTaskVoid PerformAsync()
        {
            var report = new SetupReport();
            EditorApplication.LockReloadAssemblies();
            try
            {
                if (Environment.GetEnvironmentVariable("TPLAB_CONSUMER_SAMPLES") == "1")
                {
                    report.imports = new[] { Import("com.tplab.core"), Import("com.tplab.input") };
                    report.samplesImported = report.imports.All(item => item.imported);
                    if (!report.samplesImported) throw new InvalidOperationException("Sample.Import did not copy both optional samples.");
                }
#if TPLAB_EDITOR_CONSUMER
                report.importer = await EditorProbe.RunAsync();
#endif
                report.success = true;
            }
            catch (Exception exception)
            {
                report.error = exception.ToString();
#if TPLAB_EDITOR_CONSUMER
                if (exception is EditorProbe.ProbeException probe) report.importer = probe.Result;
#endif
                Debug.LogException(exception);
            }
            finally
            {
                string path = Environment.GetEnvironmentVariable("TPLAB_CONSUMER_SETUP_RESULT");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(report, true));
                EditorApplication.UnlockReloadAssemblies();
                EditorApplication.Exit(report.success ? 0 : 1);
            }
        }

        private static ImportedSample Import(string package)
        {
            var samples = Sample.FindByPackage(package, "0.0.1").ToArray();
            if (samples.Length != 1) throw new InvalidOperationException("Expected one optional sample for " + package);
            var sample = samples[0];
            if (Directory.Exists(sample.importPath)) throw new InvalidOperationException("Fresh consumer already contains an imported sample.");
            bool imported = sample.Import((Sample.ImportOptions)0);
            return new ImportedSample { package = package, name = sample.displayName, path = sample.importPath,
                imported = imported && Directory.Exists(sample.importPath) };
        }

        [Serializable]
        private sealed class ImportedSample
        {
            public string package;
            public string name;
            public string path;
            public bool imported;
        }

        [Serializable]
        private sealed class SetupReport
        {
            public bool success;
            public bool samplesImported;
            public ImportedSample[] imports;
            public string error;
#if TPLAB_EDITOR_CONSUMER
            public EditorProbe.ProbeResult importer;
#endif
        }
    }
}
