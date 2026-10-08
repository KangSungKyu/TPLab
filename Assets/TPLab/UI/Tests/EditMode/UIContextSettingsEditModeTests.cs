using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TPLab.Core.ResourceManagement;
using NUnit.Framework;
using TPLab.UI;
using TPLab.UI.Installation;
using UnityEditor;
using UnityEngine;

namespace TPLab.UI.Tests.Installation
{
    public sealed class UIContextSettingsEditModeTests
    {
        [Test]
        public void ConfigureAndInspectorValuesCreateEquivalentDetachedSnapshots()
        {
            var settings = ScriptableObject.CreateInstance<UIContextSettings>();
            var prefab = new GameObject("borrowed-prefab");
            try
            {
                var source = new[]
                {
                    new UIContextDefinitionData("hud", prefab, role: UIRole.Hud, retention: UIRetention.Reuse)
                };
                var preloads = new[] { "hud" };
                settings.Configure(source, "hud", preloads);
                source[0] = new UIContextDefinitionData("mutated", prefab);
                preloads[0] = "mutated";
                var configured = settings.CreateSnapshot();

                var serialized = new SerializedObject(settings);
                SerializedProperty definitions = serialized.FindProperty("_definitions");
                definitions.arraySize = 1;
                SerializedProperty row = definitions.GetArrayElementAtIndex(0);
                row.FindPropertyRelative("_id").stringValue = "hud";
                row.FindPropertyRelative("_prefab").objectReferenceValue = prefab;
                row.FindPropertyRelative("_role").enumValueIndex = (int)UIRole.Hud;
                row.FindPropertyRelative("_hostId").stringValue = "default";
                row.FindPropertyRelative("_inputMode").enumValueIndex = (int)UIInputMode.Modeless;
                row.FindPropertyRelative("_retention").enumValueIndex = (int)UIRetention.Reuse;
                row.FindPropertyRelative("_hideStrategy").enumValueIndex = (int)UIHideStrategy.DeactivateView;
                serialized.FindProperty("_firstHudDefinitionId").stringValue = "hud";
                serialized.FindProperty("_preloadDefinitionIds").arraySize = 1;
                serialized.FindProperty("_preloadDefinitionIds").GetArrayElementAtIndex(0).stringValue = "hud";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var inspected = settings.CreateSnapshot();

                Assert.That(configured.Count, Is.EqualTo(1));
                Assert.That(configured[0].Id, Is.EqualTo("hud"));
                Assert.That(configured[0].Prefab, Is.SameAs(prefab));
                Assert.That(configured[0].Retention, Is.EqualTo(UIRetention.Reuse));
                Assert.That(inspected.Count, Is.EqualTo(configured.Count));
                Assert.That(inspected[0].Id, Is.EqualTo(configured[0].Id));
                Assert.That(settings.FirstHudDefinitionId, Is.EqualTo("hud"));
                Assert.That(settings.PreloadDefinitionIds, Is.EqualTo(new[] { "hud" }));
                Assert.That(settings.CreateSnapshot(), Is.Not.SameAs(inspected));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void InvalidDefinitionAndHudMetadataRejectsWithoutSceneObjects()
        {
            var settings = ScriptableObject.CreateInstance<UIContextSettings>();
            var prefab = new GameObject("borrowed-prefab");
            try
            {
                Assert.Throws<ArgumentException>(() => settings.Configure(new[]
                {
                    new UIContextDefinitionData("same", prefab),
                    new UIContextDefinitionData("same", prefab)
                }));
                Assert.Throws<ArgumentException>(() => settings.Configure(new[]
                {
                    new UIContextDefinitionData("no-source")
                }));
                Assert.Throws<ArgumentException>(() => settings.Configure(new[]
                {
                    new UIContextDefinitionData("two-sources", prefab, "key")
                }));
                Assert.Throws<ArgumentException>(() => settings.Configure(new[]
                {
                    new UIContextDefinitionData("undefined-role", prefab, role: (UIRole)999)
                }));
                settings.Configure(new[] { new UIContextDefinitionData("inspect-valid", prefab) });
                var invalidInspector = new SerializedObject(settings);
                invalidInspector.FindProperty("_definitions").GetArrayElementAtIndex(0)
                    .FindPropertyRelative("_role").intValue = 999;
                invalidInspector.ApplyModifiedPropertiesWithoutUndo();
                Assert.Catch<ArgumentException>(() => settings.CreateSnapshot());
                Assert.Throws<ArgumentException>(() => settings.Configure(new[]
                {
                    new UIContextDefinitionData("wrong-hud-role", prefab, role: UIRole.Popup)
                }, firstHudDefinitionId: "wrong-hud-role"));
                Assert.Throws<ArgumentException>(() => settings.Configure(new[]
                {
                    new UIContextDefinitionData("known-hud", prefab, role: UIRole.Hud)
                }, preloadDefinitionIds: new[] { "unknown-preload" }));
                Assert.That(prefab == null, Is.False, "Validation must leave the borrowed source alive.");

                var installerOwner = new GameObject("installer-config-owner");
                var installer = installerOwner.AddComponent<UIContextInstaller>();
                var unknownHost = ScriptableObject.CreateInstance<UIContextSettings>();
                var providerSettings = ScriptableObject.CreateInstance<UIContextSettings>();
                var resourceOwner = new GameObject("borrowed-resources");
                var resources = resourceOwner.AddComponent<ResourceManagerInstaller>();
                try
                {
                    unknownHost.Configure(new[]
                    {
                        new UIContextDefinitionData("popup", prefab, hostId: "not-registered")
                    });
                    Assert.Catch<ArgumentException>(() => installer.Configure(unknownHost));

                    providerSettings.Configure(new[]
                    {
                        new UIContextDefinitionData("keyed-popup", assetKey: "ui/popup")
                    });
                    Func<string, CancellationToken, UniTask<GameObject>> provider =
                        (_, __) => UniTask.FromResult(prefab);
                    Assert.Catch<ArgumentException>(() => installer.Configure(
                        providerSettings, resources: resources, loadPrefab: provider));
                    Assert.That(prefab == null, Is.False, "Invalid installer configuration must preserve borrowed sources.");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(resourceOwner);
                    UnityEngine.Object.DestroyImmediate(providerSettings);
                    UnityEngine.Object.DestroyImmediate(unknownHost);
                    UnityEngine.Object.DestroyImmediate(installerOwner);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }
    }
}
