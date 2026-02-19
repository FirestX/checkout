namespace CheckOut.Models.Dtos;

public class TestAuthRequest
{
	public required string GoogleId { get; set; }
	public required string Email { get; set; }
	public required string FullName { get; set; }
}
