namespace MudKeyboard.Docs.Shared;

/// <summary>Whether a "what's new" badge marks a brand-new feature or a change to an existing one.</summary>
public enum BadgeKind
{
    /// <summary>A feature, section or page that did not exist before this release.</summary>
    New,

    /// <summary>A pre-existing feature, section or page that changed this release.</summary>
    Updated,
}

/// <summary>
/// Single source of truth for the "what's new" badges scattered across the docs (nav links, section
/// headings and parameter tables). Bump <see cref="Version"/> each release and move the badges in the
/// markup to whatever changed — the badges all read this version so their tooltips never drift.
/// </summary>
public static class WhatsNew
{
    /// <summary>The release the current crop of <c>New</c>/<c>Updated</c> badges advertises.</summary>
    public const string Version = "1.2.0";
}
