using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Object = UnityEngine.Object;

namespace TPLab.Core.Input
{
    /// <summary>Describes a native binding candidate before any override is committed.</summary>
    public sealed class RebindCandidate
    {
        internal RebindCandidate(InputAction action, Guid bindingId, string path, InputControl control)
        {
            Action = action;
            BindingId = bindingId;
            Path = path;
            Control = control;
        }
        /// <summary>Borrowed runtime action; activation and overrides remain manager-owned.</summary>
        public InputAction Action { get; }
        /// <summary>Stable ID of the single binding or composite part being changed.</summary>
        public Guid BindingId { get; }
        /// <summary>Native generalized control path proposed for the override.</summary>
        public string Path { get; }
        /// <summary>Borrowed selected control, whose device may be removed before commit.</summary>
        public InputControl Control { get; }
    }

    /// <summary>Project-owned selection policy. IDs are stable; composite parts are rebound individually.</summary>
    public sealed class RebindRequest
    {
        /// <summary>Targets one action and binding GUID belonging to this scope.</summary>
        public RebindRequest(Guid actionId, Guid bindingId)
        {
            ActionId = actionId;
            BindingId = bindingId;
        }
        /// <summary>Stable target action ID.</summary>
        public Guid ActionId { get; }
        /// <summary>Stable target binding ID; composite roots are rejected.</summary>
        public Guid BindingId { get; }
        /// <summary>Optional native control matching path, such as &lt;Keyboard&gt;.</summary>
        public string ControlPath { get; set; }
        /// <summary>Optional native binding group mask.</summary>
        public string BindingGroup { get; set; }
        /// <summary>Native cancellation control path; null or empty disables key cancellation.</summary>
        public string CancelPath { get; set; } = "<Keyboard>/escape";
        /// <summary>Realtime deadline for selection and release together; zero disables the deadline.</summary>
        public float TimeoutSeconds { get; set; } = 10;
        /// <summary>Replaces the default same-map/group conflict policy; false rejects without mutation.</summary>
        public Func<RebindCandidate, bool> Validator { get; set; }
        /// <summary>Optional neutral predicate for continuous controls; buttons always wait for release.</summary>
        public Func<InputControl, bool> IsReleased { get; set; }
    }

    /// <summary>Distinguishes committed, policy-rejected, key/device-cancelled and expired requests.</summary>
    public enum RebindStatus
    {
        Applied,
        Rejected,
        Cancelled,
        TimedOut
    }

    /// <summary>Completed native selection outcome; caller/owner cancellation and errors remain exceptions.</summary>
    public readonly struct RebindResult
    {
        internal RebindResult(RebindStatus status, string path = null)
        {
            Status = status;
            Path = path;
        }
        /// <summary>The completed selection outcome; cancellation tokens and errors throw instead.</summary>
        public RebindStatus Status { get; }
        /// <summary>The applied or rejected candidate path, or null when selection did not finish.</summary>
        public string Path { get; }
    }

    /// <summary>Owns one interactive operation at a time and transactional native override mutations.</summary>
    public sealed class InputRebindingController
    {
        private readonly InputManager _owner;
        private readonly CancellationTokenSource _ownerCancellation = new CancellationTokenSource();
        private InputActionRebindingExtensions.RebindingOperation _operation;
        private Action<InputDevice, InputDeviceChange> _deviceChanged;
        private Exception _callbackFailure;
        private UniTaskCompletionSource _completion;
        private bool _busy;
        private bool _rebinding;
        private bool _committing;
        private bool _stopped;

