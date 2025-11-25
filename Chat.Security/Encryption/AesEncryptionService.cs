using Chat.Core.Interfaces;
using System.Security.Cryptography;

namespace Chat.Security.Encryption
{
    public class AesEncryptionService : IEncryptionService
    {
        private readonly Aes _aes;
        private readonly byte[] _key;
        private readonly byte[] _iv;
        private readonly Dictionary<string, byte[]> _peerKeys = new();

        public AesEncryptionService()
        {
            _aes = Aes.Create();
            _aes.GenerateKey();
            _aes.GenerateIV();
            _key = _aes.Key;
            _iv = _aes.IV;
        }

        public AesEncryptionService(byte[] key, byte[] iv)
        {
            _aes = Aes.Create();
            _key = key;
            _iv = iv;
        }

        public string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var sw = new StreamWriter(cs);

            sw.Write(plainText);
            sw.Close();
            cs.Close();

            return Convert.ToBase64String(ms.ToArray());
        }

        public string Decrypt(string cipherText)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var buffer = Convert.FromBase64String(cipherText);

            using var ms = new MemoryStream(buffer);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }

        public string EncryptForUser(string username, string plainText)
        {
            if (_peerKeys.TryGetValue(username, out var peerKey))
            {
                using var aes = Aes.Create();
                aes.Key = peerKey;
                aes.IV = _iv;

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();
                using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
                using var sw = new StreamWriter(cs);

                sw.Write(plainText);
                sw.Close();
                cs.Close();

                return Convert.ToBase64String(ms.ToArray());
            }
            throw new InvalidOperationException($"No key found for user {username}");
        }

        public void AddPeerKey(string username, byte[] key) => _peerKeys[username] = key;

        public byte[] GetPublicKey() => _key;

        public void SetPeerPublicKey(string peerUsername, byte[] publicKey) => AddPeerKey(peerUsername, publicKey);

        public static (byte[] key, byte[] iv) GenerateKeyAndIv()
        {
            using var aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();
            return (aes.Key, aes.IV);
        }
    }
}