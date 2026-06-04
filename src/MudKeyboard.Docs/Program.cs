using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using MudKeyboard.Docs;
using MudKeyboard.Docs.Services;
using MudKeyboard.Extensions;
using MudKeyboard.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// MudBlazor — MudKeyboard reuses this existing setup; it does not bundle a second theme.
builder.Services.AddMudServices();

// Global docked-keyboard services. The site uses OptIn mode so the docked keyboard only appears for
// fields explicitly marked with data-mudkeyboard (on the "Docked keyboard" page); every other input
// across the docs — including the display fields next to the inline examples — is left untouched.
builder.Services.AddMudKeyboard(o => o.AttachMode = KeyboardAttachMode.OptIn);

// Tiny JS helper used only by the docs site (syntax highlighting + copy-to-clipboard).
builder.Services.AddScoped<DocsInterop>();

await builder.Build().RunAsync();
