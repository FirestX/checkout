using CheckOut.Data;
using CheckOut.Models;
using CheckOut.Models.Dtos;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// Register AppDataContext with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var dataOptions = new DataOptions()
	.UseSQLite(connectionString);

builder.Services.AddScoped(_ => new AppDataContext(dataOptions));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();

	app.UseSwagger();
	app.UseSwaggerUI();

	AppDataContext.DeleteDatabase(connectionString);

	// Initialize database tables
	AppDataContext.InitializeDatabase(dataOptions, connectionString);
}

if (!app.Environment.IsDevelopment())
{
	app.UseHttpsRedirection();
}

app.MapPost("/api/check-in", async ([FromBody] CheckinReqest reqest, AppDataContext db) =>
{
	var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.Email == reqest.Teacher.Email);
	var device = await db.Devices.FirstOrDefaultAsync(d => d.Fingerprint == reqest.Device.Fingerprint);

	if (teacher is not null && device is not null)
	{
		var checkIn = new CheckIn
		{
			TeacherId = teacher.Id,
			DeviceId = device.TeacherId,
		};
		await db.InsertAsync(checkIn);
		return Results.Ok("Check-in successful.");
	}

	if (teacher is null)
	{
		var newTeacher = new Teacher()
		{
			GoogleId = reqest.Teacher.GoogleId,
			Email = reqest.Teacher.Email,
			FullName = reqest.Teacher.FullName
		};
		var teacherId = await db.InsertWithInt32IdentityAsync(newTeacher);
		var newDevice = new Device()
		{
			TeacherId = teacherId,
			Fingerprint = reqest.Device.Fingerprint,
			DeviceStatus = DeviceStatus.Pending
		};
		var deviceId = await db.InsertWithInt32IdentityAsync(newDevice);
		Console.WriteLine("Require admin approval for new device.");
		return Results.Ok("Check-in successful. Your device is pending approval.");
	}

	if (device is null)
	{
		var newDevice = new Device()
		{
			TeacherId = teacher.Id,
			Fingerprint = reqest.Device.Fingerprint,
			DeviceStatus = DeviceStatus.Pending
		};
		var deviceId = await db.InsertWithInt32IdentityAsync(newDevice);
		Console.WriteLine("Require admin approval for new device.");
		return Results.Ok("Check-in successful. Your device is pending approval.");
	}

	return Results.BadRequest("Check-in failed. Please contact support.");
});

app.MapGet("/api/check-ins", async (AppDataContext db, [FromQuery] string deviceStatus) =>
{
	IEnumerable<CheckIn> checkIns;
	if (string.IsNullOrEmpty(deviceStatus))
	{
		checkIns = await db.CheckIns
			.LoadWith(c => c.Teacher)
			.LoadWith(c => c.Device)
			.ToListAsync();
		return Results.Ok(checkIns);
	}

	if (DeviceStatus.IsValidStatus(deviceStatus))
	{
		checkIns = await db.CheckIns
			.LoadWith(c => c.Teacher)
			.LoadWith(c => c.Device)
			.Where(c => c.Device.DeviceStatus == deviceStatus)
			.ToListAsync();
		return Results.Ok(checkIns);
	}

	return Results.BadRequest("Invalid device status filter.");
});

app.Run();