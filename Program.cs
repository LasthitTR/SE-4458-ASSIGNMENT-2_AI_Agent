using AirbnbClone.Api.Application.Interfaces;
using AirbnbClone.Api.Application.Services;
using AirbnbClone.Api.Infrastructure.Persistence;
using AirbnbClone.Api.Application.Security;
using AirbnbClone.Api.Domain.Entities;
using AirbnbClone.Api.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecretKey = jwtSection["SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey configuration is required.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IListingService, ListingService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = string.Empty;
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AirbnbClone.Api v1");
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await SeedLocalDataAsync(app);

await app.RunAsync();

static async Task SeedLocalDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await dbContext.Database.MigrateAsync();

    var hostId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    var listingId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    var hostExists = await dbContext.Users.AnyAsync(x => x.Id == hostId);
    if (!hostExists)
    {
        dbContext.Users.Add(new User
        {
            Id = hostId,
            FirstName = "Seed",
            LastName = "Host",
            Email = "seed.host@example.com",
            PasswordHash = PasswordHashing.Hash("Host123!"),
            Role = UserRole.Host,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    var listingExists = await dbContext.Listings.AnyAsync(x => x.Id == listingId);
    if (!listingExists)
    {
        dbContext.Listings.Add(new Listing
        {
            Id = listingId,
            HostId = hostId,
            Title = "Paris Central Studio near Louvre",
            Description = "Bright studio in central Paris, walking distance to Louvre and metro.",
            Country = "France",
            City = "Paris",
            AddressLine = "Rue de Rivoli, 75001",
            Capacity = 2,
            Price = 120m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    var guestId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    var guestExists = await dbContext.Users.AnyAsync(x => x.Id == guestId);
    if (!guestExists)
    {
        dbContext.Users.Add(new User
        {
            Id = guestId,
            FirstName = "Seed",
            LastName = "Guest",
            Email = "seed.guest@example.com",
            PasswordHash = PasswordHashing.Hash("Guest123!"),
            Role = UserRole.Guest,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    await dbContext.SaveChangesAsync();
}
