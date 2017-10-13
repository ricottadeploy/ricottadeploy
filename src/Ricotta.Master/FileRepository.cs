using Common.Cryptography;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Ricotta.Master
{
    public class FileRepository
    {
        private readonly string _path;

        public string Path
        {
            get
            {
                return _path;
            }
        }

        public FileRepository(string path)
        {
            _path = path;
        }

        private string GetServerFilePath(string fileUri)
        {
            var filePath = System.IO.Path.Combine(_path, fileUri);
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

            return Sha256.CalculateFileHash(filePath);
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