using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Editor.Configuration;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TPLab.Samples.Netcode.Editor
{
    /// <summary>Creates only fresh, sample-owned fixed-scene assets. Does not change open scenes or Build Settings.</summary>
    public static class NetcodeSessionSampleBuilder
    {
        private const string Root = "Assets/TPLab/Samples/Netcode";
        private const string ScenePath = Root + "/Scenes/NetcodeSessionSample.unity";
        private const string InputPath = Root + "/Settings/NetcodeSessionSample.inputactions";
        private static readonly string[] Commands = { "start", "ready", "fail", "stop", "retry", "quit", "interact" };

        /// <summary>Gets the fixed sample scene path without creating assets or opening scenes.</summary>
        public static string GetScenePath() => ScenePath;

        /// <summary>Authors fresh scene/input assets in the idle Editor. Existing target files or meta files reject before mutation.</summary>
        [MenuItem("TPLab/Netcode/Create Session Sample")]
        public static void CreateSessionSample()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
                || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Requires the idle compiled Editor.");
            RequireFresh(ScenePath);
            RequireFresh(InputPath);
            RequireFolderTarget(Root + "/Scenes");
            RequireFolderTarget(Root + "/Settings");
            var files = new List<string>();
            var folders = new List<string>();
            var settings = NetcodeForGameObjectsProjectSettings.instance;
            bool generatePrefabs = settings.GenerateDefaultNetworkPrefabs;
            try
            {
                // NGO's Editor Reset/import hooks must not create or update a project-wide prefab asset.
                settings.GenerateDefaultNetworkPrefabs = false;
                Folder(Root + "/Scenes", folders);
                Folder(Root + "/Settings", folders);
                InputActionAsset input = CreateInput(files);
                CreateScene(input, files);
                Debug.Log("Created the Netcode fixed-scene sample. Add its scene to Build Settings before Play/build; existing Build Settings are unchanged.");
            }
            catch (Exception original)
            {
                var errors = new List<Exception> { original };
                foreach (string path in files.AsEnumerable().Reverse())
                {
                    try
                    {
                        if (!AssetDatabase.DeleteAsset(path))
                        {
                            if (File.Exists(Absolute(path))) File.Delete(Absolute(path));
                            if (File.Exists(Absolute(path) + ".meta")) File.Delete(Absolute(path) + ".meta");
                        }
                    }
                    catch (Exception cleanup) { errors.Add(cleanup); }
                }
                foreach (string path in folders.AsEnumerable().Reverse())
                {
                    try
                    {
                        if (Directory.Exists(Absolute(path)) && !Directory.EnumerateFileSystemEntries(Absolute(path)).Any())
                            if (!AssetDatabase.DeleteAsset(path)) throw new IOException("Could not remove newly created empty folder: " + path);
                    }
                    catch (Exception cleanup) { errors.Add(cleanup); }
                }
                if (errors.Count > 1) throw new AggregateException("Sample authoring and owned cleanup failed.", errors);
                throw;
            }
            finally { settings.GenerateDefaultNetworkPrefabs = generatePrefabs; }
        }

        private static InputActionAsset CreateInput(List<string> files)
        {
            RequireFresh(InputPath);
            var defaults = new DefaultInputActions();
            try
            {
                using (var stream = new FileStream(Absolute(InputPath), FileMode.CreateNew, FileAccess.Write))
                {
                    files.Add(InputPath);
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                        writer.Write(defaults.asset.ToJson());
                }
                AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceSynchronousImport);
                var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
                if (input == null) throw new IOException("Could not import sample-owned input actions.");
                return input;
            }
            finally { if (defaults.asset != null) Object.DestroyImmediate(defaults.asset); }
        }

        private static void CreateScene(InputActionAsset input, List<string> files)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var managerObject = new GameObject("Sample NetworkManager");
                managerObject.SetActive(false);
                var manager = managerObject.AddComponent<NetworkManager>();
                var transport = managerObject.AddComponent<UnityTransport>();
                transport.SetConnectionData("127.0.0.1", 7777, "127.0.0.1");
                manager.NetworkConfig = new NetworkConfig
                {
                    NetworkTransport = transport, ConnectionApproval = true, EnableSceneManagement = true,
                    AutoSpawnPlayerPrefabClientSide = false, PlayerPrefab = null, ForceSamePrefabs = true,
                    Prefabs = new NetworkPrefabs()
                };
                manager.RunInBackground = true;
                managerObject.SetActive(true);
                var owner = new GameObject("Netcode Session Sample");
                owner.SetActive(false);
                var sample = owner.AddComponent<NetcodeSessionSample>();
                var view = owner.AddComponent<NetcodeSessionSampleView>();
                var camera = new GameObject("Sample background camera").AddComponent<Camera>();
                camera.transform.SetParent(owner.transform, false);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.025f, .04f, .065f);
                camera.cullingMask = 0;
                camera.depth = -100;
                var events = new GameObject("Sample EventSystem");
                events.transform.SetParent(owner.transform, false);
                events.AddComponent<EventSystem>();
                var module = events.AddComponent<InputSystemUIInputModule>();
                module.actionsAsset = input;
                module.point = Action(input, "Point");
                module.leftClick = Action(input, "Click");
                module.rightClick = Action(input, "RightClick");
                module.middleClick = Action(input, "MiddleClick");
                module.scrollWheel = Action(input, "ScrollWheel");
                module.move = Action(input, "Navigate");
                module.submit = Action(input, "Submit");
                module.cancel = Action(input, "Cancel");
                var gameplay = Canvas(owner.transform, "Gameplay Canvas", 0);
                Label(gameplay.transform, "GameplayTitle", "LOCAL GAMEPLAY DEMONSTRATION", .48f, .35f, .97f, .52f, 26);
                var buttons = new Button[Commands.Length];
                buttons[6] = Button(gameplay.transform, "interact", "Interact", .70f, .08f, .96f, .17f);
                var shield = Canvas(owner.transform, "Protection Canvas", 50);
                var cover = shield.gameObject.AddComponent<CanvasGroup>();
                Graphic(shield.transform, "Opaque protection", new Color(.025f, .045f, .075f, 1), 0, 0, 1, 1);
                var coverText = Label(shield.transform, "CoverText", "WAITING FOR THE SESSION\nGameplay stays blocked until everyone is ready.",
                    .49f, .22f, .95f, .43f, 24);
                var controls = Canvas(owner.transform, "Control Canvas", 100);
                Graphic(controls.transform, "Lobby panel", new Color(.055f, .085f, .12f, 1), .02f, .03f, .44f, .90f);
                var badge = Label(controls.transform, "DemoBadge", "LOCAL DEMO ONLY - NOT AUTHENTICATION", .03f, .92f, .97f, .99f, 24);
                badge.color = new Color(1, .8f, .3f);
                Label(controls.transform, "LobbyTitle", "SESSION LOBBY", .04f, .81f, .42f, .88f, 26);
                var sessionId = Field(controls.transform, "SessionId", "Session", "tplab-demo", .73f);
                var attemptId = Field(controls.transform, "AttemptId", "Attempt", "round-1", .65f);
                var roster = Field(controls.transform, "Roster", "Roster", "host,c1,c2", .57f);
                var port = Field(controls.transform, "Port", "Port", "7777", .49f);
                port.contentType = InputField.ContentType.IntegerNumber;
                port.characterLimit = 5;
                Label(controls.transform, "RoleLabel", "Role", .04f, .41f, .16f, .47f, 18);
                var role = RoleDropdown(controls.transform);
                var failRestore = RestoreToggle(controls.transform);
                string[] titles = { "Start / Join", "Ready", "Fail Prepare", "Stop", "Retry Restore", "Quit Safely" };
                for (int i = 0; i < 6; i++)
                {
                    float x = .04f + (i % 3) * .128f, y = .22f - (i / 3) * .09f;
                    buttons[i] = Button(controls.transform, Commands[i], titles[i], x, y, x + .118f, y + .065f);
                }
                Label(controls.transform, "Hint", "Use the same session, attempt, roster and port.\nStart Host, then join Client1 and Client2.\nUse a NEW shared attempt after returning to the lobby.",
                    .04f, .035f, .42f, .12f, 15);
                Graphic(controls.transform, "Status panel", new Color(.055f, .085f, .12f, 1), .48f, .46f, .97f, .89f);
                var status = Label(controls.transform, "Status", "Ready to configure the local demonstration.", .50f, .48f, .95f, .87f, 16);
                status.alignment = TextAnchor.UpperLeft;
                view.Configure(sample, manager, status, coverText, cover, sessionId, attemptId, roster, port, role, failRestore, buttons, Commands);
                owner.SetActive(true);
                RequireFresh(ScenePath);
                files.Add(ScenePath);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save the sample scene.");
                AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                try
                {
                    if (scene.IsValid() && scene.isLoaded && !EditorSceneManager.CloseScene(scene, true))
                        throw new IOException("Could not close the temporary sample authoring scene.");
                }
                finally { if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
            }
        }

        private static InputActionReference Action(InputActionAsset input, string name)
        {
            Guid id = input.FindAction("UI/" + name, true).id;
            var action = AssetDatabase.LoadAllAssetsAtPath(InputPath).OfType<InputActionReference>()
                .FirstOrDefault(item => item.action != null && item.action.id == id);
            if (action == null) throw new IOException("Missing imported UI action reference: " + name);
            return action;
        }

        private static InputField Field(Transform parent, string name, string title, string value, float y)
        {
            Label(parent, name + "Label", title, .04f, y, .16f, y + .06f, 18);
            var image = Graphic(parent, name, new Color(.16f, .22f, .29f), .17f, y, .42f, y + .06f);
            var input = image.gameObject.AddComponent<InputField>();
            input.targetGraphic = image;
            input.textComponent = Label(image.transform, "Text", "", .04f, .05f, .96f, .95f, 18);
            input.textComponent.alignment = TextAnchor.MiddleLeft;
            input.characterLimit = 128;
            input.text = value;
            return input;
        }

        private static Toggle RestoreToggle(Transform parent)
        {
            var rect = Rect(parent, "FailRestore", .04f, .32f, .42f, .39f);
            var toggle = rect.gameObject.AddComponent<Toggle>();
            var box = Graphic(rect, "Box", new Color(.18f, .25f, .32f), 0, .1f, .08f, .9f);
            var check = Graphic(box.transform, "Checkmark", new Color(.35f, .9f, .65f), .2f, .2f, .8f, .8f);
            toggle.targetGraphic = box; toggle.graphic = check;
            Label(rect, "Label", "Fail first lobby restore", .11f, 0, 1, 1, 18).alignment = TextAnchor.MiddleLeft;
            toggle.isOn = false;
            return toggle;
        }

        private static Dropdown RoleDropdown(Transform parent)
        {
            var image = Graphic(parent, "Role", new Color(.16f, .22f, .29f), .17f, .41f, .42f, .47f);
            var dropdown = image.gameObject.AddComponent<Dropdown>();
            dropdown.targetGraphic = image;
            dropdown.captionText = Label(image.transform, "Caption", "Host", .04f, .05f, .9f, .95f, 18);
            Label(image.transform, "Arrow", "v", .9f, 0, .99f, 1, 18);
            var template = Rect(image.transform, "Template", 0, 0, 1, 0);
            template.pivot = new Vector2(.5f, 1); template.sizeDelta = new Vector2(0, 108);
            template.gameObject.AddComponent<Image>().color = new Color(.11f, .16f, .22f);
            var viewport = Rect(template, "Viewport", 0, 0, 1, 1);
            viewport.gameObject.AddComponent<Image>(); viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = Rect(viewport, "Content", 0, 1, 1, 1);
            content.pivot = new Vector2(.5f, 1); content.sizeDelta = new Vector2(0, 108);
            var item = Rect(content, "Item", 0, 1, 1, 1);
            item.pivot = new Vector2(.5f, 1); item.sizeDelta = new Vector2(0, 34);
            var itemBackground = item.gameObject.AddComponent<Image>(); itemBackground.color = new Color(.2f, .29f, .38f);
            var toggle = item.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = itemBackground;
            toggle.graphic = Graphic(item, "Checkmark", new Color(.35f, .9f, .65f), .02f, .3f, .09f, .7f);
            dropdown.itemText = Label(item, "ItemLabel", "Host", .13f, 0, .98f, 1, 18);
            var scroll = template.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            dropdown.template = template;
            dropdown.options = new List<Dropdown.OptionData> { new Dropdown.OptionData("Host"), new Dropdown.OptionData("Client1"), new Dropdown.OptionData("Client2") };
            dropdown.value = 0; dropdown.RefreshShownValue(); template.gameObject.SetActive(false);
            return dropdown;
        }

        private static Canvas Canvas(Transform parent, string name, int order)
        {
            var rect = Rect(parent, name, 0, 0, 1, 1);
            var canvas = rect.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var scaler = rect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
            rect.gameObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }
        private static RectTransform Rect(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            return rect;
        }
        private static Image Graphic(Transform parent, string name, Color color, float x0, float y0, float x1, float y1)
        {
            var image = Rect(parent, name, x0, y0, x1, y1).gameObject.AddComponent<Image>(); image.color = color;
            return image;
        }
        private static Text Label(Transform parent, string name, string text, float x0, float y0, float x1, float y1, int size)
        {
            var label = Rect(parent, name, x0, y0, x1, y1).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = size;
            label.text = text; label.color = Color.white; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            return label;
        }
        private static Button Button(Transform parent, string name, string title, float x0, float y0, float x1, float y1)
        {
            var image = Graphic(parent, name, new Color(.18f, .32f, .45f), x0, y0, x1, y1);
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            Label(image.transform, "Label", title, 0, 0, 1, 1, 17);
            return button;
        }
        private static string Absolute(string path) => Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);
        private static void RequireFresh(string path)
        {
            if (!path.StartsWith(Root + "/", StringComparison.Ordinal)) throw new InvalidOperationException("Outside sample ownership.");
            string absolute = Absolute(path);
            for (var directory = new DirectoryInfo(Path.GetDirectoryName(absolute)); directory != null; directory = directory.Parent)
                if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Cannot author through linked directories: " + directory.FullName);
            if (File.Exists(absolute) || Directory.Exists(absolute) || File.Exists(absolute + ".meta")
                || Directory.Exists(absolute + ".meta") || AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new IOException("Refusing to overwrite existing target or meta: " + path);
        }
        private static void RequireFolderTarget(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            if (File.Exists(Absolute(path)) || Directory.Exists(Absolute(path)) || File.Exists(Absolute(path) + ".meta")
                || Directory.Exists(Absolute(path) + ".meta"))
                throw new IOException("Unrecognized folder or meta collision: " + path);
            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            if (parent != "Assets/TPLab/Samples" && !parent.StartsWith(Root + "/", StringComparison.Ordinal) && parent != Root)
                throw new IOException("Cannot create a parent outside the sample.");
            RequireFolderTarget(parent);
        }
        private static void Folder(string path, List<string> folders)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            if (File.Exists(Absolute(path)) || Directory.Exists(Absolute(path)) || File.Exists(Absolute(path) + ".meta"))
                throw new IOException("Unrecognized folder or meta collision: " + path);
            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            if (parent != "Assets/TPLab/Samples" && !parent.StartsWith(Root, StringComparison.Ordinal))
                throw new IOException("Cannot create a parent outside the sample.");
            Folder(parent, folders);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, path.Substring(split + 1)))) throw new IOException("Could not create sample folder.");
            folders.Add(path);
        }
    }
}
