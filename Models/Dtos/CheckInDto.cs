namespace CheckOut.Models.Dtos;

public class CheckInDto
{
	public int Id { get; set; }
	public TeacherDto Teacher { get; set; } = null!;
	public DeviceDto Device { get; set; } = null!;
	public DateTime CheckInTime { get; set; }
}