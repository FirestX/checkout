namespace CheckOut.Models.Dtos;

public class GoogleAuthRequest
{
	/// <summary>
	/// The Google ID token obtained from Google Sign-In SDK on the frontend
	/// </summary>
	public required string IdToken { get; set; }
}