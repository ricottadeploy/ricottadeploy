using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Ricotta.Master
{
    public class FileRepository
    {
        private readonly string _path;

        public FileRepository(string path)
        {
            _path = path;
        }

        private string GetServerFilePath(string fileUri)
        {
            var filePath = Path.Combine(_path, fileUri);
            return filePath;
        }

        public FileInfo GetFileInfo(string fileUri)
        {
            var filePath = GetServerFilePath(fileUri);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(fileUri);
            }

            var fileInfo = new FileInfo(filePath);
            return fileInfo;
        }

        public string GetFileSha256(string fileUri)
        {
            var filePath = GetServerFilePath(fileUri);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(fileUri);
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

        public byte[] GetFileChunk(string fileUri, int offset, int length)
        {
            var filePath = GetServerFilePath(fileUri);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(fileUri);
            }

            byte[] bytes = null;
            using (var file = File.OpenRead(filePath))
            {
                bytes = new byte[length];
                file.Seek(offset, SeekOrigin.Begin);
                file.Read(bytes, 0, length);
            }
            return bytes;
        }
    }
}