using LinqToDB.Mapping;

namespace CheckOut.Models;

[Table("CheckIns")]
public class CheckIn
{
	[PrimaryKey, Identity]
    public int Id { get; set; }
	[Column, NotNull]
	public int TeacherId { get; set; }
	[Column, NotNull]
	public int DeviceId { get; set; }

	[Association(ThisKey = nameof(TeacherId), OtherKey = nameof(Teacher.Id)), NotNull]
	public Teacher Teacher { get; set; } = null!;
	[Association(ThisKey = nameof(DeviceId), OtherKey = nameof(Device.TeacherId)), NotNull]
	public Device Device { get; set; } = null!;

	[Column, NotNull]
	public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
}