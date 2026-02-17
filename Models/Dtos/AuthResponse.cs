namespace CheckOut.Models.Dtos;

public class AuthResponse
{
	/// <summary>
	/// JWT token for authenticating subsequent requests
	/// </summary>
	public required string Token { get; set; }
	
	/// <summary>
	/// When the token expires
	/// </summary>
	public required DateTime ExpiresAt { get; set; }
	
	/// <summary>
	/// Information about the authenticated teacher
	/// </summary>
	public required AuthUserInfo User { get; set; }
	
	/// <summary>
	/// Status of the device used for authentication (Pending, Approved, Blocked)
	/// </summary>
	public required string DeviceStatus { get; set; }
}

public class AuthUserInfo
{
	public int Id { get; set; }
	public required string GoogleId { get; set; }
	public required string Email { get; set; }
	public required string FullName { get; set; }
}
