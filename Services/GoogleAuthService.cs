using static Google.Apis.Auth.GoogleJsonWebSignature;

namespace CheckOut.Services;

public class GoogleAuthService
{
	private readonly string _clientId;
	private readonly string _allowedDomain;

	public GoogleAuthService(IConfiguration configuration)
	{
		var googleSection = configuration.GetSection("Authentication:Google");

		_clientId = googleSection.GetValue<string>("ClientId")
			?? throw new ArgumentNullException("Google:ClientId is not configured");
		_allowedDomain = configuration.GetSection("Authentication").GetValue<string>("AllowedDomain") 
			?? throw new ArgumentNullException("Google:AllowedDomain is not configured");
	}

	public async Task<Payload> GetPayloadFromGoogleToken(string idToken)
	{
		var settings = new ValidationSettings
		{
			Audience = [_clientId]
		};

		var payload = await ValidateAsync(idToken, settings);

		if (string.IsNullOrEmpty(payload.HostedDomain))
			throw new InvalidOperationException("Hosted domain not found in Google token");

		if (!IsAllowedEmailDomain(payload.HostedDomain))
			throw new InvalidOperationException($"Only @{_allowedDomain} accounts are allowed");

		return payload;
	}

	private bool IsAllowedEmailDomain(string hostedDomain)
	{
		return hostedDomain.Equals(_allowedDomain, StringComparison.OrdinalIgnoreCase);
	}
}