        internal InputRebindingController(InputManager owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>Whether an interactive request still owns selection, release wait or cleanup.</summary>
        public bool IsRebinding => _rebinding;

        /// <summary>
        /// Rebinds one disabled runtime binding after policy validation and button release.
        /// Options are copied at entry. Only one mutation is permitted; caller/owner cancellation throws.
        /// </summary>
        public async UniTask<RebindResult> RebindAsync(RebindRequest request, CancellationToken cancellationToken = default)
        {
            EnsureAvailable();
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var options = CopyRequest(request);
            if (float.IsNaN(options.TimeoutSeconds) || float.IsInfinity(options.TimeoutSeconds) || options.TimeoutSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Timeout must be finite and nonnegative.");
            }
            var action = _owner.GetAction(options.ActionId);
            int bindingIndex = FindBindingIndex(action, options.BindingId);
            if (action.bindings[bindingIndex].isComposite)
            {
                throw new ArgumentException("Rebind composite parts individually.", nameof(request));
            }
            cancellationToken.ThrowIfCancellationRequested();
            BeginWork(true);
            IDisposable block = null;
            CancellationTokenSource linked = null;
            Exception failure = null;
            RebindResult result = default;
            try
            {
                linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _ownerCancellation.Token);
                var token = linked.Token;
                double deadline = options.TimeoutSeconds == 0
                    ? double.PositiveInfinity : Time.realtimeSinceStartupAsDouble + options.TimeoutSeconds;
                block = _owner.Layers.BlockAll();
                CheckLifetime(token);
                RequireDisabled(action);

                RebindCandidate candidate = null;
                InputDevice potentialDevice = null;
                bool completed = false;
                bool cancelled = false;
                _operation = action.PerformInteractiveRebinding(bindingIndex);
                // BlockAll disables managed maps; native device state must still update for button-release waiting.
                _operation.WithMatchingEventsBeingSuppressed(false)
                    .WithCancelingThrough(options.CancelPath)
                    .OnPotentialMatch(operation =>
                    {
                        try
                        {
                            // Removed devices no longer resolve through the native candidate control list.
                            potentialDevice = operation.selectedControl?.device;
                        }
                        catch (Exception error)
                        {
                            _callbackFailure = error;
                        }
                    })
                    .OnApplyBinding((operation, path) =>
                    {
                        try
                        {
                            candidate = new RebindCandidate(action, options.BindingId, path, operation.selectedControl);
                        }
                        catch (Exception error)
                        {
                            _callbackFailure = error;
                        }
                    })
                    .OnComplete(_ => completed = true)
                    .OnCancel(_ => cancelled = true);
                if (!string.IsNullOrEmpty(options.ControlPath))
                {
                    _operation.WithControlsHavingToMatchPath(options.ControlPath);
                }
                if (!string.IsNullOrEmpty(options.BindingGroup))
                {
                    _operation.WithBindingGroup(options.BindingGroup);
                }
                _deviceChanged = (device, change) =>
                {
                    if (change != InputDeviceChange.Removed && change != InputDeviceChange.Disconnected)
                    {
                        return;
                    }
                    var operation = _operation;
                    if (operation == null)
                    {
                        return;
                    }
                    try
                    {
                        bool selected = potentialDevice == device || candidate?.Control?.device == device;
                        if (selected)
                        {
                            cancelled = true;
                            // Cancel unhooks native onAfterUpdate before it can complete a removed control.
                            operation.Cancel();
                        }
                    }
                    catch (Exception error)
                    {
                        _callbackFailure = error;
                        try
                        {
                            operation.Cancel();
                        }
                        catch (Exception cleanupFailure)
                        {
                            _callbackFailure = Combine(error, cleanupFailure);
                        }
                    }
                };
                InputSystem.onDeviceChange += _deviceChanged;
                _operation.Start();
                while (!completed && !cancelled && _callbackFailure == null)
                {
                    CheckLifetime(token);
                    RequireDisabled(action);
                    if (Time.realtimeSinceStartupAsDouble >= deadline)
                    {
                        result = new RebindResult(RebindStatus.TimedOut);
                        break;
                    }
                    await UniTask.NextFrame(token);
                }
                CheckLifetime(token);
                if (_callbackFailure != null)
                {
                    ExceptionDispatchInfo.Capture(_callbackFailure).Throw();
                }
                if (cancelled)
                {
                    result = new RebindResult(RebindStatus.Cancelled);
                }
                else if (completed)
                {
                    result = await ValidateAndCommitAsync(candidate, options, deadline, token);
                }
            }
            catch (ObjectDisposedException error) when (linked != null && linked.IsCancellationRequested)
            {
                // Disabling maps can synchronously stop the owner before BlockAll returns its lease.
                failure = new OperationCanceledException("Input scope was cancelled during rebinding.", error, linked.Token);
            }
            catch (Exception error)
            {
                failure = error;
            }
            finally
            {
                failure = Cleanup(block, linked, failure);
                EndWork();
            }
            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
            return result;
        }

