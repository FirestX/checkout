namespace CheckOut.Models.Dtos;

public class DeviceDto
{
	public int Id { get; set; }
	public string Fingerprint { get; set; } = null!;
	public string DeviceStatus { get; set; } = null!;
	public DateTime LastSeen { get; set; }
}