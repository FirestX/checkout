using CheckOut.Models.Dtos;
using CheckOut.Data;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Register AppDataContext with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddScoped(_ => new AppDataContext(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// app.MapPost("/api/check-in", async ([FromBody] TeacherDto teacher, [FromBody] DeviceDto device) => {
// 		});

app.Run();