using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CheckOut.Services;

public class JwtService
{
	private readonly string _issuer;
	private readonly string _audience;
	private readonly string _secretKey;
	private readonly int _expirationHours;

	public JwtService(IConfiguration configuration)
	{
		var jwtSection = configuration.GetSection("Authentication:Jwt");
		
		_issuer = jwtSection.GetValue<string>("Issuer") 
			?? throw new ArgumentNullException("Jwt:Issuer is not configured");
		_audience = jwtSection.GetValue<string>("Audience") 
			?? throw new ArgumentNullException("Jwt:Audience is not configured");
		_secretKey = jwtSection.GetValue<string>("SecretKey") 
			?? throw new ArgumentNullException("Jwt:SecretKey is not configured");
		_expirationHours = jwtSection.GetValue<int>("ExpirationHours");
		
		if (_expirationHours <= 0)
			_expirationHours = 10; // Default to 10 hours
		
		if (_secretKey.Length < 32)
			throw new ArgumentException("Jwt:SecretKey must be at least 32 characters long");
	}

	public string GenerateToken(int teacherId, string googleId, string email)
	{
		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, googleId),
			new Claim(JwtRegisteredClaimNames.Email, email),
			new Claim("TeacherId", teacherId.ToString()),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
		};

		var token = new JwtSecurityToken(
			issuer: _issuer,
			audience: _audience,
			claims: claims,
			expires: DateTime.UtcNow.AddHours(_expirationHours),
			signingCredentials: credentials
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	public DateTime GetExpirationTime()
	{
		return DateTime.UtcNow.AddHours(_expirationHours);
	}

	public TokenValidationParameters GetTokenValidationParameters()
	{
		return new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = _issuer,
			ValidAudience = _audience,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
			ClockSkew = TimeSpan.Zero // No tolerance for token expiration
		};
	}
}
