using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Keuken_inmeten;
using Keuken_inmeten.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<KeukenStateService>();
builder.Services.AddScoped<PersistentieService>();
builder.Services.AddScoped<ActieFeedbackService>();
builder.Services.AddScoped<DeelLinkService>();

var host = builder.Build();

// Load persisted state before rendering begins
var persistentie = host.Services.GetRequiredService<PersistentieService>();
await persistentie.LadenAsync();

await host.RunAsync();
