using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using B2BAgentPortal.Data;
using B2BAgentPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// Database: the connection string comes from configuration, never from code
builder.Services.AddDbContext<B2BContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("B2BWholesaleDb")));

builder.Services.AddScoped<OrderService>();

// Cookie login: unauthenticated visitors are sent to /Login
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => { o.LoginPath = "/Login"; o.AccessDeniedPath = "/Login"; });
builder.Services.AddAuthorization(o =>
    o.AddPolicy("AgentOnly", p => p.RequireRole("Agent")));

builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/", "AgentOnly");   // every page needs an Agent...
    o.Conventions.AllowAnonymousToPage("/Login");      // ...except the login page
    o.Conventions.AllowAnonymousToPage("/Error");
});

var app = builder.Build();
// Dev only: code-server forwards /absproxy/5000/... WITHOUT removing the prefix,
// so the app has to know it lives under that path. Empty on a normal machine.
// builder.Configuration reads the environment variable PathBase
var pathBase = builder.Configuration["PathBase"];
if (!string.IsNullOrEmpty(pathBase)) app.UsePathBase(pathBase);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();   // reads the login cookie
app.UseAuthorization();

app.MapRazorPages();

app.Run();
