namespace MudKeyboard.Docs.Shared;

/// <summary>A single row in an API reference parameter/member table.</summary>
/// <param name="Name">Member or parameter name.</param>
/// <param name="Type">CLR type, shown verbatim.</param>
/// <param name="Default">Default value, or <c>"—"</c> when not applicable.</param>
/// <param name="Description">What it does.</param>
/// <param name="Badge">Optional "what's new" badge shown next to the name (e.g. a member added this release).</param>
public sealed record ApiMember(string Name, string Type, string Default, string Description, BadgeKind? Badge = null)
{
    /// <summary>Convenience for tables that don't show a Default column.</summary>
    public ApiMember(string name, string type, string description, BadgeKind? badge = null)
        : this(name, type, "—", description, badge)
    {
    }
}
