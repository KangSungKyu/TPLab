using System;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using TPLab.Netcode;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

namespace TPLab.Samples.Netcode
{
    /// <summary>Project-owned local walkthrough controls. Uses the sample's public operations and never owns shared scenes.</summary>
    public sealed class NetcodeSessionSampleView : MonoBehaviour
    {
        [SerializeField] private NetcodeSessionSample _sample;
        [SerializeField] private NetworkManager _manager;
        [SerializeField] private Text _status;
        [SerializeField] private Text _coverText;
        [SerializeField] private CanvasGroup _cover;
        [SerializeField] private InputField _sessionId;
        [SerializeField] private InputField _attemptId;
        [SerializeField] private InputField _roster;
        [SerializeField] private InputField _port;
        [SerializeField] private Dropdown _role;
        [SerializeField] private Toggle _failRestore;
        [SerializeField] private Button[] _buttons = Array.Empty<Button>();
        [SerializeField] private string[] _commands = Array.Empty<string>();
        private UnityEngine.Events.UnityAction[] _listeners;
        private string _notice = "Start Host first, then join from two other players. This is a local demo, not authentication.";
        private bool _initialized;
        private float _nextRefresh;

        /// <summary>Authoring wiring. Referenced scene objects remain project-owned; call before enabling this component.</summary>
        public void Configure(NetcodeSessionSample sample, NetworkManager manager, Text status, Text coverText,
            CanvasGroup cover, InputField sessionId, InputField attemptId, InputField roster, InputField port,
            Dropdown role, Toggle failRestore, Button[] buttons, string[] commands)
        {
            _sample = sample; _manager = manager; _status = status; _coverText = coverText; _cover = cover;
            _sessionId = sessionId; _attemptId = attemptId; _roster = roster; _port = port; _role = role;
            _failRestore = failRestore; _buttons = buttons; _commands = commands;
        }

        private void Start()
        {
            try
            {
                if (_sample == null || _manager == null || _status == null || _coverText == null || _cover == null
                    || _sessionId == null || _attemptId == null || _roster == null || _port == null || _role == null
                    || _failRestore == null || _buttons == null || _commands == null || _buttons.Length != _commands.Length
                    || _buttons.Any(button => button == null))
                    throw new InvalidOperationException("Session sample scene wiring is incomplete.");
                // Explicit demo values, not wrapper defaults or measured production budgets.
                _sample.Configure(_manager, new NetworkSessionOptions(4096, 120, 10, 10));
                _listeners = new UnityEngine.Events.UnityAction[_buttons.Length];
                for (int i = 0; i < _buttons.Length; i++)
                {
                    string command = _commands[i];
                    _listeners[i] = () => ExecuteAsync(command).Forget(ShowError);
                    _buttons[i].onClick.AddListener(_listeners[i]);
                }
                _initialized = true;
                Refresh();
            }
            catch (Exception error) { ShowError(error); }
        }

        /// <summary>Runs the same operation as a sample button. A pending Start does not prevent Ready or owner Stop.</summary>
        public async UniTask ExecuteAsync(string command)
        {
            if (!_initialized) throw new InvalidOperationException("Session sample is not initialized.");
            try
            {
                switch (command)
                {
                    case "start":
                        if (!CanStart()) throw new InvalidOperationException("Finish cleanup or recovery before starting another attempt.");
                        if (!ushort.TryParse(_port.text, out ushort port) || port == 0)
                            throw new ArgumentException("Port must be between 1 and 65535.");
                        var transport = _manager.NetworkConfig.NetworkTransport as UnityTransport;
                        if (transport == null) throw new InvalidOperationException("This local sample requires UnityTransport.");
                        transport.SetConnectionData("127.0.0.1", port, "127.0.0.1");
                        _sample.FailNextRestore = _failRestore.isOn;
                        string proof = _role.value == 0 ? "demo-host" : _role.value == 1 ? "demo-c1" : "demo-c2";
                        _notice = "Connecting. Use Ready after local preparation begins.";
                        ApplyProtection(true);
                        if (_role.value == 0)
                            await _sample.StartHostAsync(_sessionId.text, _attemptId.text,
                                _roster.text.Split(',').Select(value => value.Trim()).ToArray(), Encoding.UTF8.GetBytes(proof));
                        else
                            await _sample.StartClientAsync(_sessionId.text, _attemptId.text, Encoding.UTF8.GetBytes(proof));
                        break;
                    case "ready": _sample.CompletePreparation(); break;
                    case "fail": _sample.FailPreparation(); break;
                    case "stop":
                        if (_sample.Context != null && _sample.Context.State == NetworkSessionState.RecoveryFault)
                        {
                            _notice = "Lobby restoration failed. Use Retry Restore, or Quit Safely to exit after cleanup.";
                            break;
                        }
                        _notice = "Waiting for owned cleanup.";
                        ApplyProtection(true);
                        await _sample.ShutdownAsync();
                        break;
                    case "retry":
                        _notice = "Retrying lobby restoration after verified cleanup.";
                        ApplyProtection(true);
                        await _sample.RetryLobbyRestoreAsync();
                        break;
                    case "interact":
                        _notice = _sample.RecordGameplayInput() ? "Gameplay input accepted." : "Gameplay remains blocked.";
                        break;
                    case "quit":
                        ApplyProtection(true);
                        var stopped = await _sample.ShutdownAsync();
                        if (stopped != null && stopped.State != NetworkSessionState.Closed && stopped.State != NetworkSessionState.Lobby)
                            throw new InvalidOperationException("Cleanup is not complete. Keep this window open and inspect recovery.");
                        _notice = "Session stopped. In the Editor, leave Play Mode now.";
                        if (!Application.isEditor) Application.Quit();
                        break;
                    default: throw new ArgumentException("Unknown sample command.", nameof(command));
                }
            }
            finally
            {
                if (this != null) Refresh();
            }
        }

