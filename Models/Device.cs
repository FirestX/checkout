using LinqToDB.Mapping;

namespace CheckOut.Models;

[Table("Devices")]
public class Device
{
	[PrimaryKey, Identity]
	public Guid Id { get; set; }
	[Column, NotNull]
	public required string Fingerprint { get; set; }
	[Column, NotNull]
	public required string DeviceStatus { get; set; }

	[Column, NotNull]
	public DateTime LastSeen { get; set; }
	[Column, NotNull]
	public DateTime UpdatedAt { get; set; }
	[Column, NotNull]
	public DateTime CreatedAt { get; set; }
}