using Microsoft.Extensions.DependencyInjection;
using MudKeyboard.Services;

namespace MudKeyboard.Extensions;

/// <summary>
/// Registration helpers for MudKeyboard's global docked-keyboard feature.
/// </summary>
public static class MudKeyboardServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services needed by <see cref="MudKeyboard.Components.MudKeyboardHost"/>: the
    /// global <see cref="MudKeyboardOptions"/> and the scoped <see cref="KeyboardInteropService"/>.
    /// Call this in <c>Program.cs</c>, then place a single <c>&lt;MudKeyboardHost /&gt;</c> in your layout.
    /// Not required for the inline <see cref="MudKeyboard.Components.MudKeyboard"/> component.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration (for example the attach mode).</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddMudKeyboard(
        this IServiceCollection services,
        Action<MudKeyboardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MudKeyboardOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<KeyboardInteropService>();
        return services;
    }
}
