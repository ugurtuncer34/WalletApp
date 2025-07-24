namespace WalletBackend.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string TokenHash { get; set; } = null!; // store SHA-256 hash, not plaintext
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}