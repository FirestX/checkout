using CheckOut.Data;
using CheckOut.Models;
using LinqToDB;
using LinqToDB.Async;

namespace CheckOut.Services;

public class DeviceService(AppDataContext context)
{
	public async Task<Device?> GetDeviceAsync(int deviceId)
	{
		return await context.Devices.FirstOrDefaultAsync(d => d.Id == deviceId);
	}
	
	public async Task<Device?> GetDeviceAsync(string deviceFingetprint)
	{
		return await context.Devices.FirstOrDefaultAsync(d => d.Fingerprint == deviceFingetprint);
	}

	public async Task<int> CreateDeviceAsync(int teacherId, string deviceFingerprint)
	{
		var device = new Device
		{
			TeacherId = teacherId,
			Fingerprint = deviceFingerprint,
			DeviceStatus = DeviceStatus.Pending
		};
		return await context.InsertWithInt32IdentityAsync(device);
	}

	public async Task SeenDeviceAsync(Device device)
	{
		device.LastSeen = DateTime.UtcNow;
		await context.UpdateAsync(device);
	}
	
	public async Task ApproveDeviceAsync(Device device)
	{
		device.DeviceStatus = DeviceStatus.Approved;
		await context.UpdateAsync(device);
	}
	
	public async Task BlockDeviceAsync(Device device)
	{
		device.DeviceStatus = DeviceStatus.Blocked;
		await context.UpdateAsync(device);
	}
}