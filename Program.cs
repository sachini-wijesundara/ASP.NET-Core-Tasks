using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ASP.NET_Core_Tasks.Data;
using ASP.NET_Core_Tasks.Models;
using Task = ASP.NET_Core_Tasks.Models.Task;

var builder = WebApplication.CreateBuilder(args);

// Register DB Context with SQLite
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JSON options to serialize/deserialize Enums as Strings in Minimal APIs
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 1. GET /tasks -> returns tasks with optional filtering and pagination
app.MapGet("/tasks", async (bool? completed, Priority? priority, int? page, int? pageSize, TaskDbContext db) =>
{
    var query = db.Tasks.AsQueryable();

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

    return await query.ToListAsync();
});

// 2. GET /tasks/{id} -> returns one task; returns HTTP 404 with error body if id doesn't exist
app.MapGet("/tasks/{id:int}", async (int id, TaskDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    return task is not null 
        ? Results.Ok(task) 
        : Results.NotFound(new { error = "Task not found" });
});

// Helper validation function
bool TryValidateTask(Task task, out string? errorMessage)
{
    var context = new ValidationContext(task);
    var results = new List<ValidationResult>();
    bool isValid = Validator.TryValidateObject(task, context, results, true);
    errorMessage = isValid ? null : results.FirstOrDefault()?.ErrorMessage;
    return isValid;
}

// 3. POST /tasks -> creates a task. Returns HTTP 201. Returns HTTP 400 on validation error.
app.MapPost("/tasks", async (Task task, TaskDbContext db) =>
{
    // Force ID to 0 to trigger database auto-generation
    task.Id = 0;

    if (!TryValidateTask(task, out var errorMessage))
    {
        return Results.BadRequest(new { error = errorMessage });
    }

    db.Tasks.Add(task);
    await db.SaveChangesAsync();

    return Results.Created($"/tasks/{task.Id}", task);
});

// 4. PUT /tasks/{id} -> updates an existing task. Returns 200 on success, 404 if not found, 400 on validation error.
app.MapPut("/tasks/{id:int}", async (int id, Task task, TaskDbContext db) =>
{
    var existingTask = await db.Tasks.FindAsync(id);
    if (existingTask is null)
    {
        return Results.NotFound(new { error = "Task not found" });
    }

    // Force incoming task ID to match URL parameter
    task.Id = id;

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
});

// 5. DELETE /tasks/{id} -> deletes a task. Returns 204 on success, 404 if not found.
app.MapDelete("/tasks/{id:int}", async (int id, TaskDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    if (task is null)
    {
        return Results.NotFound(new { error = "Task not found" });
    }

    db.Tasks.Remove(task);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.Run();
