using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DevBlog.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DevBlog.Api.Endpoints;

public static class AuthEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/auth/login", async (LoginRequest req, AppDbContext db) =>
        {
            // TODO: proper hashing needed
            var hash = Convert.ToBase64String(Encoding.UTF8.GetBytes(req.Password));
            var user = await db.Users.FirstOrDefaultAsync(u =>
                u.Username == req.Username && u.PasswordHash == hash);

            if (user is null)
                return Results.Unauthorized();

            var jwtSecret = "devblog-super-secret-key-2024-dev"; // TODO: move to config
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return Results.Ok(new { token = tokenString });
        });
    }
}

public record LoginRequest(string Username, string Password);
