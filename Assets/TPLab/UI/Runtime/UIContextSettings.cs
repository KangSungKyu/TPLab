using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPLab.UI.Installation
{
    /// <summary>Serializable definition metadata with borrowed prefab references and no live UI or project delegates.</summary>
    /// <remarks>Construction stores values; UIContextSettings validates them before producing registration metadata.</remarks>
    [Serializable]
    public sealed class UIContextDefinitionData
    {
        [SerializeField] private string _id;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private string _assetKey;
        [SerializeField] private UIRole _role = UIRole.Popup;
        [SerializeField] private string _hostId = "default";
        [SerializeField] private UIInputMode _inputMode = UIInputMode.Modeless;
        [SerializeField] private UIRetention _retention = UIRetention.DestroyOnClose;
        [SerializeField] private UIHideStrategy _hideStrategy = UIHideStrategy.DeactivateView;

        /// <summary>Creates empty metadata for Unity serialization; configure valid values before creating a snapshot.</summary>
        public UIContextDefinitionData()
        {
        }

        /// <summary>Stores metadata without loading assets, validating registrations, or creating a display.</summary>
        /// <param name="id">Unique, nonblank definition ID within a context.</param>
        /// <param name="prefab">Borrowed live direct source, or null when an explicit asset key is used.</param>
        /// <param name="assetKey">Provider key, or null when a direct source is used.</param>
        /// <param name="role">HUD or popup role.</param>
        /// <param name="hostId">Registered borrowed host ID; default refers to the owner root.</param>
        /// <param name="inputMode">Default native input policy.</param>
        /// <param name="retention">Owned clone retention after display termination.</param>
        /// <param name="hideStrategy">Native hiding strategy of owned displays.</param>
        public UIContextDefinitionData(string id, GameObject prefab = null, string assetKey = null,
            UIRole role = UIRole.Popup, string hostId = "default",
            UIInputMode inputMode = UIInputMode.Modeless,
            UIRetention retention = UIRetention.DestroyOnClose,
            UIHideStrategy hideStrategy = UIHideStrategy.DeactivateView)
        {
            _id = id;
            _prefab = prefab;
            _assetKey = assetKey;
            _role = role;
            _hostId = hostId;
            _inputMode = inputMode;
            _retention = retention;
            _hideStrategy = hideStrategy;
        }

        /// <summary>Gets the definition ID.</summary>
        public string Id => _id;
        /// <summary>Gets the borrowed direct prefab; metadata does not own its lifetime.</summary>
        public GameObject Prefab => _prefab;
        /// <summary>Gets the explicit provider key.</summary>
        public string AssetKey => _assetKey;
        /// <summary>Gets the HUD or popup role.</summary>
        public UIRole Role => _role;
        /// <summary>Gets the registered physical host ID.</summary>
        public string HostId => _hostId;
        /// <summary>Gets the default input policy.</summary>
        public UIInputMode InputMode => _inputMode;
        /// <summary>Gets owned clone retention.</summary>
        public UIRetention Retention => _retention;
        /// <summary>Gets owned display hiding.</summary>
        public UIHideStrategy HideStrategy => _hideStrategy;
    }

    /// <summary>Owns reusable registration/preload metadata while borrowing its prefab assets.</summary>
    /// <remarks>
    /// Inspector data and Configure use the same validation when creating a snapshot. This asset contains no
    /// context, scene host, service, or callback. Validation and configuration require Unity's main thread.
    /// Each installer snapshots metadata at Install; later asset edits do not alter an installed owner.
    /// </remarks>
    [CreateAssetMenu(menuName = "TPLab/UI Context Settings")]
    public sealed class UIContextSettings : ScriptableObject
    {
        [SerializeField] private UIContextDefinitionData[] _definitions = Array.Empty<UIContextDefinitionData>();
        [SerializeField] private string _firstHudDefinitionId;
        [SerializeField] private string[] _preloadDefinitionIds = Array.Empty<string>();

        /// <summary>Gets the optional first HUD ID; null or empty means no automatic display.</summary>
        public string FirstHudDefinitionId => _firstHudDefinitionId;
        /// <summary>Gets a detached read-only copy of optional source-preload IDs.</summary>
        public IReadOnlyList<string> PreloadDefinitionIds
            => Array.AsReadOnly((string[])(_preloadDefinitionIds ?? Array.Empty<string>()).Clone());

        /// <summary>Validates and replaces metadata with defensive copies without loading or changing native scene objects.</summary>
        /// <param name="definitions">Definition metadata to copy; null means an empty registry.</param>
        /// <param name="firstHudDefinitionId">Optional registered HUD ID; null or empty means no initial HUD.</param>
        /// <param name="preloadDefinitionIds">Unique registered IDs whose source assets are optionally prepared.</param>
        /// <remarks>Invalid input preserves previous metadata. Prefabs stay borrowed; no services or clones are created.</remarks>
        /// <exception cref="ArgumentException">IDs, sources, policies, HUD selection, or preload references are invalid.</exception>
        /// <exception cref="InvalidOperationException">Called outside Unity's main thread.</exception>
        public void Configure(UIContextDefinitionData[] definitions,
            string firstHudDefinitionId = null, string[] preloadDefinitionIds = null)
        {
            UIContext.EnsureMainThread();
            UIDefinition[] snapshot = ValidateAndCopy(definitions, firstHudDefinitionId, preloadDefinitionIds);
            var copy = new UIContextDefinitionData[snapshot.Length];
            for (int index = 0; index < snapshot.Length; ++index)
            {
                UIDefinition definition = snapshot[index];
                copy[index] = new UIContextDefinitionData(definition.Id, definition.Prefab, definition.AssetKey,
                    definition.Role, definition.HostId, definition.InputMode, definition.Retention, definition.HideStrategy);
            }
            _definitions = copy;
            _firstHudDefinitionId = firstHudDefinitionId;
            _preloadDefinitionIds = preloadDefinitionIds == null ? Array.Empty<string>() : (string[])preloadDefinitionIds.Clone();
        }

        /// <summary>Validates current serialized values and creates detached immutable definition metadata.</summary>
        /// <returns>A new read-only registry snapshot; prefab references remain borrowed.</returns>
        /// <exception cref="ArgumentException">Serialized definition/HUD/preload metadata is invalid.</exception>
        /// <exception cref="InvalidOperationException">Called outside Unity's main thread.</exception>
        public IReadOnlyList<UIDefinition> CreateSnapshot()
        {
            UIContext.EnsureMainThread();
            return Array.AsReadOnly(ValidateAndCopy(_definitions, _firstHudDefinitionId, _preloadDefinitionIds));
        }

        private static UIDefinition[] ValidateAndCopy(UIContextDefinitionData[] definitions,
            string firstHudDefinitionId, string[] preloads)
        {
            definitions = definitions ?? Array.Empty<UIContextDefinitionData>();
            var snapshot = new UIDefinition[definitions.Length];
            var byId = new Dictionary<string, UIDefinition>(StringComparer.Ordinal);
            for (int index = 0; index < definitions.Length; ++index)
            {
                UIContextDefinitionData data = definitions[index];
                if (data == null || string.IsNullOrWhiteSpace(data.Id) || byId.ContainsKey(data.Id))
                {
                    throw new ArgumentException("Definition IDs must be unique and nonblank.", nameof(definitions));
                }
                bool hasPrefab = data.Prefab != null;
                bool hasKey = !string.IsNullOrWhiteSpace(data.AssetKey);
                if (hasPrefab == hasKey)
                {
                    throw new ArgumentException("Exactly one live prefab or explicit provider key is required.", nameof(definitions));
                }
                if (string.IsNullOrWhiteSpace(data.HostId)
                    || !Enum.IsDefined(typeof(UIRole), data.Role)
                    || !Enum.IsDefined(typeof(UIInputMode), data.InputMode)
                    || !Enum.IsDefined(typeof(UIRetention), data.Retention)
                    || !Enum.IsDefined(typeof(UIHideStrategy), data.HideStrategy))
                {
                    throw new ArgumentException("Definition host or policy is invalid.", nameof(definitions));
                }
                var definition = new UIDefinition(data.Id, data.Prefab, data.AssetKey, data.Role,
                    data.HostId, data.InputMode, data.Retention, data.HideStrategy);
                snapshot[index] = definition;
                byId.Add(definition.Id, definition);
            }
            if (!string.IsNullOrEmpty(firstHudDefinitionId)
                && (!byId.TryGetValue(firstHudDefinitionId, out UIDefinition first) || first.Role != UIRole.Hud))
            {
                throw new ArgumentException("The first HUD must refer to a registered HUD definition.", nameof(firstHudDefinitionId));
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in preloads ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(id) || !byId.ContainsKey(id) || !seen.Add(id))
                {
                    throw new ArgumentException("Preload IDs must be unique registered definitions.", nameof(preloads));
                }
            }
            return snapshot;
        }
    }
}