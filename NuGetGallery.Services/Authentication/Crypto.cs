using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;

namespace NuGetGallery.Infrastructure.Authentication;

public static class Crypto
{
    private const int Pbkdf2IterCount = 1000; // پیش‌فرض ASP.NET CryptoHelper
    private const int Pbkdf2SubkeyLength = 256 / 8; // 32 bytes
    private const int SaltSize = 128 / 8; // 16 bytes

    /// <summary>
    /// بازنویسی متد Crypto.VerifyHashedPassword از System.Web.Helpers برای سازگاری کامل با هش‌های قدیمی Pbkdf2
    /// فرمت بایت‌ها: 0x00 | salt (16 bytes) | subkey (32 bytes)
    /// </summary>
    public static bool VerifyHashedPassword(string hashedPassword, string password)
    {
        if (hashedPassword == null)
        {
            return false;
        }
        if (password == null)
        {
            throw new ArgumentNullException(nameof(password));
        }

        byte[] hashedPasswordBytes;
        try
        {
            hashedPasswordBytes = Convert.FromBase64String(hashedPassword);
        }
        catch (FormatException)
        {
            return false;
        }

        // بررسی طول و بایت هدر (0x00 نشان‌دهنده ورژن 0 هش در ASP.NET است)
        if (hashedPasswordBytes.Length != (1 + SaltSize + Pbkdf2SubkeyLength) || hashedPasswordBytes[0] != 0x00)
        {
            return false;
        }

        byte[] salt = new byte[SaltSize];
        Buffer.BlockCopy(hashedPasswordBytes, 1, salt, 0, SaltSize);

        byte[] expectedSubkey = new byte[Pbkdf2SubkeyLength];
        Buffer.BlockCopy(hashedPasswordBytes, 1 + SaltSize, expectedSubkey, 0, Pbkdf2SubkeyLength);

        byte[] actualSubkey;
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Pbkdf2IterCount, HashAlgorithmName.SHA1))
        {
            actualSubkey = pbkdf2.GetBytes(Pbkdf2SubkeyLength);
        }

        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }

    public static string GenerateToken()
    {
        byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);
        return WebEncoders.Base64UrlEncode(tokenBytes);
    }
}
