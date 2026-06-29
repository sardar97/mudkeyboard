using MudKeyboard.Models;

namespace MudKeyboard.Docs.Services;

/// <summary>
/// Shared state that lets the "Docked keyboard" page drive the single, layout-level
/// <c>MudKeyboardHost</c>'s toolbar parameters live. The host is global (placed once in
/// <c>MainLayout</c>), so the interactive <c>VisibleActions</c>/<c>DisabledActions</c> demo on the page
/// can't render its own host — it nudges this state instead, and the layout re-renders the host.
/// </summary>
public sealed class DocsKeyboardToolbarState
{
    private KeyboardAction _visibleActions = KeyboardAction.All;
    private KeyboardAction _disabledActions = KeyboardAction.None;
    private bool _showValuePreview;
    private bool _showBackdrop;
    private bool _disableBackdropClick;
    private bool _sound;

    /// <summary>Raised whenever the toolbar settings change, so the layout host re-renders.</summary>
    public event Action? Changed;

    /// <summary>Whether the docked keyboard shows the live value-preview bar. Bound to <c>MudKeyboardHost.ShowValuePreview</c>.</summary>
    public bool ShowValuePreview
    {
        get => _showValuePreview;
        set => Set(ref _showValuePreview, value);
    }

    /// <summary>Whether a dimming backdrop is shown behind the docked keyboard. Bound to <c>MudKeyboardHost.ShowBackdrop</c>.</summary>
    public bool ShowBackdrop
    {
        get => _showBackdrop;
        set => Set(ref _showBackdrop, value);
    }

    /// <summary>Whether backdrop clicks are ignored (showing a Cancel button instead). Bound to <c>MudKeyboardHost.DisableBackdropClick</c>.</summary>
    public bool DisableBackdropClick
    {
        get => _disableBackdropClick;
        set => Set(ref _disableBackdropClick, value);
    }

    /// <summary>Whether the docked keyboard plays a click sound on key press. Bound to <c>MudKeyboardHost.Sound</c>.</summary>
    public bool Sound
    {
        get => _sound;
        set => Set(ref _sound, value);
    }

    /// <summary>Which toolbar buttons the docked keyboard renders. Bound to <c>MudKeyboardHost.VisibleActions</c>.</summary>
    public KeyboardAction VisibleActions
    {
        get => _visibleActions;
        set => Set(ref _visibleActions, value);
    }

    /// <summary>Which toolbar buttons render greyed-out. Bound to <c>MudKeyboardHost.DisabledActions</c>.</summary>
    public KeyboardAction DisabledActions
    {
        get => _disabledActions;
        set => Set(ref _disabledActions, value);
    }

    /// <summary>Whether <paramref name="action"/> is currently in the visible set.</summary>
    public bool IsVisible(KeyboardAction action) => (_visibleActions & action) == action;

    /// <summary>Whether <paramref name="action"/> is currently in the disabled set.</summary>
    public bool IsDisabled(KeyboardAction action) => (_disabledActions & action) == action;

    /// <summary>Adds or removes <paramref name="action"/> from the visible set.</summary>
    public void SetVisible(KeyboardAction action, bool visible) =>
        VisibleActions = visible ? _visibleActions | action : _visibleActions & ~action;

    /// <summary>Adds or removes <paramref name="action"/> from the disabled set.</summary>
    public void SetDisabled(KeyboardAction action, bool disabled) =>
        DisabledActions = disabled ? _disabledActions | action : _disabledActions & ~action;

    /// <summary>Restores the defaults (all buttons visible, none disabled, new features off).</summary>
    public void Reset()
    {
        _disabledActions = KeyboardAction.None;
        _showValuePreview = false;
        _showBackdrop = false;
        _disableBackdropClick = false;
        _sound = false;
        VisibleActions = KeyboardAction.All; // raises Changed
    }

    private void Set(ref KeyboardAction field, KeyboardAction value)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        Changed?.Invoke();
    }

    private void Set(ref bool field, bool value)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        Changed?.Invoke();
    }
}