        /// <summary>Reads committed native override JSON during selection; rejects an in-progress native commit.</summary>
        public string ExportOverridesJson()
        {
            _owner.EnsureActive();
            if (_committing)
            {
                throw new InvalidOperationException("Overrides are being committed.");
            }
            return _owner.Actions.SaveBindingOverridesAsJson();
        }

        /// <summary>
        /// Validates native override JSON on a temporary clone, then replaces overrides transactionally.
        /// Empty restores defaults; null is rejected. Invalid schema/IDs leave input unchanged; active mutations reject.
        /// </summary>
        public void ImportOverridesJson(string json)
        {
            EnsureAvailable();
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }
            Mutate(() => _owner.Actions.LoadBindingOverridesFromJson(json), () => ValidateOverrides(json));
        }

        /// <summary>Removes one known binding's overrides while preserving the current layer ownership.</summary>
        public void ResetBinding(Guid actionId, Guid bindingId)
        {
            EnsureAvailable();
            var action = _owner.GetAction(actionId);
            FindBindingIndex(action, bindingId);
            Mutate(() => action.RemoveBindingOverride(FindBindingIndex(action, bindingId)));
        }

        /// <summary>Removes every runtime override transactionally; the source asset is untouched.</summary>
        public void ResetAll()
        {
            Mutate(() => _owner.Actions.RemoveAllBindingOverrides());
        }

        internal void Stop()
        {
            if (_stopped)
            {
                return;
            }
            _stopped = true;
            RemoveDeviceListener();
            var operation = _operation;
            _operation = null;
            try
            {
                _ownerCancellation.Cancel();
            }
            finally
            {
                _ownerCancellation.Dispose();
                operation?.Dispose();
            }
        }

        internal UniTask WaitForCompletionAsync()
        {
            return _completion == null ? UniTask.CompletedTask : _completion.Task;
        }

        private async UniTask<RebindResult> ValidateAndCommitAsync(RebindCandidate candidate, RebindRequest options,
            double deadline, CancellationToken token)
        {
            if (candidate == null || candidate.Control == null || string.IsNullOrEmpty(candidate.Path))
            {
                throw new InvalidOperationException("Native rebinding completed without a control candidate.");
            }
            CheckLifetime(token);
            if (!candidate.Control.device.added)
            {
                return new RebindResult(RebindStatus.Cancelled);
            }
            if (Time.realtimeSinceStartupAsDouble >= deadline)
            {
                return new RebindResult(RebindStatus.TimedOut);
            }
            if (!string.IsNullOrEmpty(options.ControlPath)
                && !InputControlPath.MatchesPrefix(options.ControlPath, candidate.Control))
            {
                return new RebindResult(RebindStatus.Rejected, candidate.Path);
            }
            bool allowed = options.Validator != null ? options.Validator(candidate) : HasNoConflict(candidate);
            CheckLifetime(token);
            RequireDisabled(candidate.Action);
            if (!candidate.Control.device.added)
            {
                return new RebindResult(RebindStatus.Cancelled);
            }
            if (Time.realtimeSinceStartupAsDouble >= deadline)
            {
                return new RebindResult(RebindStatus.TimedOut);
            }
            if (!allowed)
            {
                return new RebindResult(RebindStatus.Rejected, candidate.Path);
            }
            while (true)
            {
                CheckLifetime(token);
                RequireDisabled(candidate.Action);
                if (!candidate.Control.device.added)
                {
                    return new RebindResult(RebindStatus.Cancelled);
                }
                if (Time.realtimeSinceStartupAsDouble >= deadline)
                {
                    return new RebindResult(RebindStatus.TimedOut);
                }
                if (IsCancelPressed(options.CancelPath))
                {
                    return new RebindResult(RebindStatus.Cancelled);
                }
                bool released = candidate.Control is ButtonControl button
                    ? !button.isPressed : options.IsReleased == null || options.IsReleased(candidate.Control);
                CheckLifetime(token);
                if (released)
                {
                    break;
                }
                await UniTask.NextFrame(token);
            }
            CheckLifetime(token);
            if (!candidate.Control.device.added)
            {
                return new RebindResult(RebindStatus.Cancelled);
            }
            if (Time.realtimeSinceStartupAsDouble >= deadline)
            {
                return new RebindResult(RebindStatus.TimedOut);
            }
            if (IsCancelPressed(options.CancelPath))
            {
                return new RebindResult(RebindStatus.Cancelled);
            }
            RequireDisabled(candidate.Action);
            int bindingIndex = FindBindingIndex(candidate.Action, candidate.BindingId);
            ApplyWithRollback(() => candidate.Action.ApplyBindingOverride(bindingIndex, candidate.Path));
            return new RebindResult(RebindStatus.Applied, candidate.Path);
        }

