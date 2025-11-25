namespace Chat.Core.Interfaces
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        byte[] GetPublicKey();
        void SetPeerPublicKey(string peerUsername, byte[] publicKey);
    }
}
