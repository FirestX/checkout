using CheckOut.Data;
using CheckOut.Models;
using CheckOut.Models.Dtos;
using LinqToDB;
using LinqToDB.Async;

namespace CheckOut.Services;

public class CheckInService(AppDataContext context)
{
	public async Task CheckInAsync(int teacherId, int deviceId)
	{
		var checkIn = new CheckIn
		{
			TeacherId = teacherId,
			DeviceId = deviceId
		};
		await context.InsertAsync(checkIn);
	}

	public async Task<List<CheckInDto>> GetCheckInsWithDetailsAsync()
	{
		return await context.CheckIns
			.LoadWith(c => c.Device)
			.LoadWith(c => c.Teacher)
			.Select(c => new CheckInDto
			{
				Id = c.Id,
				Teacher = new TeacherDto
				{
					GoogleId = c.Teacher.GoogleId,
					Email = c.Teacher.Email,
					FullName = c.Teacher.FullName
				},
				Device = new DeviceDto
				{
					Id = c.Device.Id,
					Fingerprint = c.Device.Fingerprint,
					DeviceStatus = c.Device.DeviceStatus,
					LastSeen = c.Device.LastSeen
				},
				CheckInTime = c.CheckInTime
			})
			.ToListAsync();
	}

	public async Task<List<CheckInDto>> GetCheckInsWithDetailsAsync(string deviceStatus)
	{
		return await context.CheckIns
			.LoadWith(c => c.Device)
			.LoadWith(c => c.Teacher)
			.Select(c => new CheckInDto
			{
				Id = c.Id,
				Teacher = new TeacherDto
				{
					GoogleId = c.Teacher.GoogleId,
					Email = c.Teacher.Email,
					FullName = c.Teacher.FullName
				},
				Device = new DeviceDto
				{
					Id = c.Device.Id,
					Fingerprint = c.Device.Fingerprint,
					DeviceStatus = c.Device.DeviceStatus,
					LastSeen = c.Device.LastSeen
				},
				CheckInTime = c.CheckInTime
			})
			.Where(c => c.Device.DeviceStatus == deviceStatus)
			.ToListAsync();
	}
}