namespace Ricotta.Cryptography
{
    public interface ICryptoProvider
    {
        byte[] Encrypt(byte[] data);
        byte[] Decrypt(byte[] data);
    }
}