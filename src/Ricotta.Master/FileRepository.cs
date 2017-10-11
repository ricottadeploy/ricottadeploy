using System;
using System.IO;
using Serilog;

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
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                return fileInfo;
            }
            return null;
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