using System;
using System.IO;
using TPLab.Core.Input;
using TPLab.Core.Lifecycle;
using TPLab.Core.ResourceManagement;
using TPLab.Core.SceneManagement;
using TPLab.UI;
using TPLab.UI.InputSystem;
using TPLab.UI.Installation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TPLab.Samples.UI.Editor
{
    /// <summary>Creates only new named sample assets/scenes; never installs Build Settings or saves unrelated assets.</summary>
    public static class UIContextSampleBuilder
    {
        private const string Root = "Assets/TPLab/Samples/UI";
        private const string Prefabs = Root + "/Prefabs";
        private const string Settings = Root + "/Settings";
        private const string Scenes = Root + "/Scenes";
        private const string OwnedInput = Settings + "/UIInput.inputactions";
        private const string Hub = Scenes + "/UIContextHub.unity";
        private const string Main = Scenes + "/UIContextMain.unity";
        private const string Area = Scenes + "/UIContextArea.unity";
        private const string Nested = Scenes + "/UIContextNested.unity";
        private static readonly string[] Commands =
        {
            "hud-a", "hud-b", "abc", "close-b", "child", "close-child", "close-parent", "front-a", "mode-b",
            "veto", "user-c", "outside-c", "force-c", "1000", "10000", "0", "last", "resize", "refresh",
            "replace", "loading", "manual", "area", "nested", "remove-nested", "remove-area"
        };

        /// <summary>One-shot authoring. Existing target files/meta, linked folders, Play/compile/import state reject before mutation.</summary>
        [MenuItem("TPLab/UI/Create Sample")]
        public static void CreateOnlyNamedOwnedAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Author only while Editor is idle.");
            RequireFresh(OwnedInput);
            string[] names = { "HudA", "HudB", "PopupA", "PopupB", "PopupC", "Inventory", "Cell", "SceneHud" };
            foreach (string name in names) RequireFresh(Prefabs + "/" + name + ".prefab");
            foreach (string name in new[] { "CommonUI", "SceneUI", "TransitionsSingle", "TransitionsAdditive" })
                RequireFresh(Settings + "/" + name + ".asset");
            foreach (string path in new[] { Hub, Main, Area, Nested, Scenes + "/UIContextBootstrapSingle.unity", Scenes + "/UIContextBootstrapAdditive.unity" })
                RequireFresh(path);
            Folder(Root); Folder(Prefabs); Folder(Settings); Folder(Scenes);
            var source = CreateInputAsset();
            GameObject hudA = null, hudB = null, a = null, b = null, c = null, inventory = null, cell = null, sceneHud = null;
            InNewScene(null, scene =>
            {
                hudA = SavePrefab(Hud("HUD A", new Color(.1f, .25f, .35f), true), "HudA");
                hudB = SavePrefab(Hud("HUD B", new Color(.3f, .17f, .35f), true), "HudB");
                a = SavePrefab(Popup("A modeless", 0), "PopupA");
                b = SavePrefab(Popup("B modal", .08f), "PopupB");
                c = SavePrefab(Popup("C modeless", .16f), "PopupC");
                inventory = SavePrefab(Inventory(), "Inventory");
                cell = SavePrefab(Cell(), "Cell");
                sceneHud = SavePrefab(Hud("Scene-owned HUD", new Color(.07f, .12f, .15f), false), "SceneHud");
            });
            var common = ScriptableObject.CreateInstance<UIContextSettings>();
            common.Configure(new[]
            {
                new UIContextDefinitionData("hud-a", hudA, hostId: "sample", role: UIRole.Hud, retention: UIRetention.Reuse),
                new UIContextDefinitionData("hud-b", hudB, hostId: "sample", role: UIRole.Hud, retention: UIRetention.Reuse),
                new UIContextDefinitionData("a", a, hostId: "sample", retention: UIRetention.Reuse),
                new UIContextDefinitionData("b", b, hostId: "sample", inputMode: UIInputMode.Modal, retention: UIRetention.Reuse),
                new UIContextDefinitionData("c", c, hostId: "sample", retention: UIRetention.DestroyOnClose),
                new UIContextDefinitionData("inventory", inventory, hostId: "sample", retention: UIRetention.Reuse),
                new UIContextDefinitionData("transition-shield", b, hostId: "sample", inputMode: UIInputMode.Modal, retention: UIRetention.Reuse)
            }, "hud-a", new[] { "a", "b", "c", "inventory" });
            SaveAsset(common, Settings + "/CommonUI.asset");
            var sceneSettings = ScriptableObject.CreateInstance<UIContextSettings>();
            sceneSettings.Configure(new[] { new UIContextDefinitionData("scene-hud", sceneHud, hostId: "sample", role: UIRole.Hud) }, "scene-hud");
            SaveAsset(sceneSettings, Settings + "/SceneUI.asset");
            CreateGameScene(Hub, sceneSettings); CreateGameScene(Main, sceneSettings);
            CreateGameScene(Area, sceneSettings); CreateGameScene(Nested, sceneSettings);
            foreach (bool single in new[] { false, true })
            {
                var transitions = ScriptableObject.CreateInstance<SceneTransitionSettings>();
                var mode = single ? LoadSceneMode.Single : LoadSceneMode.Additive;
                transitions.Configure(
                    new SceneTransitionDefinition("entry", SceneTransitionKind.FirstEntry, null, SceneTarget.BuildScene(Hub), mode),
                    new SceneTransitionDefinition("to-main", SceneTransitionKind.ReplacePrimary, Hub, SceneTarget.BuildScene(Main), mode),
                    new SceneTransitionDefinition("to-hub", SceneTransitionKind.ReplacePrimary, Main, SceneTarget.BuildScene(Hub), mode),
                    new SceneTransitionDefinition("area-hub", SceneTransitionKind.AddDerived, Hub, SceneTarget.BuildScene(Area)),
                    new SceneTransitionDefinition("area-main", SceneTransitionKind.AddDerived, Main, SceneTarget.BuildScene(Area)),
                    new SceneTransitionDefinition("nested", SceneTransitionKind.AddDerived, Area, SceneTarget.BuildScene(Nested)),
                    new SceneTransitionDefinition("remove-nested", SceneTransitionKind.RemoveDerived, Area, SceneTarget.BuildScene(Nested)),
                    new SceneTransitionDefinition("remove-area-hub", SceneTransitionKind.RemoveDerived, Hub, SceneTarget.BuildScene(Area)),
                    new SceneTransitionDefinition("remove-area-main", SceneTransitionKind.RemoveDerived, Main, SceneTarget.BuildScene(Area)));
                SaveAsset(transitions, Settings + "/Transitions" + (single ? "Single" : "Additive") + ".asset");
                CreateBootstrap(single, common, transitions, source, (RectTransform)cell.transform);
            }
            Debug.Log("Created UI Context sample-owned assets. Install the listed scenes manually before Play; Build Settings are unchanged.");
        }

        private static InputActionAsset CreateInputAsset()
        {
            var defaults = new DefaultInputActions();
            try
            {
                string absolute = Path.Combine(Directory.GetParent(Application.dataPath).FullName, OwnedInput);
                File.WriteAllText(absolute, defaults.asset.ToJson(), new System.Text.UTF8Encoding(false));
                AssetDatabase.ImportAsset(OwnedInput, ImportAssetOptions.ForceSynchronousImport);
                var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>(OwnedInput);
                if (source == null)
                {
                    throw new IOException("Could not import owned UI input asset: " + OwnedInput);
                }
                source.FindActionMap("Player", true).FindAction("Fire", true);
                source.FindActionMap("UI", true);
                return source;
            }
            finally
            {
                // DefaultInputActions.Dispose uses delayed Destroy, which is invalid during Editor authoring.
                if (defaults.asset != null)
                {
                    UnityEngine.Object.DestroyImmediate(defaults.asset);
                }
            }
        }

        private static void CreateGameScene(string path, UIContextSettings settings)
        {
            InNewScene(path, scene =>
            {
                var owner = new GameObject("Scene UI owner"); owner.SetActive(false);
                Canvas canvas = MakeCanvas(owner.transform, "Scene Canvas", 0);
                var installer = owner.AddComponent<UIContextInstaller>();
                installer.Configure(settings, new[] { new UIContextInstaller.HostBinding("sample", canvas.transform) });
                var root = owner.AddComponent<SceneOwnedRoot>();
                root.Configure(new SceneRootInstaller[] { installer });
                owner.SetActive(true);
            });
        }

        private static void CreateBootstrap(bool single, UIContextSettings settings, SceneTransitionSettings transitions,
            InputActionAsset source, RectTransform cell)
        {
            string path = Scenes + "/UIContextBootstrap" + (single ? "Single" : "Additive") + ".unity";
            InNewScene(path, scene =>
            {
                var owner = new GameObject("Common UI/Input owner"); owner.SetActive(false);
                var input = owner.AddComponent<InputManagerInstaller>(); input.Configure(source);
                var scope = owner.AddComponent<UIContextSampleScopeInstaller>();
                var ui = owner.AddComponent<UIContextInstaller>();
                Canvas canvas = MakeCanvas(owner.transform, "Managed UI Canvas", 100);
                var eventObject = new GameObject("Explicit EventSystem"); eventObject.SetActive(false);
                eventObject.transform.SetParent(owner.transform, false);
                var system = eventObject.AddComponent<EventSystem>();
                var module = eventObject.AddComponent<InputSystemUIInputModule>(); module.enabled = false;
                var adapter = eventObject.AddComponent<UIInputSystemAdapter>(); eventObject.SetActive(true);
                Canvas harness = MakeCanvas(owner.transform, "Project command harness", 300);
                Text status = Label(harness.transform, "Status", "Bootstrap pending", new Vector2(.02f, .78f), new Vector2(.98f, .99f), 16);
                status.alignment = TextAnchor.UpperLeft;
                var buttons = new Button[Commands.Length];
                for (int i = 0; i < Commands.Length; ++i)
                {
                    int row = i / 9, column = i % 9;
                    Vector2 min = new Vector2(.01f + column * .11f, .02f + row * .055f);
                    buttons[i] = Button(harness.transform, Commands[i], min, min + new Vector2(.105f, .05f));
                }
                Canvas loadingCanvas = MakeCanvas(owner.transform, "Loading presentation", 400);
                var loading = loadingCanvas.gameObject.AddComponent<CanvasGroup>();
                Graphic(loadingCanvas.transform, "Loading backdrop", new Color(.04f, .08f, .12f, 1), Vector2.zero, Vector2.one);
                Text loadingStatus = Label(loadingCanvas.transform, "Loading status", "Preparing", new Vector2(.15f, .5f), new Vector2(.85f, .7f), 26);
                Button proceed = Button(loadingCanvas.transform, "Continue", new Vector2(.35f, .3f), new Vector2(.65f, .42f));
                Canvas coverCanvas = MakeCanvas(owner.transform, "Cover", 500);
                var cover = coverCanvas.gameObject.AddComponent<CanvasGroup>();
                Graphic(coverCanvas.transform, "Cover blocker", new Color(.02f, .035f, .05f, 1), Vector2.zero, Vector2.one);
                Label(coverCanvas.transform, "Cover status", "Covered: preparing owned scenes", new Vector2(.08f, .3f), new Vector2(.92f, .7f), 24);
                var bootstrap = owner.AddComponent<BootstrapSystem>();
                var controller = owner.AddComponent<UIContextSampleController>();
                controller.ConfigureSample(bootstrap, ui, input, settings, source, system, module, adapter,
                    canvas.transform, cell, status, cover, loading, loadingStatus, proceed, buttons, Commands);
                scope.Configure(controller);
                ui.Configure(settings, new[] { new UIContextInstaller.HostBinding("sample", canvas.transform) },
                    eventSystem: system);
                MonoBehaviour host;
                SceneRootInstaller[] ordered = { input, scope, ui };
                if (single)
                {
                    var root = owner.AddComponent<SingletonSceneRoot>(); root.Configure(ordered, true); host = root;
                }
                else
                {
                    var root = owner.AddComponent<SceneOwnedRoot>(); root.Configure(ordered, false); host = root;
                }
                bootstrap.Configure(host, transitions, "entry", false, controller);
                // Visibility is serialized without deactivating borrowed canvases/hosts.
                loading.alpha = 0; loading.blocksRaycasts = false; loading.interactable = false;
                proceed.interactable = false;
                owner.SetActive(true);
            });
        }

        private static GameObject Hud(string title, Color color, bool interactive)
        {
            var root = Rect(title, null, Vector2.zero, Vector2.one).gameObject;
            float bottom = interactive ? .72f : .2f;
            Graphic(root.transform, "HUD strip", color, new Vector2(0, bottom), new Vector2(1, bottom + .06f)).raycastTarget = false;
            Label(root.transform, "HUD title", title, new Vector2(.04f, bottom), new Vector2(.65f, bottom + .06f), 22);
            if (interactive) Button(root.transform, "User close HUD", new Vector2(.78f, .72f), new Vector2(.98f, .78f));
            return root;
        }

        private static GameObject Popup(string title, float shift)
        {
            var root = Rect(title, null, new Vector2(.08f + shift, .32f), new Vector2(.4f + shift, .68f)).gameObject;
            var image = root.AddComponent<Image>(); image.color = new Color(.1f + shift, .22f, .3f, .98f);
            Label(root.transform, "Title", title, new Vector2(.05f, .6f), new Vector2(.95f, .95f), 22);
            Button(root.transform, "User close", new Vector2(.1f, .15f), new Vector2(.9f, .4f));
            return root;
        }

        private static GameObject Inventory()
        {
            var root = Rect("Inventory (separate inherited Canvas)", null, new Vector2(.68f, .22f), new Vector2(.99f, .7f)).gameObject;
            // Dedicated batching Canvas inherits the managed host/order and wrapper input mask.
            root.AddComponent<Canvas>().overrideSorting = false;
            root.AddComponent<GraphicRaycaster>();
            root.AddComponent<VirtualScrollRect>();
            var background = root.AddComponent<Image>(); background.color = new Color(.08f, .13f, .18f);
            Label(root.transform, "Title", "Inventory: 1k / 10k / 0 / resize", new Vector2(.02f, .87f), new Vector2(.98f, 1), 17);
            var scrollHost = Rect("ScrollRect", root.transform, Vector2.zero, Vector2.one);
            var scroll = scrollHost.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.inertia = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Rect("Viewport", scrollHost, new Vector2(.5f, 1), new Vector2(.5f, 1));
            viewport.pivot = new Vector2(.5f, 1); viewport.sizeDelta = new Vector2(360, 240); viewport.anchoredPosition = new Vector2(0, -45);
            viewport.gameObject.AddComponent<RectMask2D>();
            var image = viewport.gameObject.AddComponent<Image>(); image.color = new Color(.07f, .1f, .14f); image.raycastTarget = true;
            var content = Rect("Content", viewport, new Vector2(0, 1), new Vector2(1, 1));
            content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero; content.anchoredPosition = Vector2.zero;
            scroll.viewport = viewport; scroll.content = content;
            return root;
        }

        private static GameObject Cell()
        {
            var rect = Rect("Cell", null, new Vector2(0, 1), new Vector2(1, 1));
            rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(0, 24); rect.anchoredPosition = Vector2.zero;
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.12f, .2f, .26f); image.raycastTarget = false;
            var label = Label(rect, "Text", "Prepared item", Vector2.zero, Vector2.one, 18);
            label.alignment = TextAnchor.MiddleLeft;
            return rect.gameObject;
        }

        private static Canvas MakeCanvas(Transform parent, string name, int order)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.one);
            var canvas = rect.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var scaler = rect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
            rect.gameObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var host = new GameObject(name, typeof(RectTransform)); host.transform.SetParent(parent, false);
            var rect = (RectTransform)host.transform;
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
            return rect;
        }

        private static Image Graphic(Transform parent, string name, Color color, Vector2 min, Vector2 max)
        {
            var rect = Rect(name, parent, min, max);
            var image = rect.gameObject.AddComponent<Image>(); image.color = color;
            return image;
        }

        private static Text Label(Transform parent, string name, string text, Vector2 min, Vector2 max, int fontSize)
        {
            var rect = Rect(name, parent, min, max);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = fontSize;
            label.color = Color.white; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false; label.text = text;
            return label;
        }

        private static Button Button(Transform parent, string text, Vector2 min, Vector2 max)
        {
            Image image = Graphic(parent, text, new Color(.2f, .36f, .46f), min, max);
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            Label(image.transform, "Label", text, Vector2.zero, Vector2.one, 16);
            return button;
        }

        private static GameObject SavePrefab(GameObject source, string name)
        {
            source.SetActive(false);
            try
            {
                string path = Prefabs + "/" + name + ".prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(source, path, out bool success);
                if (!success || prefab == null) throw new IOException("Could not save new owned prefab: " + path);
                AssetDatabase.SaveAssetIfDirty(prefab);
                return prefab;
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }

        private static void SaveAsset(UnityEngine.Object asset, string path)
        {
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        private static void InNewScene(string path, Action<Scene> build)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                build(scene);
                if (path != null && !EditorSceneManager.SaveScene(scene, path)) throw new IOException("Could not save owned scene: " + path);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static void RequireFresh(string path)
        {
            if (!path.StartsWith(Root + "/", StringComparison.Ordinal)) throw new InvalidOperationException("Path outside owned sample.");
            string absolute = Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);
            for (var directory = new DirectoryInfo(Path.GetDirectoryName(absolute)); directory != null; directory = directory.Parent)
                if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Sample authoring cannot traverse a linked directory: " + directory.FullName);
            if (File.Exists(absolute) || Directory.Exists(absolute) || File.Exists(absolute + ".meta") || AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new IOException("Refusing to overwrite owned target: " + path);
        }

        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            if (!parent.StartsWith("Assets/TPLab/Samples", StringComparison.Ordinal)) throw new IOException("Only new sample child folders are allowed.");
            if (!AssetDatabase.IsValidFolder(parent)) Folder(parent);
            string guid = AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
            if (string.IsNullOrEmpty(guid)) throw new IOException("Could not create owned folder: " + path);
        }
    }
}