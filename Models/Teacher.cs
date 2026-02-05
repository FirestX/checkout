using LinqToDB.Mapping;

namespace CheckOut.Models;

[Table("Teachers")]
public class Teacher
{
	[PrimaryKey, Identity]
	public Guid Id { get; set; }
	[Column, NotNull]
	public required string GoogleId { get; set; }
	[Column, NotNull]
	public required string Email { get; set; }
	[Column, NotNull]
	public required string FullName { get; set; }

	[Column, NotNull]
	public Guid DeviceId { get; set; }
	[Association(ThisKey = nameof(DeviceId), OtherKey = nameof(Device.Id))]
	public required Device Device { get; set; }

	[Column, NotNull]
	public DateTime UpdatedAt { get; set; }
	[Column, NotNull]
	public DateTime CreatedAt { get; set; }
}