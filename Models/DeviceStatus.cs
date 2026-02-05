namespace CheckOut.Models;

public static class DeviceStatus
{
	public const string Pending = "Pending";
	public const string Approved = "Approved";
	public const string Blocked = "Blocked";

	private static readonly HashSet<string> AllStatuses = 
	[
		Pending,
		Approved,
		Blocked
	];

	public static bool IsValidStatus(string status)
	{
		return AllStatuses.Contains(status);
	}
}