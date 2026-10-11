using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CinemaManagement.API.Services
{
    public class JwtTokenService
    {
        private readonly IConfiguration _config;

        public JwtTokenService(IConfiguration config)
        {
            _config = config;
        }

        public string CreateToken(int userId, string username, string role, int? memberId)
        {
            var key = _config["Jwt:Key"]
                      ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Key trong appsettings.json");

            var claims = new List<Claim>
            {
                new("userId", userId.ToString()),
                new("username", username),
                new("role", role)
            };

            if (memberId.HasValue)
                claims.Add(new Claim("memberId", memberId.Value.ToString()));

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var expireHours = _config.GetValue("Jwt:ExpireHours", 8);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expireHours),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}