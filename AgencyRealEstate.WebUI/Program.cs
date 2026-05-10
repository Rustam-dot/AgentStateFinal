using AgencyRealEstate.WebUI.Auth;
using AgencyRealEstate.WebUI.Components;
using AgencyRealEstate.WebUI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddMudServices();


builder.Services.AddHttpContextAccessor();


builder.Services.AddScoped<TokenStorage>();


builder.Services.AddScoped<AuthMessageHandler>();


builder.Services.AddScoped(sp =>
{
    var tokenStorage = sp.GetRequiredService<TokenStorage>();
    var handler = new AuthMessageHandler(tokenStorage)
    {
        InnerHandler = new HttpClientHandler()
    };
    var client = new HttpClient(handler)
    {
        BaseAddress = new Uri("https://localhost:7159/api/")
    };
    return client;
});


builder.Services.AddScoped<ApiClient>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:7075") // Порт вашего Blazor приложения
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // РАЗРЕШАЕТ ПЕРЕДАЧУ КУК/АВТОРИЗАЦИИ
    });
});

builder.Services.AddScoped<AuthenticationStateProvider, TokenAuthenticationStateProvider>();
builder.Services.AddAuthorizationCore();

builder.Services.Configure<HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 20 * 1024 * 1024; // 10 МБ
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Custom";
    options.DefaultChallengeScheme = "Custom";
})
.AddScheme<AuthenticationSchemeOptions, CustomAuthenticationHandler>("Custom", null);


builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// CORS должен быть ДО аутентификации/авторизации
app.UseCors("BlazorPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();