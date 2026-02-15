using LinqToDB.Mapping;

namespace CheckOut.Models;

[Table("Devices")]
public class Device
{
	[PrimaryKey, Identity]
	public int TeacherId { get; set; }
	[Column, NotNull]
	public required string Fingerprint { get; set; } = null!;
	[Column, NotNull]
	public required string DeviceStatus { get; set; } = null!;

	[Association(ThisKey = nameof(TeacherId), OtherKey = nameof(Teacher.Id)), NotNull]
	public Teacher Teacher { get; set; } = null!;

	[Column, NotNull]
	public DateTime LastSeen { get; set; } = DateTime.UtcNow;
	[Column, NotNull]
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	[Column, NotNull]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}