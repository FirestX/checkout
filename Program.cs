using CheckOut.Data;
using CheckOut.Models;
using CheckOut.Models.Dtos;
using CheckOut.Services;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// Register AppDataContext with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var dataOptions = new DataOptions()
	.UseSQLite(connectionString);

builder.Services.AddScoped(_ => new AppDataContext(dataOptions));

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

	// Initialize database tables
	AppDataContext.InitializeDatabase(dataOptions, connectionString);
}

if (!app.Environment.IsDevelopment())
{
	app.UseHttpsRedirection();
}

// Middleware order is important
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// =============================================================================
// Authentication Endpoints
// =============================================================================

app.MapPost("/api/auth/google", async (
	[FromBody] string idToken,
	AppDataContext db,
	GoogleAuthService googleAuth,
	JwtService jwt) =>
{
	try
	{
		// 1. Verify Google token
		var payload = await googleAuth.VerifyGoogleTokenAsync(idToken);

		// 2. Find or create teacher
		var teacher = await db.Teachers
			.FirstOrDefaultAsync(t => t.GoogleId == payload.Subject);

		if (teacher == null)
		{
			teacher = new Teacher
			{
				GoogleId = payload.Subject,
				Email = payload.Email,
				FullName = payload.Name ?? payload.Email
			};
			teacher.Id = await db.InsertWithInt32IdentityAsync(teacher);
		}
		else
		{
			// Update teacher info if changed
			if (teacher.Email != payload.Email || teacher.FullName != payload.Name)
			{
				teacher.Email = payload.Email;
				teacher.FullName = payload.Name ?? payload.Email;
				await db.UpdateAsync(teacher);
			}
		}

		// 4. Generate JWT
		var token = jwt.GenerateToken(teacher.Id, teacher.GoogleId, teacher.Email);

		// 5. Return auth response
		return Results.Ok(new AuthResponse
		{
			Token = token,
			ExpiresAt = jwt.GetExpirationTime(),
			User = new AuthUserInfo
			{
				Id = teacher.Id,
				GoogleId = teacher.GoogleId,
				Email = teacher.Email,
				FullName = teacher.FullName
			},
			DeviceStatus = deviceStatus
		});
	}
	catch (InvalidOperationException ex)
	{
		return Results.Problem(ex.Message, statusCode: StatusCodes.Status401Unauthorized);
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Authentication error: {ex.Message}");
		return Results.Problem("An error occurred during authentication");
	}
})
.WithName("GoogleAuth");

app.MapGet("/api/auth/me", async (HttpContext context, AppDataContext db) =>
{
	var teacherIdClaim = context.User.FindFirst("TeacherId")?.Value;
	if (teacherIdClaim == null || !int.TryParse(teacherIdClaim, out var teacherId))
	{
		return Results.Unauthorized();
	}

	var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.Id == teacherId);
	if (teacher == null)
	{
		return Results.NotFound("Teacher not found");
	}

	return Results.Ok(new AuthUserInfo
	{
		Id = teacher.Id,
		GoogleId = teacher.GoogleId,
		Email = teacher.Email,
		FullName = teacher.FullName
	});
})
.RequireAuthorization()
.WithName("GetCurrentUser");

// =============================================================================
// Check-in Endpoints
// =============================================================================

app.MapPost("/api/check-ins", async ([FromBody] CheckinReqest reqest, AppDataContext db) =>
{
	var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.Email == reqest.Teacher.Email);
	var device = await db.Devices.FirstOrDefaultAsync(d => d.Fingerprint == reqest.Device.Fingerprint);

	if (teacher is not null && device is not null)
	{
		var checkIn = new CheckIn
		{
			TeacherId = teacher.Id,
			DeviceId = device.Id,
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
});

// =============================================================================
// Device Management Endpoints
// =============================================================================

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
});

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
});

app.Run();