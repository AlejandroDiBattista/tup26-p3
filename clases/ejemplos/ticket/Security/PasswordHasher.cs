using System.Security.Cryptography;

namespace Agenda.Security;

public static class PasswordHasher {
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public static (string Hash, string Salt) Hashear(string password) {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Pbkdf2(password, salt);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool Verificar(string password, string hashGuardado, string saltGuardado) {
        byte[] salt = Convert.FromBase64String(saltGuardado);
        byte[] hash = Pbkdf2(password, salt);
        byte[] esperado = Convert.FromBase64String(hashGuardado);

        return CryptographicOperations.FixedTimeEquals(hash, esperado);
    }

    private static byte[] Pbkdf2(string password, byte[] salt) {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
    }
}