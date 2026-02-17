using Google.Apis.Auth;

namespace CheckOut.Services;

public class GoogleAuthService
{
	private readonly string _clientId;
	private readonly string? _allowedDomain;

	public GoogleAuthService(IConfiguration configuration)
	{
		var googleSection = configuration.GetSection("Authentication:Google");
		
		_clientId = googleSection.GetValue<string>("ClientId") 
			?? throw new ArgumentNullException("Google:ClientId is not configured");
		_allowedDomain = googleSection.GetValue<string>("AllowedDomain");
	}

	/// <summary>
	/// Verifies the Google ID token and returns the payload if valid.
	/// </summary>
	/// <param name="idToken">The Google ID token from the frontend</param>
	/// <returns>The validated Google payload containing user information</returns>
	/// <exception cref="InvalidOperationException">Thrown when token validation fails</exception>
	public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(string idToken)
	{
		try
		{
			var settings = new GoogleJsonWebSignature.ValidationSettings
			{
				Audience = new[] { _clientId }
			};

			var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

			// Validate domain if configured
			if (!string.IsNullOrEmpty(_allowedDomain))
			{
				if (string.IsNullOrEmpty(payload.Email))
				{
					throw new InvalidOperationException("Email not found in Google token");
				}

				if (!payload.Email.EndsWith($"@{_allowedDomain}", StringComparison.OrdinalIgnoreCase))
				{
					throw new InvalidOperationException($"Only @{_allowedDomain} accounts are allowed");
				}
			}

			return payload;
		}
		catch (InvalidJwtException ex)
		{
			throw new InvalidOperationException($"Invalid Google token: {ex.Message}", ex);
		}
	}
}
