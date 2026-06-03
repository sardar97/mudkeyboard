using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using MudKeyboard.Demo.Wasm;
using MudKeyboard.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();

// Registers the global docked-keyboard services. Pair with a single <MudKeyboardHost /> in the layout.
builder.Services.AddMudKeyboard();

await builder.Build().RunAsync();
