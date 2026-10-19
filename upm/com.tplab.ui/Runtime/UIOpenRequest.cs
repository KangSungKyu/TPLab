namespace TPLab.UI
{
    /// <summary>Project display parameters; acceptance snapshots hooks and never infers a parent from depth.</summary>
    public sealed class UIOpenRequest
    {
        /// <summary>Creates a request without preparing assets or changing input.</summary>
        /// <param name="definitionId">Registered definition to display.</param>
        /// <param name="parent">Optional live display in the same context; null means context ownership.</param>
        /// <param name="inputMode">Override of the definition mode, or null to use its default.</param>
        /// <param name="hooks">Optional project callbacks for this generation; domain data stays with the project.</param>
        public UIOpenRequest(string definitionId, UIHandle parent = null,
            UIInputMode? inputMode = null, UIHooks hooks = null)
        {
            DefinitionId = definitionId;
            Parent = parent;
            InputMode = inputMode;
            Hooks = hooks;
        }

        /// <summary>Gets the registered screen ID.</summary>
        public string DefinitionId { get; }
        /// <summary>Gets the explicitly requested logical parent.</summary>
        public UIHandle Parent { get; }
        /// <summary>Gets the requested input override, if any.</summary>
        public UIInputMode? InputMode { get; }
        /// <summary>Gets project hooks to snapshot on acceptance.</summary>
        public UIHooks Hooks { get; }
    }
}