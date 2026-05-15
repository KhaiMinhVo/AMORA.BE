using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Amora.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("dev-token")]
    public ActionResult<object> CreateDevToken([FromBody] DevTokenRequest request)
    {
        try
        {
            if (request is null)
            {
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if (request.UserId == Guid.Empty)
            {
                return BadRequest(new { success = false, message = "UserId is required." });
            }

            var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? "Dev User" : request.DisplayName;
            var jwtKey = _configuration["Jwt:Key"] ?? "dev-only-secret-key-change-me-please-use-a-longer-256-bit-key";
            var issuer = _configuration["Jwt:Issuer"] ?? "Amora";
            var audience = _configuration["Jwt:Audience"] ?? "Amora";
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new("id", request.UserId.ToString()),
                new(ClaimTypes.NameIdentifier, request.UserId.ToString()),
                new(ClaimTypes.Name, displayName),
                new("role", "User")
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                success = true,
                data = new
                {
                    accessToken = tokenString,
                    tokenType = "Bearer",
                    expiresAt = token.ValidTo
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = ex.Message
            });
        }
    }
}

public sealed class DevTokenRequest
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = "Dev User";
}