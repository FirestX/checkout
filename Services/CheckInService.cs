using CheckOut.Data;
using CheckOut.Models;
using LinqToDB;

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
}