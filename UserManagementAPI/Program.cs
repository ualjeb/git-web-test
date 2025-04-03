using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// In-memory list to store users
var users = new List<User>();


// GET: Retrieve all users
app.MapGet("/users", () => Results.Ok(users));

// GET: Retrieve a user by ID
app.MapGet("/users/{id}", (int id) => 
{
    var user = users.Find(u => u.Id == id);
    return user != null ? Results.Ok(user) : Results.NotFound();
});

// POST: Create a new user user.Id = users.Count + 1;
app.MapPost("/users", (User user) =>
{
    if (string.IsNullOrWhiteSpace(user.Name) || string.IsNullOrWhiteSpace(user.Email))
        return Results.BadRequest("Invalid user data.");

    user.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
    users.Add(user);

    return Results.Created($"/users/{user.Id}", user);
}).WithOpenApi();

// PUT: Update an existing user
app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
    var user = users.Find(u => u.Id == id);
    if (user == null) return Results.NotFound();

    user.Name = updatedUser.Name ?? user.Name;  //  Prevents null overwrite
    user.Email = updatedUser.Email ?? user.Email;

    return Results.NoContent();
});

// DELETE: Remove a user by ID
app.MapDelete("/users/{id}", (int id) =>
{
    var user = users.Find(u => u.Id == id);
    if (user == null) return Results.NotFound();

    users.Remove(user);
    return Results.NoContent();
});

app.Run();

// Define User Model
public class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
}