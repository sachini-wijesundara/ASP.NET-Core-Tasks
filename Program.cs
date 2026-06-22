using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ASP.NET_Core_Tasks.Data;
using ASP.NET_Core_Tasks.Models;
using ASP.NET_Core_Tasks.Helpers;
using Task = ASP.NET_Core_Tasks.Models.Task;

var builder = WebApplication.CreateBuilder(args);

// Register DB Context with SQLite
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyForJWTAuthSuperSecretKeyForJWTAuth";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TaskTrackerAPI",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TaskTrackerAPI",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// Configure JSON options to serialize/deserialize Enums as Strings in Minimal APIs
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();

// Configure Swagger UI to allow inputting Bearer Token
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Helper method to retrieve user ID from ClaimsPrincipal
bool TryGetUserId(ClaimsPrincipal principal, out int userId)
{
    userId = 0;
    var claimValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return claimValue != null && int.TryParse(claimValue, out userId);
}

// Helper validation function
bool TryValidateTask(Task task, out string? errorMessage)
{
    var context = new ValidationContext(task);
    var results = new List<ValidationResult>();
    bool isValid = Validator.TryValidateObject(task, context, results, true);
    errorMessage = isValid ? null : results.FirstOrDefault()?.ErrorMessage;
    return isValid;
}

// A. POST /register -> register new user account
app.MapPost("/register", async (RegisterDto dto, TaskDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
    {
        return Results.BadRequest(new { error = "Username and password are required." });
    }

    var existingUser = await db.Users.AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower());
    if (existingUser)
    {
        return Results.BadRequest(new { error = "Username is already taken." });
    }

    var user = new User
    {
        Username = dto.Username,
        PasswordHash = PasswordHasher.HashPassword(dto.Password)
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "User registered successfully." });
});

// B. POST /login -> authenticates user and returns JWT token
app.MapPost("/login", async (LoginDto dto, TaskDbContext db, IConfiguration config) =>
{
    if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
    {
        return Results.BadRequest(new { error = "Username and password are required." });
    }

    var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == dto.Username.ToLower());
    if (user == null || !PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash))
    {
        return Results.BadRequest(new { error = "Invalid username or password." });
    }

    var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        }),
        Expires = DateTime.UtcNow.AddDays(7),
        Issuer = config["Jwt:Issuer"] ?? "TaskTrackerAPI",
        Audience = config["Jwt:Audience"] ?? "TaskTrackerAPI",
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    var tokenString = tokenHandler.WriteToken(token);

    return Results.Ok(new { token = tokenString });
});

// 1. GET /tasks -> returns tasks of the authenticated user with optional filtering and pagination
app.MapGet("/tasks", async (ClaimsPrincipal principal, bool? completed, Priority? priority, int? page, int? pageSize, TaskDbContext db) =>
{
    if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();

    var query = db.Tasks.Where(t => t.UserId == userId);

    if (completed.HasValue)
    {
        query = query.Where(t => t.IsCompleted == completed.Value);
    }

    if (priority.HasValue)
    {
        query = query.Where(t => t.Priority == priority.Value);
    }

    // Apply pagination if parameters are provided
    if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
    {
        query = query.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
    }

    return Results.Ok(await query.ToListAsync());
})
.RequireAuthorization();

// 2. GET /tasks/{id} -> returns one task of the authenticated user
app.MapGet("/tasks/{id:int}", async (int id, ClaimsPrincipal principal, TaskDbContext db) =>
{
    if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();

    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    return task is not null 
        ? Results.Ok(task) 
        : Results.NotFound(new { error = "Task not found" });
})
.RequireAuthorization();

// 3. POST /tasks -> creates a task associated with the authenticated user
app.MapPost("/tasks", async (Task task, ClaimsPrincipal principal, TaskDbContext db) =>
{
    if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();

    // Force ID to 0 to trigger database auto-generation and bind to current user
    task.Id = 0;
    task.UserId = userId;

    if (!TryValidateTask(task, out var errorMessage))
    {
        return Results.BadRequest(new { error = errorMessage });
    }

    db.Tasks.Add(task);
    await db.SaveChangesAsync();

    return Results.Created($"/tasks/{task.Id}", task);
})
.RequireAuthorization();

// 4. PUT /tasks/{id} -> updates an existing task of the authenticated user
app.MapPut("/tasks/{id:int}", async (int id, Task task, ClaimsPrincipal principal, TaskDbContext db) =>
{
    if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();

    var existingTask = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    if (existingTask is null)
    {
        return Results.NotFound(new { error = "Task not found" });
    }

    // Force incoming task ID and UserId to match
    task.Id = id;
    task.UserId = userId;

    if (!TryValidateTask(task, out var errorMessage))
    {
        return Results.BadRequest(new { error = errorMessage });
    }

    // Update fields
    existingTask.Title = task.Title;
    existingTask.Description = task.Description;
    existingTask.IsCompleted = task.IsCompleted;
    existingTask.DueDate = task.DueDate;
    existingTask.Priority = task.Priority;

    await db.SaveChangesAsync();

    return Results.Ok(existingTask);
})
.RequireAuthorization();

// 5. DELETE /tasks/{id} -> deletes a task of the authenticated user
app.MapDelete("/tasks/{id:int}", async (int id, ClaimsPrincipal principal, TaskDbContext db) =>
{
    if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();

    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    if (task is null)
    {
        return Results.NotFound(new { error = "Task not found" });
    }

    db.Tasks.Remove(task);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization();

app.Run();

// User Auth DTOs
record RegisterDto(string Username, string Password);
record LoginDto(string Username, string Password);
