using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace TPLabPackageConsumer
{
    /// <summary>Records actual installed packages and loaded assemblies; imports only explicitly selected package samples.</summary>
    public static class PackageConsumerProbe
    {
        /// <summary>Runs one batch phase. Import and post-reload compile verification use separate Unity processes.</summary>
        public static void Perform()
        {
            var report = new Report();
            bool locked = false;
            int exitCode = 1;
            try
            {
                string planPath = Environment.GetEnvironmentVariable("TPLAB_PACKAGE_PLAN");
                var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(planPath));
                report.runId = plan.runId;
                report.sourceRevision = plan.sourceRevision;
                report.projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                report.phase = Environment.GetEnvironmentVariable("TPLAB_PACKAGE_PHASE");
                if (report.projectPath != plan.projectPath || (report.phase != "import" && report.phase != "verify"))
                {
                    throw new InvalidOperationException("Consumer project or phase mismatch.");
                }
                report.installedPackages = PackageInfo.GetAllRegisteredPackages()
                    .Where(item => item.name.StartsWith("com.tplab.", StringComparison.Ordinal))
                    .Select(item => new InstalledPackage { name = item.name, version = item.version, resolvedPath = item.resolvedPath, source = item.source.ToString() }).ToArray();
                if (report.phase == "import")
                {
                    EditorApplication.LockReloadAssemblies();
                    locked = true;
                }
                report.imports = plan.samples.Select(selected =>
                {
                    var available = Sample.FindByPackage(selected.package, plan.version).ToArray();
                    if (available.Length != 1 || available[0].displayName != selected.displayName)
                    {
                        throw new InvalidOperationException("Selected sample metadata mismatch: " + selected.package);
                    }
                    var sample = available[0];
                    if (report.phase == "import")
                    {
                        if (Directory.Exists(sample.importPath))
                        {
                            throw new InvalidOperationException("Fresh sample import target already exists.");
                        }
                        if (!sample.Import((Sample.ImportOptions)0))
                        {
                            throw new InvalidOperationException("Public Sample.Import failed.");
                        }
                    }
                    string importedPath = Path.GetFullPath(sample.importPath);
                    return new ImportedSample { package = selected.package, importPath = importedPath, imported = Directory.Exists(importedPath) };
                }).ToArray();
                if (report.imports.Any(item => !item.imported))
                {
                    throw new InvalidOperationException("Sample import output is missing.");
                }
                if (report.phase == "verify")
                {
                    report.assemblies = plan.expectedAssemblies.Select(name =>
                    {
                        var assembly = Assembly.Load(new AssemblyName(name));
                        return new LoadedAssembly { name = assembly.GetName().Name, location = assembly.Location, references = assembly.GetReferencedAssemblies().Select(item => item.Name).ToArray() };
                    }).ToArray();
                }
                report.unityVersion = Application.unityVersion;
                report.success = true;
                exitCode = 0;
            }
            catch (Exception error)
            {
                report.error = error.ToString();
                Debug.LogException(error);
            }
            finally
            {
                string resultPath = Environment.GetEnvironmentVariable("TPLAB_PACKAGE_RESULT");
                try
                {
                    using (var stream = new FileStream(resultPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(JsonUtility.ToJson(report, true));
                    }
                }
                finally
                {
                    if (locked)
                    {
                        EditorApplication.UnlockReloadAssemblies();
                    }
                    EditorApplication.Exit(exitCode);
                }
            }
        }

        [Serializable]
        private sealed class Plan
        {
            public string runId;
            public string sourceRevision;
            public string version;
            public string projectPath;
            public string[] expectedAssemblies;
            public SelectedSample[] samples;
        }

        [Serializable]
        private sealed class SelectedSample
        {
            public string package;
            public string displayName;
        }

        [Serializable]
        private sealed class InstalledPackage
        {
            public string name;
            public string version;
            public string resolvedPath;
            public string source;
        }

        [Serializable]
        private sealed class LoadedAssembly
        {
            public string name;
            public string location;
            public string[] references;
        }

        [Serializable]
        private sealed class ImportedSample
        {
            public string package;
            public string importPath;
            public bool imported;
        }

        [Serializable]
        private sealed class Report
        {
            public bool success;
            public string error;
            public string runId;
            public string sourceRevision;
            public string projectPath;
            public string phase;
            public string unityVersion;
            public InstalledPackage[] installedPackages;
            public LoadedAssembly[] assemblies;
            public ImportedSample[] imports;
        }
    }
}
