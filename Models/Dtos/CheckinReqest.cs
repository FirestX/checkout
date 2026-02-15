namespace CheckOut.Models.Dtos;

public class CheckinReqest
{
	public TeacherDto Teacher { get; set; } = null!;
	public DeviceDto Device { get; set; } = null!;
}