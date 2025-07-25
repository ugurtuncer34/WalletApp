namespace WalletBackend.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/auth").AllowAnonymous().WithTags("Auth");
        var jwt = app.Configuration.GetSection("Jwt");
        var keyBytes = Encoding.UTF8.GetBytes(jwt["Key"]!);

        auth.MapPost("/register", async (WalletDbContext db, RegisterDto dto) =>
    {
        if (await db.Users.AnyAsync(u => u.Email == dto.Email))
            return Results.BadRequest("Email already exists");

        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var user = new User { Email = dto.Email, PasswordHash = hash };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Results.Created($"/users/{user.Id}", new { user.Id, user.Email });
    });

        auth.MapPost("/login", async (WalletDbContext db, LoginDto dto) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Results.Unauthorized();

            // Build claims
            var claims = new[]{
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiresMinutes"]!));

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Results.Ok(new AuthResponseDto(tokenString, expires));
        });

        return auth;
    }
}