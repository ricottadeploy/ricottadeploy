using System;
using System.Security.Cryptography;
using System.Text;

namespace Ricotta.Cryptography
{
    public class TLS12
    {
        public static byte[] GetRandomBytes(int length)
        {
            var rngCsp = RNGCryptoServiceProvider.Create();
            var random = new byte[length];
            rngCsp.GetBytes(random);
            return random;
        }

        public static byte[] GetRandom()
        {
            return GetRandomBytes(28);
        }

        public static byte[] GetPreMasterSecret()
        {
            return GetRandomBytes(46);
        }

        public static byte[] GetIV()
        {
            return GetRandomBytes(16);
        }


        public static byte[] GetMasterSecret(byte[] preMasterSecret, byte[] clientRandom, byte[] serverRandom)
        {
            var label = Encoding.ASCII.GetBytes("master secret");
            var seed = ConcatByteArrays(clientRandom, serverRandom);
            return PRF(preMasterSecret, label, seed, 48);
        }

        // https://tools.ietf.org/html/rfc5246#section-6.3
        // 32 - client write MAC key
        // 32 - server write MAC key
        // 32 - client write key
        // 32 - server write key
        public static byte[] GetKeys(byte[] masterSecret, byte[] clientRandom, byte[] serverRandom)
        {
            var label = Encoding.ASCII.GetBytes("key expansion");
            var seed = ConcatByteArrays(serverRandom, clientRandom);
            return PRF(masterSecret, label, seed, 128);
        }

        public static byte[] GetClientWriteMACKey(byte[] keys)
        {
            return GetByteBlock(keys, 0, 32);
        }

        public static byte[] GetServerWriteMACKey(byte[] keys)
        {
            return GetByteBlock(keys, 1, 32);
        }

        public static byte[] GetClientWriteKey(byte[] keys)
        {
            return GetByteBlock(keys, 2, 32);
        }

        public static byte[] GetServerWriteKey(byte[] keys)
        {
            return GetByteBlock(keys, 3, 32);
        }

        private static byte[] GetByteBlock(byte[] array, int blockIndex, int blockSize)
        {
            var result = new byte[blockSize];
            Buffer.BlockCopy(array, blockIndex * blockSize, result, 0, blockSize);
            return result;
        }

        private static byte[] PRF(byte[] secret, byte[] label, byte[] seed, int bytesToGenerate)
        {
            var labelAndSeed = new byte[label.Length + seed.Length];
            Buffer.BlockCopy(label, 0, labelAndSeed, 0, label.Length);
            Buffer.BlockCopy(seed, 0, labelAndSeed, seed.Length, label.Length);
            return P_SHA384(secret, labelAndSeed, bytesToGenerate);
        }

        private static byte[] P_SHA384(byte[] secret, byte[] seed, int bytesToGenerate)
        {
            var BLOCKSIZE = 48;
            var iterations = bytesToGenerate / BLOCKSIZE + 1;
            var generatedBytes = new byte[iterations * BLOCKSIZE];

            var hmac = new HMACSHA384(secret);

            var a = new byte[iterations][];
            a[0] = seed;
            a[1] = hmac.ComputeHash(a[0]);

            var previousA = hmac.ComputeHash(seed);

            for (int i = 1; i <= iterations; i++)
            {
                var A = hmac.ComputeHash(previousA);
                var aAndSeed = new byte[BLOCKSIZE + seed.Length];
                Buffer.BlockCopy(A, 0, aAndSeed, 0, A.Length);
                Buffer.BlockCopy(seed, 0, aAndSeed, A.Length, seed.Length);

                var part = hmac.ComputeHash(aAndSeed);
                Buffer.BlockCopy(part, 0, generatedBytes, (i - 1) * BLOCKSIZE, part.Length);
                previousA = A;
            }

            var result = new byte[bytesToGenerate];
            Buffer.BlockCopy(generatedBytes, 0, result, 0, result.Length);
            return result;
        }

        private static byte[] ConcatByteArrays(byte[] array1, byte[] array2)
        {
            byte[] result = new byte[array1.Length + array2.Length];
            Buffer.BlockCopy(array1, 0, result, 0, array1.Length);
            Buffer.BlockCopy(array2, 0, result, array1.Length, array2.Length);
            return result;
        }
    }
}