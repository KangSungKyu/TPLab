using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TPLab.Core.Lifecycle;
using TPLab.Core.ResourceManagement;
using TPLab.UI;
using TPLab.UI.Installation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TPLab.UI.Tests.Installation
{
    public sealed class UIContextInstallerPlayModeTests
    {
        private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = _owned.Count - 1; i >= 0; --i)
            {
                if (_owned[i] != null) UnityEngine.Object.Destroy(_owned[i]);
            }
            _owned.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator InstallDoesNotClone_PrepareSelectsExactlyTheConfiguredHud()
            => InstallDoesNotCloneThenPreparesHudAsync().ToCoroutine();

        [UnityTest]
        public IEnumerator SourcePreloadDoesNotCreateDisplay_ExplicitOpenUsesPreparedSource()
            => SourcePreloadDoesNotCreateDisplayAsync().ToCoroutine();

        [UnityTest]
        public IEnumerator FirstHudFailureStillReleasesPartialContextAndPreservesBorrowedPrefab()
            => FailedPreparationUnwindsAsync().ToCoroutine();

        [UnityTest]
        public IEnumerator SceneOwnedShutdownLeavesPersistentRootContextAlive()
            => IndependentRootsAsync().ToCoroutine();

        private async UniTask InstallDoesNotCloneThenPreparesHudAsync()
        {
            GameObject prefab = Own(new GameObject("HUD-source"));
            var settings = CreateSettings(new UIContextDefinitionData("hud", prefab, role: UIRole.Hud), "hud");
            var installer = CreateInstaller(settings);
            SceneOwnedRoot root = CreateRoot(installer);

            Assert.That(installer.Context, Is.Not.Null);
            Assert.That(installer.Context.Displays, Is.Empty);
            Assert.That(root.IsReady, Is.True);
            await root.PrepareAsync();
            Assert.That(root.IsPrepared, Is.True);
            Assert.That(installer.Context.CurrentHud, Is.Not.Null);
            Assert.That(installer.Context.CurrentHud.State, Is.EqualTo(UIState.Visible));
            Assert.That(installer.Context.Displays.Count, Is.EqualTo(1));
            Assert.That(prefab == null, Is.False, "UI context borrows the source prefab.");
            await root.ShutdownAsync();
            Assert.That(installer.Context, Is.Null);
            Assert.That(prefab == null, Is.False);
        }

        private async UniTask SourcePreloadDoesNotCreateDisplayAsync()
        {
            GameObject prefab = Own(new GameObject("Popup-source"));
            int providerCalls = 0;
            var settings = CreateSettings(new UIContextDefinitionData("popup", assetKey: "popup-key"), null, "popup");
            var installer = CreateInstaller(settings, loadPrefab: (key, token) =>
            {
                Assert.That(key, Is.EqualTo("popup-key"));
                token.ThrowIfCancellationRequested();
                ++providerCalls;
                return UniTask.FromResult(prefab);
            });
            SceneOwnedRoot root = CreateRoot(installer);
            await root.PrepareAsync();

            Assert.That(providerCalls, Is.EqualTo(1));
            Assert.That(installer.Context.Displays, Is.Empty);
            Assert.That(installer.Context.CurrentHud, Is.Null);
            UIHandle popup = await installer.Context.OpenAsync(new UIOpenRequest("popup"));
            Assert.That(popup.State, Is.EqualTo(UIState.Visible));
            Assert.That(installer.Context.Displays.Count, Is.EqualTo(1));
            await root.ShutdownAsync();
            Assert.That(prefab == null, Is.False);
        }

        private async UniTask FailedPreparationUnwindsAsync()
        {
            GameObject prefab = Own(new GameObject("HUD-source"));
            bool closed = false;
            var hooks = new UIHooks
            {
                PrepareAsync = async (_, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    await UniTask.Yield();
                    throw new InvalidOperationException("expected first HUD failure");
                },
                Closed = _ => closed = true
            };
            var settings = CreateSettings(new UIContextDefinitionData("hud", prefab, role: UIRole.Hud), "hud");
            var installer = CreateInstaller(settings, firstHudHooks: hooks);
            SceneOwnedRoot root = CreateRoot(installer);

            bool preparationFailed = false;
            try
            {
                await root.PrepareAsync();
            }
            catch (InvalidOperationException)
            {
                preparationFailed = true;
            }
            Assert.That(preparationFailed, Is.True, "The first HUD preparation failure must be observable by the root caller.");
            await root.ShutdownAsync();
            Assert.That(closed, Is.True);
            Assert.That(installer.Context, Is.Null);
            Assert.That(prefab == null, Is.False);
        }

        private async UniTask IndependentRootsAsync()
        {
            GameObject prefabA = Own(new GameObject("Persistent-HUD-source"));
            GameObject prefabB = Own(new GameObject("Scene-HUD-source"));
            SceneOwnedRoot persistent = CreateRoot(CreateInstaller(
                CreateSettings(new UIContextDefinitionData("hud", prefabA, role: UIRole.Hud), "hud")), true);
            SceneOwnedRoot sceneOwned = CreateRoot(CreateInstaller(
                CreateSettings(new UIContextDefinitionData("hud", prefabB, role: UIRole.Hud), "hud")));
            var persistentUi = persistent.GetComponentInChildren<UIContextInstaller>(true);
            var sceneUi = sceneOwned.GetComponentInChildren<UIContextInstaller>(true);
            await persistent.PrepareAsync();
            await sceneOwned.PrepareAsync();
            UIHandle persistentHud = persistentUi.Context.CurrentHud;
            Assert.That(persistent.PersistsAcrossScenes, Is.True);
            Assert.That(sceneUi.Context.CurrentHud, Is.Not.Null);

            await sceneOwned.ShutdownAsync();
            Assert.That(sceneUi.Context, Is.Null);
            UnityEngine.Object.Destroy(sceneOwned.gameObject);
            await UniTask.Yield();
            Assert.That(persistentUi.Context.CurrentHud, Is.SameAs(persistentHud));
            Assert.That(persistentHud.State, Is.EqualTo(UIState.Visible));
            await persistent.ShutdownAsync();
        }

        private UIContextSettings CreateSettings(UIContextDefinitionData definition, string firstHud,
            params string[] preloads)
        {
            var settings = Own(ScriptableObject.CreateInstance<UIContextSettings>());
            settings.Configure(new[] { definition }, firstHud, preloads);
            return settings;
        }

        private UIContextInstaller CreateInstaller(UIContextSettings settings,
            Func<string, CancellationToken, UniTask<GameObject>> loadPrefab = null,
            UIHooks firstHudHooks = null)
        {
            var owner = new GameObject("InstallerHost");
            owner.SetActive(false);
            var installer = owner.AddComponent<UIContextInstaller>();
            _owned.Add(owner);
            installer.Configure(settings, loadPrefab: loadPrefab, firstHudHooks: firstHudHooks);
            return installer;
        }

        private SceneOwnedRoot CreateRoot(UIContextInstaller installer, bool persist = false)
        {
            var host = new GameObject("Root");
            host.SetActive(false);
            var root = host.AddComponent<SceneOwnedRoot>();
            installer.transform.SetParent(host.transform, false);
            _owned.Add(host);
            root.Configure(new SceneRootInstaller[] { installer }, persist);
            host.SetActive(true);
            return root;
        }

        private static GameObject Child(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private T Own<T>(T value) where T : UnityEngine.Object
        {
            _owned.Add(value);
            return value;
        }
    }
}
