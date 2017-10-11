using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using sc = System.Security.Cryptography;

namespace Ricotta.Cryptography
{
    public class Aes : ICryptoProvider
    {
        private byte[] _key;
        private byte[] _iv;

        private Aes(byte[] key, byte[] iv)
        {
            _key = key;
            _iv = iv;
        }

        public byte[] Key
        {
            get
            {
                return _key;
            }
        }

        public byte[] IV
        {
            get
            {
                return _iv;
            }
        }

        public static Aes Create()
        {
            var aes = sc.Aes.Create();
            var instance = new Aes(aes.Key, aes.IV);
            return instance;
        }

        public static Aes Create(byte[] key, byte[] iv)
        {
            return new Aes(key, iv);
        }

        public byte[] Encrypt(byte[] data)
        {
            using (var aes = sc.Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                var encryptor = aes.CreateEncryptor(_key, _iv);
                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new sc.CryptoStream(memoryStream, encryptor, sc.CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0, data.Length);
                    }
                    return memoryStream.ToArray();
                }
            }
        }

        public byte[] Decrypt(byte[] data)
        {
            using (var aes = sc.Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                var decryptor = aes.CreateDecryptor(_key, _iv);
                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new sc.CryptoStream(memoryStream, decryptor, sc.CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0, data.Length);
                    }
                    return memoryStream.ToArray();
                }
            }
        }
    }
}