        private static bool IsCancelPressed(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            using (var controls = InputSystem.FindControls<ButtonControl>(path))
            {
                foreach (var control in controls)
                {
                    if (control.isPressed)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool HasNoConflict(RebindCandidate candidate)
        {
            // ponytail: exact paths only; use Validator for wildcard, alias or composite conflicts.
            var target = candidate.Action.bindings[FindBindingIndex(candidate.Action, candidate.BindingId)];
            foreach (var action in candidate.Action.actionMap.actions)
            {
                foreach (var binding in action.bindings)
                {
                    if (binding.id != candidate.BindingId && !binding.isComposite
                        && string.Equals(binding.effectivePath, candidate.Path, StringComparison.OrdinalIgnoreCase)
                        && GroupsIntersect(target.groups, binding.groups))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool GroupsIntersect(string first, string second)
        {
            if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second))
            {
                return true;
            }
            foreach (var left in first.Split(';'))
            {
                foreach (var right in second.Split(';'))
                {
                    if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void Mutate(Action mutation, Action validation = null)
        {
            EnsureAvailable();
            BeginWork(false);
            IDisposable block = null;
            Exception failure = null;
            try
            {
                validation?.Invoke();
                _owner.EnsureActive();
                block = _owner.Layers.BlockAll();
                _owner.EnsureActive();
                if (_owner.Actions.enabled)
                {
                    throw new InvalidOperationException("Override mutations require disabled actions.");
                }
                ApplyWithRollback(mutation);
            }
            catch (Exception error)
            {
                failure = error;
            }
            finally
            {
                failure = Cleanup(block, null, failure);
                EndWork();
            }
            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        private void ApplyWithRollback(Action mutation)
        {
            var actions = _owner.Actions;
            string previous = actions.SaveBindingOverridesAsJson();
            _committing = true;
            try
            {
                mutation();
            }
            catch (Exception failure)
            {
                try
                {
                    actions.LoadBindingOverridesFromJson(previous);
                }
                catch (Exception rollbackFailure)
                {
                    var combined = new AggregateException(failure, rollbackFailure);
                    _owner.Layers.MarkFault(combined);
                    throw combined;
                }
                throw;
            }
            finally
            {
                _committing = false;
            }
        }

        private void ValidateOverrides(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }
            OverrideDocument document;
            try
            {
                string text = json.Trim();
                if (!text.StartsWith("{", StringComparison.Ordinal) || !text.EndsWith("}", StringComparison.Ordinal))
                {
                    throw new ArgumentException("Native override JSON must be an object.", nameof(json));
                }
                document = JsonUtility.FromJson<OverrideDocument>(text);
                if (document == null || document.bindings == null)
                {
                    throw new ArgumentException("Native override JSON requires a bindings array.", nameof(json));
                }
            }
            catch (Exception failure)
            {
                throw new ArgumentException("Invalid native override JSON.", nameof(json), failure);
            }
            var seen = new HashSet<Guid>();
            foreach (var binding in document.bindings)
            {
                if (binding == null || !Guid.TryParse(binding.id, out var id) || id == Guid.Empty || !seen.Add(id)
                    || string.IsNullOrEmpty(binding.action) || binding.path == null
                    || binding.interactions == null || binding.processors == null)
                {
                    throw new ArgumentException("Override entries require distinct binding IDs and native string fields.", nameof(json));
                }
                var action = _owner.Actions.FindAction(binding.action);
                if (action == null || !string.Equals(binding.action, action.actionMap.name + "/" + action.name, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Override action path does not belong to this scope.", nameof(json));
                }
                FindBindingIndex(action, id);
            }
            var temporary = Object.Instantiate(_owner.Actions);
            try
            {
                temporary.Disable();
                temporary.LoadBindingOverridesFromJson(json);
            }
            catch (Exception failure)
            {
                throw new ArgumentException("Native override validation failed.", nameof(json), failure);
            }
            finally
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(temporary);
                }
                else
                {
                    Object.DestroyImmediate(temporary);
                }
            }
        }

        private Exception Cleanup(IDisposable block, CancellationTokenSource linked, Exception failure)
        {
            RemoveDeviceListener();
            var operation = _operation;
            _operation = null;
            try
            {
                operation?.Dispose();
            }
            catch (Exception cleanupFailure)
            {
                failure = Combine(failure, cleanupFailure);
                try
                {
                    _owner.Layers.MarkFault(failure);
                }
                catch (Exception faultFailure)
                {
                    failure = Combine(failure, faultFailure);
                }
            }
            try
            {
                block?.Dispose();
            }
            catch (Exception cleanupFailure)
            {
                failure = Combine(failure, cleanupFailure);
            }
            linked?.Dispose();
            return failure;
        }

        private static Exception Combine(Exception first, Exception second)
        {
            return first == null ? second : new AggregateException(first, second);
        }

        private static int FindBindingIndex(InputAction action, Guid id)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].id == id && id != Guid.Empty)
                {
                    return i;
                }
            }
            throw new ArgumentException("Binding ID does not belong to the target action.", nameof(id));
        }

        private static void RequireDisabled(InputAction action)
        {
            if (action.enabled)
            {
                throw new InvalidOperationException("Native rebinding requires a disabled action.");
            }
        }

        private void CheckLifetime(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _owner.EnsureActive();
            if (_callbackFailure != null)
            {
                ExceptionDispatchInfo.Capture(_callbackFailure).Throw();
            }
        }

        private void RemoveDeviceListener()
        {
            if (_deviceChanged == null)
            {
                return;
            }
            InputSystem.onDeviceChange -= _deviceChanged;
            _deviceChanged = null;
        }

        private void EnsureAvailable()
        {
            _owner.EnsureActive();
            if (_stopped)
            {
                throw new ObjectDisposedException(nameof(InputRebindingController));
            }
            if (_busy)
            {
                throw new InvalidOperationException("An input mutation is already in progress.");
            }
        }

        private void BeginWork(bool rebinding)
        {
            _busy = true;
            _rebinding = rebinding;
            _callbackFailure = null;
            _completion = new UniTaskCompletionSource();
        }

        private void EndWork()
        {
            _busy = false;
            _rebinding = false;
            var completion = _completion;
            _completion = null;
            completion.TrySetResult();
        }

        private static RebindRequest CopyRequest(RebindRequest request)
        {
            return new RebindRequest(request.ActionId, request.BindingId)
            {
                ControlPath = request.ControlPath,
                BindingGroup = request.BindingGroup,
                CancelPath = request.CancelPath,
                TimeoutSeconds = request.TimeoutSeconds,
                Validator = request.Validator,
                IsReleased = request.IsReleased
            };
        }

        [Serializable]
        private sealed class OverrideDocument
        {
            public OverrideBinding[] bindings = null;
        }

        [Serializable]
        private sealed class OverrideBinding
        {
            public string action = null;
            public string id = null;
            public string path = null;
            public string interactions = null;
            public string processors = null;
        }
    }
}
