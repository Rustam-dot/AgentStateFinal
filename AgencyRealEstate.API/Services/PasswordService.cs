using System.Security.Cryptography;
using System.Text;

namespace AgencyRealEstate.API.Services;

public static class PasswordService
{
    /// <summary>
    /// Создаёт хеш и соль пароля (аналог SQL-процедуры usp_CreateUser)
    /// </summary>
    public static (byte[] hash, byte[] salt) CreatePasswordHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(32);
        var passwordBytes = Encoding.Unicode.GetBytes(password); // UTF-16LE, как в SQL
        var hash = SHA512.HashData(passwordBytes.Concat(salt).ToArray());
        return (hash, salt);
    }

    /// <summary>
    /// Проверяет пароль по хранимым хешу и соли
    /// </summary>
    public static bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
    {
        var passwordBytes = Encoding.Unicode.GetBytes(password);
        var hash = SHA512.HashData(passwordBytes.Concat(storedSalt).ToArray());
        return hash.SequenceEqual(storedHash);
    }
}