using LinqToDB.Mapping;

namespace CheckOut.Models;

[Table("CheckIns")]
public class CheckIn
{
	[PrimaryKey, Identity]
    public Guid Id { get; set; }
	[Column, NotNull]
    public Guid TeacherId { get; set; }
	[Column, NotNull]
    public Guid DeviceId { get; set; }

	[Association(ThisKey = nameof(TeacherId), OtherKey = nameof(Teacher.Id))]
	public required Teacher Teacher { get; set; }
	[Association(ThisKey = nameof(DeviceId), OtherKey = nameof(Device.Id))]
	public required Device Device { get; set; }
    
	[Column, NotNull]
    public DateTime CheckInTime { get; set; }
	[Column, NotNull]
    public DateTime CreatedAt { get; set; }
}