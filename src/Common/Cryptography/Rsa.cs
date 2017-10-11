using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO.Pem;

namespace Ricotta.Cryptography
{
    public class Rsa : ICryptoProvider
    {
        private const string SIGNATURE_ALGORITHM = "SHA1withRSA";
        private const int DEFAULT_KEY_LENGTH = 2048;
        private AsymmetricKeyParameter _keyPublic;
        private AsymmetricKeyParameter _keyPrivate;

        private Rsa(AsymmetricKeyParameter keyPublic,
                    AsymmetricKeyParameter keyPrivate = null)
        {
            _keyPublic = keyPublic;
            _keyPrivate = keyPrivate;
        }

        public string PrivatePem
        {
            get
            {
                using (MemoryStream mem = new MemoryStream())
                {
                    StreamWriter writer = new StreamWriter(mem);
                    Org.BouncyCastle.OpenSsl.PemWriter pem = new Org.BouncyCastle.OpenSsl.PemWriter(writer);
                    pem.WriteObject(_keyPrivate);
                    pem.Writer.Flush();
                    StreamReader reader = new StreamReader(mem);
                    mem.Position = 0;
                    string pemStr = reader.ReadToEnd();
                    return pemStr;
                }
            }
        }
        public string PublicPem
        {
            get
            {
                using (MemoryStream mem = new MemoryStream())
                {
                    StreamWriter writer = new StreamWriter(mem);
                    Org.BouncyCastle.OpenSsl.PemWriter pem = new Org.BouncyCastle.OpenSsl.PemWriter(writer);
                    pem.WriteObject(_keyPublic);
                    pem.Writer.Flush();
                    StreamReader reader = new StreamReader(mem);
                    mem.Position = 0;
                    string pemStr = reader.ReadToEnd();
                    return pemStr;
                }
            }
        }

        public string PublicPemSignature
        {
            get
            {
                var pemBytes = Encoding.UTF8.GetBytes(PublicPem);
                return Sign(pemBytes);
            }
        }

        public string Fingerprint
        {
            get
            {
                var pem = PublicPem;
                using (var md5 = MD5.Create())
                {
                    var hashBytes = md5.ComputeHash(Encoding.ASCII.GetBytes(pem));
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                        sb.Append(":");
                    }
                    var fingerprint = sb.ToString();
                    fingerprint = fingerprint.Substring(0, fingerprint.Length - 1);
                    return fingerprint;
                }
            }
        }

        public static Rsa Create()
        {
            var keyLength = DEFAULT_KEY_LENGTH;
            var secureRandom = new SecureRandom();
            var parameters = new KeyGenerationParameters(secureRandom, keyLength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(parameters);
            var keyPair = keyPairGenerator.GenerateKeyPair();
            return new Rsa(keyPair.Public, keyPair.Private);
        }

        private static MemoryStream GetMemoryStream(string publicPem)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(publicPem));
        }

        public static Rsa CreateFromPublicPEM(string publicPem)
        {
            using (var publicPemStream = GetMemoryStream(publicPem))
            {
                using (var streamReaderPub = new StreamReader(publicPemStream))
                {
                    var pemPubReader = new PemReader(streamReaderPub);
                    var pub = pemPubReader.ReadPemObject();
                    var pubKey = PublicKeyFactory.CreateKey(pub.Content);
                    return new Rsa(pubKey);
                }
            }
        }

        public static Rsa CreateFromPrivatePEM(string privatePem)
        {
            using (var privatePemStream = GetMemoryStream(privatePem))
            {
                using (var streamReaderPriv = new StreamReader(privatePemStream))
                {
                    var pemPrivReader = new PemReader(streamReaderPriv);
                    var priv = pemPrivReader.ReadPemObject();
                    var seq = Asn1Sequence.GetInstance(priv.Content);
                    var rsa = RsaPrivateKeyStructure.GetInstance(seq);
                    var pubSpec = new RsaKeyParameters(false, rsa.Modulus, rsa.PublicExponent);
                    var privSpec = new RsaPrivateCrtKeyParameters(
                                                rsa.Modulus, rsa.PublicExponent, rsa.PrivateExponent,
                                                rsa.Prime1, rsa.Prime2, rsa.Exponent1, rsa.Exponent2,
                                                rsa.Coefficient);
                    return new Rsa(pubSpec, privSpec);
                }
            }
        }

        public byte[] Encrypt(byte[] data)
        {
            if (_keyPublic == null)
            {
                throw new Exception("Public key not set");
            }

            var engine = new OaepEncoding(new RsaEngine());
            engine.Init(true, _keyPublic);
            int length = data.Length;
            int blockSize = engine.GetInputBlockSize();
            var encryptedBytes = new List<byte>();
            for (int chunkPosition = 0;
                chunkPosition < length;
                chunkPosition += blockSize)
            {
                int chunkSize = Math.Min(blockSize, length - chunkPosition);
                encryptedBytes.AddRange(engine.ProcessBlock(
                    data, chunkPosition, chunkSize
                ));
            }
            return encryptedBytes.ToArray();
        }

        public byte[] Decrypt(byte[] data)
        {
            if (_keyPrivate == null)
            {
                throw new Exception("Private key not set");
            }

            var engine = new OaepEncoding(new RsaEngine());
            engine.Init(false, _keyPrivate);

            int length = data.Length;
            int blockSize = engine.GetInputBlockSize();
            var decryptedBytes = new List<byte>();
            for (int chunkPosition = 0;
                chunkPosition < length;
                chunkPosition += blockSize)
            {
                int chunkSize = Math.Min(blockSize, length - chunkPosition);
                decryptedBytes.AddRange(engine.ProcessBlock(
                    data, chunkPosition, chunkSize
                ));
            }
            return decryptedBytes.ToArray();
        }

        public string Sign(byte[] data)
        {
            if (_keyPrivate == null)
            {
                throw new Exception("Private key not set");
            }

            ISigner signer = SignerUtilities.GetSigner(SIGNATURE_ALGORITHM);
            signer.Init(true, _keyPrivate);
            signer.BlockUpdate(data, 0, data.Length);
            var signatureBytes = signer.GenerateSignature();
            return Convert.ToBase64String(signatureBytes);
        }

        public bool Verify(byte[] data, string signature)
        {
            if (_keyPublic == null)
            {
                throw new Exception("Public key not set");
            }

            var signatureBytes = Convert.FromBase64String(signature);
            ISigner signer = SignerUtilities.GetSigner(SIGNATURE_ALGORITHM);
            signer.Init(false, _keyPublic);
            signer.BlockUpdate(data, 0, data.Length);
            return signer.VerifySignature(signatureBytes);
        }
    }
}