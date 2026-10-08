namespace TPLab.UI
{
    /// <summary>Observable state of one display generation, independent of native activation callbacks.</summary>
    public enum UIState
    {
        /// <summary>The request is accepted but preparation or opening has not completed.</summary>
        Opening,
        /// <summary>Preparation and opening have completed.</summary>
        Visible,
        /// <summary>Termination has begun and remains responsible for cleanup.</summary>
        Closing,
        /// <summary>This generation has terminated; inspect its completion tasks for errors.</summary>
        Closed
    }

    /// <summary>Defines the screen role without prescribing Canvas placement.</summary>
    public enum UIRole
    {
        /// <summary>A context's selected heads-up display.</summary>
        Hud,
        /// <summary>An independently requested popup.</summary>
        Popup
    }

    /// <summary>Defines a popup's input policy, separately from rendering order.</summary>
    public enum UIInputMode
    {
        /// <summary>Does not acquire gameplay blocking merely by being displayed.</summary>
        Modeless,
        /// <summary>Participates in the context's modal input boundary.</summary>
        Modal
    }

    /// <summary>Identifies an explicit project/native user request without forcing display termination.</summary>
    public enum UIUserCloseReason
    {
        /// <summary>An opted-in cancel action intercepted before the native EventSystem dispatch.</summary>
        Cancel,
        /// <summary>A project-owned outside-pointer callback; no automatic backdrop is created.</summary>
        OutsidePointer,
        /// <summary>A project-owned explicit close-button callback.</summary>
        Button
    }
    /// <summary>Defines owned clone retention after a managed close.</summary>
    public enum UIRetention
    {
        /// <summary>Destroy the owned clone after each display.</summary>
        DestroyOnClose,
        /// <summary>Keep a reset inactive clone until its owning context ends.</summary>
        Reuse
    }

    /// <summary>Defines native hiding; both strategies require managed input and lifetime cleanup.</summary>
    public enum UIHideStrategy
    {
        /// <summary>Deactivate the view GameObject; this alone does not cancel arbitrary asynchronous work.</summary>
        DeactivateView,
        /// <summary>Disable only a dedicated view Canvas or a whole registered host's rendering.</summary>
        DisableCanvasRendering
    }
}