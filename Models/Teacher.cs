using LinqToDB.Mapping;

namespace CheckOut.Models;

[Table("Teachers")]
public class Teacher
{
	[PrimaryKey, Identity]
	public int Id { get; set; }
	[Column, NotNull]
	public string GoogleId { get; set; } = null!;
	[Column, NotNull]
	public string Email { get; set; } = null!;
	[Column, NotNull]
	public string FullName { get; set; } = null!;

	[Association(ThisKey = nameof(Id), OtherKey = nameof(Device.TeacherId))]
	public Device? Device { get; set; }

	[Column, NotNull]
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	[Column, NotNull]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}