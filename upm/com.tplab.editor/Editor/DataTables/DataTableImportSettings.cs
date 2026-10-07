using UnityEngine;

namespace TPLab.Core.Editor.DataTables
{
    /// <summary>Controls automatic Editor work. Manual commands always run the same required validation.</summary>
    public enum DataTableAutomationMode
    {
        Disabled, ValidateOnly, GenerateValidated
    }

    /// <summary>Project-owned import policy. Absent settings disable automation; this asset is never a runtime dependency.</summary>
    [CreateAssetMenu(menuName = "TPLab/Data Table Import Settings", fileName = "setting")]
    public sealed class DataTableImportSettings : ScriptableObject
    {
        /// <summary>The single active settings location used by the automatic importer.</summary>
        public const string ActivePath = "Assets/Editor/TPLab/setting.asset";
        [SerializeField] private DataTableAutomationMode _automationMode;
        [SerializeField] private string _inputFolder = "Assets/Game/Data";
        [SerializeField] private string _schemaFolder = "Assets/Game/DataSchemas";
        [SerializeField] private string _outputFolder = "Assets/Game/Generated/Data";
        [SerializeField] private string _defaultNamespace = "Game.Data";
        [SerializeField] private string _validationProfileId = "";

        /// <summary>Automatic mode. Disabled still permits explicit manual commands.</summary>
        public DataTableAutomationMode AutomationMode
        {
            get => _automationMode; set => _automationMode = value;
        }
        /// <summary>Assets folder containing row CSV files.</summary>
        public string InputFolder
        {
            get => _inputFolder; set => _inputFolder = value;
        }
        /// <summary>Assets folder containing table schema JSON files.</summary>
        public string SchemaFolder
        {
            get => _schemaFolder; set => _schemaFolder = value;
        }
        /// <summary>Project runtime source folder, outside inputs and protected core/test paths.</summary>
        public string OutputFolder
        {
            get => _outputFolder; set => _outputFolder = value;
        }
        /// <summary>Namespace used when a schema does not specify one.</summary>
        public string DefaultNamespace
        {
            get => _defaultNamespace; set => _defaultNamespace = value;
        }
        /// <summary>Explicit project Editor registration identity; functions and Type objects are not serialized.</summary>
        public string ValidationProfileId
        {
            get => _validationProfileId; set => _validationProfileId = value;
        }
    }
}
