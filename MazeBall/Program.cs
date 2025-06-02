using MazeBall.Database.Entities;
using MazeBall.Hubs;
using MazeBall.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]));
    options.TokenValidationParameters = new TokenValidationParameters
    {
        //ValidateIssuer = false,
        //ValidateAudience = false,
        //ValidateLifetime = true,
        //IssuerSigningKey = key,
        //ValidateIssuerSigningKey = true,

        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = key,
        ValidateIssuerSigningKey = true,
    };
}
);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddDbContext<MazeBallContext>(options =>
    options.UseSqlServer(builder.Configuration["ConnectionStrings:DatabaseConnection"]));
builder.Services.AddScoped<IUserAuthorizationService, UserAuthorizationService>();
builder.Services.AddSingleton<GameHub>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();
app.MapHub<LobbyHub>("/lobby/lobbyHub");
app.MapHub<GameHub>("/game/gameHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.MapControllerRoute(
    name: "Lobby",
    pattern: "lobby",
    defaults: new { controller = "Home", action = "Lobby" });

app.MapControllerRoute(
    name: "Game",
    pattern: "game",
    defaults: new { controller = "Home", action = "Game" });

app.Run();
