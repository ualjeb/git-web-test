//for program.cs

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization options
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Ensures camelCase in JSON format
    WriteIndented = true // Pretty-print JSON output
};

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Use custom exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// In-memory list to store users
var users = new List<User>();

// GET: Retrieve all users
app.MapGet("/users", () => Results.Json(users.ToList(), jsonOptions));

// GET: Retrieve a user by ID
app.MapGet("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user != null ? Results.Json(user, jsonOptions) : Results.NotFound($"User with ID {id} not found.");
});

// POST: Create a new user ---this could also be done by user.Id = users.Count + 1; in place of Id=users.Any()?
app.MapPost("/users", (UpdateUser newUser) =>
{
    try
    {
        // for extra fields
        if (newUser.ExtraFields != null && newUser.ExtraFields.Any())
            return Results.BadRequest("Please enter only Name and Email in JSON format. Extra fields are not allowed.");
        // Validate input fields
        if (string.IsNullOrWhiteSpace(newUser.Name) || !User.IsValidEmail(newUser.Email))
            return Results.BadRequest("Invalid user data. Name must be provided and Email must be valid.");

        var user = new User
        {
            Id = users.Any() ? users.Max(u => u.Id) + 1 : 0,
            Name = newUser.Name,
            Email = newUser.Email
        };

        users.Add(user);
        return Results.Json(user, jsonOptions);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Unexpected error: {ex.Message}");
    }
}).WithOpenApi();

// PUT: Update an existing user
app.MapPut("/users/{id}", (int id, UpdateUser updatedUser) =>
{
    try
    {
        var user = users.FirstOrDefault(u => u.Id == id);
        if (user == null) return Results.NotFound($"User with ID {id} not found.");

        // Reject requests with extra fields
        if (updatedUser.ExtraFields != null && updatedUser.ExtraFields.Any())
            return Results.BadRequest("Please enter only Name and Email in JSON format. Extra fields are not allowed.");
        // Validate input fields
        if (string.IsNullOrWhiteSpace(updatedUser.Name) || !User.IsValidEmail(updatedUser.Email))
            return Results.BadRequest("Invalid user data. Ensure Name is not empty and Email is in correct format.");

        user.Name = updatedUser.Name;
        user.Email = updatedUser.Email;

        return Results.Json(user, jsonOptions);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Unexpected error: {ex.Message}");
    }
}).WithOpenApi();

// DELETE: Remove a user by ID
app.MapDelete("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user == null) return Results.NotFound($"User with ID {id} not found.");

    users.Remove(user);
    return Results.NoContent();
});

app.Run();

// Exception Handling Middleware
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred. Please try again later. Please ensure you have entered a valid JSON with name and email fields only." });
        }
    }
}

// Define User Model
public class User
{
    public int Id { get; set; }

    [JsonPropertyName("name")] // Ensures camelCase in JSON output
    public required string Name { get; set; }

    [JsonPropertyName("email")]
    public required string Email { get; set; }

    // Email validation method
    public static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}

public class UpdateUser
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("email")]
    public required string Email { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtraFields { get; set; }
}