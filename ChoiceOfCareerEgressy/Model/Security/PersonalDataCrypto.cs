using System.Security.Cryptography;
using System.Text;

namespace ChoiceOfCareerEgressy.Model.Security;

/// <summary>
/// Személyes adatok alkalmazásszintű titkosítása.
/// A két, egymástól független 32 bájtos kulcsot az alkalmazáson kívül kell tárolni.
/// </summary>
public sealed class PersonalDataCrypto
{
    private readonly byte[] _encryptionKey;
    private readonly byte[] _lookupKey;

    public PersonalDataCrypto(IConfiguration configuration)
    {
        _encryptionKey = ReadKey(configuration["PersonalData:EncryptionKey"], "PersonalData:EncryptionKey");
        _lookupKey = ReadKey(configuration["PersonalData:LookupKey"], "PersonalData:LookupKey");
        if (CryptographicOperations.FixedTimeEquals(_encryptionKey, _lookupKey))
            throw new InvalidOperationException("A titkosítási és a keresési kulcsnak különböznie kell.");
    }

    private static byte[] ReadKey(string? value, string setting)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Hiányzó titkosítási beállítás: {setting}");
        byte[] key;
        try { key = Convert.FromBase64String(value); }
        catch (FormatException ex) { throw new InvalidOperationException($"Érvénytelen Base64 kulcs: {setting}", ex); }
        if (key.Length != 32)
            throw new InvalidOperationException($"A {setting} kulcsnak 32 bájtosnak kell lennie.");
        return key;
    }

    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var clear = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[clear.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(_encryptionKey, 16))
            aes.Encrypt(nonce, clear, ciphertext, tag);
        var packed = new byte[1 + nonce.Length + tag.Length + ciphertext.Length];
        packed[0] = 1; // verzió
        Buffer.BlockCopy(nonce, 0, packed, 1, nonce.Length);
        Buffer.BlockCopy(tag, 0, packed, 13, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, packed, 29, ciphertext.Length);
        return Convert.ToBase64String(packed);
    }

    public string Decrypt(string encrypted)
    {
        ArgumentNullException.ThrowIfNull(encrypted);
        var packed = Convert.FromBase64String(encrypted);
        if (packed.Length < 29 || packed[0] != 1)
            throw new CryptographicException("Érvénytelen titkosított adat vagy verzió.");
        var clear = new byte[packed.Length - 29];
        using (var aes = new AesGcm(_encryptionKey, 16))
            aes.Decrypt(packed.AsSpan(1, 12), packed.AsSpan(29), packed.AsSpan(13, 16), clear);
        return Encoding.UTF8.GetString(clear);
    }

    /// <summary>Csak pontos egyezés keresésére. A HMAC kulcsa különbözik az AES kulcstól.</summary>
    public string EmailLookup(string email) => Lookup("email:", email.Trim().ToLowerInvariant());

    public string OmLookup(string om) => Lookup("om:", om.Trim());

    private string Lookup(string domain, string normalizedValue)
    {
        using var hmac = new HMACSHA256(_lookupKey);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(domain + normalizedValue)));
    }
}