        private void Update()
        {
            if (!_initialized) return;
            ApplyProtection(_sample.IsProtected);
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + .1f;
            Refresh();
        }

        private bool CanStart()
        {
            var context = _sample.Context;
            return !_sample.IsProtected && (context == null || context.State == NetworkSessionState.Lobby || context.State == NetworkSessionState.Closed);
        }

        private void Refresh()
        {
            if (_sample == null) return;
            bool idle = CanStart();
            var context = _sample.Context;
            bool retry = context != null && context.CanRetryLobbyRestore;
            _sessionId.interactable = idle; _attemptId.interactable = idle; _roster.interactable = idle;
            _port.interactable = idle; _role.interactable = idle; _failRestore.interactable = idle;
            ApplyProtection(_sample.IsProtected);
            _coverText.text = _sample.IsPreparing ? "PREPARING\nSelect Ready when your local work is complete."
                : context != null && context.State == NetworkSessionState.RecoveryFault
                    ? "RECOVERY REQUIRED\nKeep gameplay blocked. Retry restoration only when cleanup is complete."
                    : context != null && context.State == NetworkSessionState.Closed && _sample.IsProtected
                        ? "SAFE TO EXIT\nLobby restoration did not succeed. Leave Play Mode or quit this window."
                        : "WAITING FOR THE SESSION\nGameplay stays blocked until everyone is ready.";
            _status.text = _sample.StatusText + "\n\nLocal preparation: " + (_sample.IsPreparing ? "waiting for Ready" : "not waiting")
                + "\nGameplay: " + (_sample.GameplayEnabled ? "enabled" : "blocked")
                + "\nAccepted gameplay inputs: " + _sample.GameplayInputCount
                + "\nRelease / restore calls: " + _sample.ReleaseCalls + " / " + _sample.RestoreCalls
                + RecoveryDiagnostics() + "\n\n" + _notice;
            for (int i = 0; i < _buttons.Length; i++)
            {
                string command = _commands[i];
                _buttons[i].interactable = command == "start" ? idle
                    : command == "ready" || command == "fail" ? _sample.IsPreparing
                    : command == "retry" ? retry
                    : command == "interact" ? _sample.GameplayEnabled && !_sample.IsProtected
                    : true;
            }
        }

        // Apply before UI commands dispatch native work; status text can refresh less frequently.
        private void ApplyProtection(bool protect)
        {
            _cover.alpha = protect ? 1 : 0;
            _cover.blocksRaycasts = protect;
            _cover.interactable = protect;
        }

        private string RecoveryDiagnostics()
        {
            var result = _sample.LastResult;
            if (result == null || result.RecoveryErrors.Count == 0) return string.Empty;
            string message = "\nRecovery errors (local):";
            foreach (var error in result.RecoveryErrors)
            {
                string detail = error.Message.Replace('\n', ' ').Replace('\r', ' ');
                if (detail.Length > 100) detail = detail.Substring(0, 100) + "...";
                message += "\n" + error.GetType().Name + ": " + detail;
            }
            return message;
        }

        private void ShowError(Exception error)
        {
            // Project UI shows local diagnostics; nothing here is forwarded to remote peers.
            _notice = error.GetType().Name + ": " + error.Message;
            if (_status != null) _status.text = _notice;
        }

        private void OnDestroy()
        {
            if (_listeners == null) return;
            for (int i = 0; i < _listeners.Length; i++)
                if (_buttons[i] != null) _buttons[i].onClick.RemoveListener(_listeners[i]);
        }
    }
}
