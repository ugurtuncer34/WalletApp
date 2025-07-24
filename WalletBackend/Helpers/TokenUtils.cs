using System.Security.Cryptography;
using System.Text;

namespace WalletBackend.Helpers;

public static class TokenUtils
{
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);  // e.g. "A413F9..."
    }
}