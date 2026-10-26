using System.Security.Cryptography;
using System.Text;

namespace Testify.Api.Services;

public static class TokenHasher
{
    // 256 random bits from the operating system, safe to put in a cookie or a link.
    public static string NewToken()
    {
        var base64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    // Only this hash is stored, so a copy of the database is not enough to use anyone's tokens.
    // A fast hash is fine because the token is random and cannot be guessed. Passwords need bcrypt because people pick guessable ones.
    public static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
