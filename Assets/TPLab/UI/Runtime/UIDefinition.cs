using UnityEngine;

namespace TPLab.UI
{
    /// <summary>
    /// Describes one registered screen without owning its prefab, host, or live display.
    /// Registration validates the descriptor; neither registration nor construction instantiates a view.
    /// </summary>
    public sealed class UIDefinition
    {
        /// <summary>Creates metadata with exactly one asset source to be validated by UIContext.Register.</summary>
        /// <param name="id">Stable nonempty definition ID, unique within its context.</param>
        /// <param name="prefab">Borrowed live source GameObject; do not release it while clones depend on it.</param>
        /// <param name="assetKey">Explicit provider key when prefab is absent; no automatic source guessing.</param>
        /// <param name="role">HUD or popup role; does not create a Canvas.</param>
        /// <param name="hostId">Explicit registered display host identifier.</param>
        /// <param name="inputMode">Default input mode, independently of sorting order.</param>
        /// <param name="retention">Owned clone retention; borrowed asset lifetime is unchanged.</param>
        /// <param name="hideStrategy">Managed native hiding strategy.</param>
        public UIDefinition(string id, GameObject prefab = null, string assetKey = null,
            UIRole role = UIRole.Popup, string hostId = "default",
            UIInputMode inputMode = UIInputMode.Modeless,
            UIRetention retention = UIRetention.DestroyOnClose,
            UIHideStrategy hideStrategy = UIHideStrategy.DeactivateView)
        {
            Id = id;
            Prefab = prefab;
            AssetKey = assetKey;
            Role = role;
            HostId = hostId;
            InputMode = inputMode;
            Retention = retention;
            HideStrategy = hideStrategy;
        }

        /// <summary>Gets the definition ID; registration owns validation and uniqueness.</summary>
        public string Id { get; }
        /// <summary>Gets the borrowed direct source, or null for a provider key.</summary>
        public GameObject Prefab { get; }
        /// <summary>Gets the provider key, or null for a direct source.</summary>
        public string AssetKey { get; }
        /// <summary>Gets the screen role.</summary>
        public UIRole Role { get; }
        /// <summary>Gets the requested host identifier.</summary>
        public string HostId { get; }
        /// <summary>Gets the default input mode.</summary>
        public UIInputMode InputMode { get; }
        /// <summary>Gets owned instance retention; this does not release prefab assets.</summary>
        public UIRetention Retention { get; }
        /// <summary>Gets the managed native hiding strategy.</summary>
        public UIHideStrategy HideStrategy { get; }
    }
}