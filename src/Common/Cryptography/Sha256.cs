using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Common.Cryptography
{
    public class Sha256
    {
        public static string CalculateFileHash(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            using (var fs = new FileStream(filePath, FileMode.Open))
            {
                using (var sha256 = SHA256.Create())
                {
                    sha256.ComputeHash(fs);
                    byte[] hash = sha256.ComputeHash(fs);
                    StringBuilder formatted = new StringBuilder(2 * hash.Length);
                    foreach (byte b in hash)
                    {
                        formatted.AppendFormat("{0:x2}", b);
                    }
                    return formatted.ToString();
                }
            }
        }
    }
}
