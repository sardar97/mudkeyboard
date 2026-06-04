namespace MudKeyboard.Docs.Shared;

/// <summary>A single row in an API reference parameter/member table.</summary>
/// <param name="Name">Member or parameter name.</param>
/// <param name="Type">CLR type, shown verbatim.</param>
/// <param name="Default">Default value, or <c>"—"</c> when not applicable.</param>
/// <param name="Description">What it does.</param>
public sealed record ApiMember(string Name, string Type, string Default, string Description)
{
    /// <summary>Convenience for tables that don't show a Default column.</summary>
    public ApiMember(string name, string type, string description) : this(name, type, "—", description)
    {
    }
}
