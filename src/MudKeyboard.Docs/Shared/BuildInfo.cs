using System.Reflection;

namespace MudKeyboard.Docs.Shared;

/// <summary>
/// Surfaces the running versions of the MudKeyboard package and of this documentation site so they
/// can be shown in the UI (the footer and the Releases page). Each value is read once from the
/// relevant assembly's <see cref="AssemblyInformationalVersionAttribute"/> — which the .NET SDK
/// derives from that project's <c>&lt;Version&gt;</c> — so the displayed numbers can never drift from
/// the build. Assembly-level attributes survive trimming and AOT, so this is safe on the published
/// WebAssembly site; the hard-coded fallbacks only apply if an attribute is somehow absent.
/// </summary>
public static class BuildInfo
{
    /// <summary>The MudKeyboard NuGet package version these docs are built against (e.g. <c>1.0.2</c>).</summary>
    public static string PackageVersion { get; } =
        Read(typeof(global::MudKeyboard.Services.KeyboardAttachMode).Assembly) ?? "1.1.0";

    /// <summary>The version of this documentation website itself.</summary>
    public static string DocsVersion { get; } =
        Read(typeof(BuildInfo).Assembly) ?? "1.0.0";

    private static string? Read(Assembly assembly)
    {
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(informational))
        {
            return null;
        }

        // The SDK appends source-control build metadata (e.g. "1.0.2+a1b2c3d"); trim it for display.
        var plus = informational.IndexOf('+');
        return plus >= 0 ? informational[..plus] : informational;
    }
}
