using System.Security.Claims;
using CheckOut.Data;
using CheckOut.Models;
using CheckOut.Models.Dtos;
using CheckOut.Services;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.Http,
		Scheme = "Bearer",
		BearerFormat = "JWT"
	});

	c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
	{
		[new OpenApiSecuritySchemeReference("Bearer", doc)] = []
	});
});

// Register AppDataContext with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var dataOptions = new DataOptions()
	.UseSQLite(connectionString);

builder.Services.AddScoped(_ => new AppDataContext(dataOptions));
builder.Services.AddScoped<TeacherService>();
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<CheckInService>();

// Register authentication services
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<GoogleAuthService>();

// Configure CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowFrontend", policy =>
	{
		policy.WithOrigins(allowedOrigins)
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

// Configure JWT authentication
var jwtService = new JwtService(builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = jwtService.GetTokenValidationParameters();
	});
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();

	app.UseSwagger();
	app.UseSwaggerUI();

	AppDataContext.DeleteDatabase(connectionString);
	AppDataContext.InitializeDatabase(dataOptions, connectionString);
}

if (!app.Environment.IsDevelopment())
{
	app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// =============================================================================
// Authentication Endpoints
// =============================================================================

app.MapPost("/api/auth/google", async (
	[FromBody] string idToken,
	TeacherService teacherService,
	GoogleAuthService googleAuth,
	JwtService jwt) =>
{
	var payload = await googleAuth.GetPayloadFromGoogleToken(idToken);

	var teacher = await teacherService.GetTeacherAsync(payload.Subject);

	if (teacher is null)
	{
		var teacherId = await teacherService.CreateTeacher(payload);
		teacher = await teacherService.GetTeacherAsync(teacherId)
			?? throw new InvalidOperationException("Failed to create teacher record");
	}
	else
	{
		if (teacherService.IsInfoOutdated(teacher, payload))
		{
			teacher.Email = payload.Email;
			teacher.FullName = payload.Name ?? payload.Email;
			await teacherService.UpdateTeacherAsync(teacher);
		}
	}

	var token = jwt.GenerateToken(teacher.Id, teacher.FullName, teacher.Email);

	return Results.Ok(token);
})
.WithName("GoogleAuth");

app.MapPost("/api/check-ins", async (
	[FromBody] string deviceFingerprint,
	TeacherService teacherService,
	DeviceService deviceService,
	CheckInService checkInService,
	HttpContext httpContext) =>
{
	var teacherIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
	var emailClaim = httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
	var fullNameClaim = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;

	if (string.IsNullOrEmpty(teacherIdClaim) || string.IsNullOrEmpty(emailClaim))
		return Results.Unauthorized();

	if (!int.TryParse(teacherIdClaim, out var teacherId))
		return Results.Unauthorized();

	var teacher = await teacherService.GetTeacherAsync(teacherId);
	if (teacher is null)
		return Results.NotFound("Teacher not found");

	var device = await deviceService.GetDeviceAsync(deviceFingerprint);

	if (device is null)
	{
		var deviceId = await deviceService.CreateDeviceAsync(teacherId, deviceFingerprint);
		return Results.Ok(new
		{
			Message = "Check-in successful. Your device is pending approval.",
			Status = DeviceStatus.Pending,
			RequiresApproval = true
		});
	}

	if (device.TeacherId != teacherId)
	{
		return Results.Problem(
			"This device is registered to a different teacher",
			statusCode: StatusCodes.Status403Forbidden);
	}

	switch (device.DeviceStatus)
	{
		case DeviceStatus.Blocked:
			return Results.Problem(
				"This device has been blocked. Please contact an administrator.",
				statusCode: StatusCodes.Status403Forbidden);

		case DeviceStatus.Pending:
			return Results.Ok(new
			{
				Message = "Your device is pending approval. Check-in recorded but awaiting admin approval.",
				Status = DeviceStatus.Pending,
				RequiresApproval = true
			});

		case DeviceStatus.Approved:
			await checkInService.CheckInAsync(teacherId, device.Id);

			await deviceService.SeenDeviceAsync(device);
			return Results.Ok(new
			{
				Message = "Check-in successful.",
				Status = DeviceStatus.Approved,
			});

		default:
			return Results.Problem(
				"Unknown device status. Please contact support.",
				statusCode: StatusCodes.Status400BadRequest);
	}
})
.RequireAuthorization()
.WithName("CreateCheckIn");

app.MapGet("/api/check-ins", async (AppDataContext db, [FromQuery] string? deviceStatus) =>
{
	// use dtos
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
})
.RequireAuthorization();

app.MapPatch("/api/devices/{deviceId}/approve", async (int deviceId, AppDataContext db) =>
{
	var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == deviceId);
	if (device is null)
	{
		return Results.NotFound("Device not found.");
	}

	device.DeviceStatus = DeviceStatus.Approved;
	await db.UpdateAsync(device);
	return Results.Ok("Device approved successfully.");
})
.RequireAuthorization();

app.MapPatch("/api/devices/{deviceId}/block", async (int deviceId, AppDataContext db) =>
{
	var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == deviceId);
	if (device is null)
	{
		return Results.NotFound("Device not found.");
	}

	device.DeviceStatus = DeviceStatus.Blocked;
	await db.UpdateAsync(device);
	return Results.Ok("Device blocked successfully.");
})
.RequireAuthorization();

app.MapGet("/api/devices", async (AppDataContext db) =>
{
	var devices = await db.Devices
		.LoadWith(d => d.Teacher)
		.ToListAsync();
	return Results.Ok(devices);
})
.RequireAuthorization()
.WithName("GetAllDevices");

app.Run();