using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;

var builder = WebApplication.CreateBuilder(args);

// Load Ocelot routes
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Constants for JWT signing
const string issuer = "coffee-demo";
const string audience = "coffee-clients";
const string secret = "super-secret-demo-key-12345-67890-abcde"; // 36 chars
var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));


// Configure JWT auth
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("JwtBearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = issuer,
            ValidateAudience = true, ValidAudience = audience,
            ValidateIssuerSigningKey = true, IssuerSigningKey = key,
            ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(5)
        };
    });

builder.Services.AddOcelot().AddPolly();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "gateway" }));

// 👉 Development token endpoint
app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/dev"), devApp =>
{
    devApp.Run(async ctx =>
    {
        if (ctx.Request.Path.Equals("/dev/token") && ctx.Request.Method == "POST")
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Audience = audience,
                Expires = DateTime.UtcNow.AddHours(8),
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "demo-user"),
                    new Claim(ClaimTypes.Name, "Demo User"),
                    new Claim("role", "user")
                }),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            });

            var jwt = handler.WriteToken(token);

            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync($"{{ \"access_token\": \"{jwt}\" }}");
        }
        else
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
        }
    });
});


// Must be last – Ocelot proxies everything else
await app.UseOcelot();
app.Run();
