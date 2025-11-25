using System.Security.Cryptography;
using System.Text;
using Chat.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Chat.Security.Encryption;

public class AesEncryptionService : IEncryptionService
{
    private readonly Dictionary<string, (byte[] X, byte[] Y)> _peerPublicKeys = new();
    private byte[] _privateKey;
    private (byte[] X, byte[] Y) _publicKey;
    private readonly ILogger<AesEncryptionService> _logger;

    public AesEncryptionService(ILogger<AesEncryptionService> logger)
    {
        _logger = logger;
        GenerateKeyPair();
    }

    public byte[] GetPublicKey()
    {
        var publicKeyBytes = new byte[_publicKey.X.Length + _publicKey.Y.Length];
        Buffer.BlockCopy(_publicKey.X, 0, publicKeyBytes, 0, _publicKey.X.Length);
        Buffer.BlockCopy(_publicKey.Y, 0, publicKeyBytes, _publicKey.X.Length, _publicKey.Y.Length);
        return publicKeyBytes;
    }

    public void SetPeerPublicKey(string peerUsername, byte[] publicKey)
    {
        try
        {
            if (publicKey.Length != 64)
            {
                _logger.LogWarning("Invalid public key length from {PeerUsername}: {Length} bytes (expected 64)",
                    peerUsername, publicKey.Length);
                return;
            }

            var x = new byte[32];
            var y = new byte[32];
            Buffer.BlockCopy(publicKey, 0, x, 0, 32);
            Buffer.BlockCopy(publicKey, 32, y, 0, 32);

            _peerPublicKeys[peerUsername] = (x, y);
            _logger.LogInformation("Public key set for peer: {PeerUsername}. Total peers: {PeerCount}",
                peerUsername, _peerPublicKeys.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set public key for peer: {PeerUsername}", peerUsername);
        }
    }

    public string Encrypt(string plainText)
    {
        try
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            if (!_peerPublicKeys.Any())
            {
                _logger.LogWarning("No peer keys available for encryption, returning plain text");
                return plainText;
            }

            var firstPeerKey = _peerPublicKeys.First();

            using var aes = Aes.Create();
            aes.Key = DeriveSharedSecret(firstPeerKey.Value.X, firstPeerKey.Value.Y);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();

            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs)) sw.Write(plainText);

            var encrypted = ms.ToArray();
            return Convert.ToBase64String(encrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption failed");
            return plainText;
        }
    }

    public string Decrypt(string cipherText)
    {
        try
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            if (!IsLikelyEncrypted(cipherText))
            {
                _logger.LogDebug("Text does not appear to be encrypted, returning as is");
                return cipherText;
            }

            var fullCipher = Convert.FromBase64String(cipherText);

            if (fullCipher.Length <= 16)
            {
                _logger.LogWarning("Cipher text too short, likely not encrypted");
                return cipherText;
            }

            using var aes = Aes.Create();

            var iv = new byte[16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);

            var cipherData = new byte[fullCipher.Length - iv.Length];
            Buffer.BlockCopy(fullCipher, iv.Length, cipherData, 0, cipherData.Length);

            foreach (var (peerUsername, (x, y)) in _peerPublicKeys)
            {
                try
                {
                    aes.Key = DeriveSharedSecret(x, y);
                    aes.IV = iv;

                    using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using var ms = new MemoryStream(cipherData);
                    using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                    using var sr = new StreamReader(cs);

                    var plainText = sr.ReadToEnd();

                    _logger.LogDebug("Successfully decrypted message using key from: {PeerUsername}", peerUsername);
                    return plainText;
                }
                catch (CryptographicException ex) when (ex.Message.Contains("Padding") || ex.Message.Contains("checksum"))
                {
                    _logger.LogDebug("Failed to decrypt with key from {PeerUsername}, trying next key", peerUsername);
                    continue;
                }
            }

            _logger.LogWarning("Failed to decrypt message with any available key");
            return "[ENCRYPTED MESSAGE - UNABLE TO DECRYPT]";
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid base64 format for cipher text");
            return cipherText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed");
            return "[ENCRYPTED MESSAGE - DECRYPTION ERROR]";
        }
    }

    private bool IsLikelyEncrypted(string text)
    {
        try
        {
            if (string.IsNullOrEmpty(text) || text.Length < 24)
                return false;

            var data = Convert.FromBase64String(text);

            return data.Length >= 16;
        }
        catch
        {
            return false;
        }
    }

    private void GenerateKeyPair()
    {
        try
        {
            using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

            var parameters = ecdh.ExportParameters(true);
            _privateKey = parameters.D;
            _publicKey = (parameters.Q.X, parameters.Q.Y);

            _logger.LogInformation("ECDH key pair generated successfully. Public key size: {XLength}+{YLength} bytes", _publicKey.X.Length, _publicKey.Y.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate key pair");
            throw;
        }
    }

    private byte[] DeriveSharedSecret(byte[] peerX, byte[] peerY)
    {
        try
        {
            using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

            var ourParams = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = _privateKey,
                Q = new ECPoint
                {
                    X = _publicKey.X,
                    Y = _publicKey.Y
                }
            };
            ecdh.ImportParameters(ourParams);

            var peerParams = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = peerX,
                    Y = peerY
                }
            };

            using var peerEcdh = ECDiffieHellman.Create();
            peerEcdh.ImportParameters(peerParams);

            var sharedSecret = ecdh.DeriveKeyMaterial(peerEcdh.PublicKey);

            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(sharedSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to derive shared secret");
            throw;
        }
    }

    public bool HasPeerKeys() => _peerPublicKeys.Any();

    public int GetPeerKeysCount() => _peerPublicKeys.Count;

    public IEnumerable<string> GetPeerUsernames() => _peerPublicKeys.Keys;
}