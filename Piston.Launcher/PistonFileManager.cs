using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Piston.Launcher
{
    public class PistonFileManager
    {
        private readonly string _baseUri;

        public PistonFileManager(string baseUri)
        {
            _baseUri = baseUri;
        }

        public List<string> GetDirectoryList()
        {
            var fileList = new List<string>();

            var response = RequestFtp("", WebRequestMethods.Ftp.ListDirectory);
            using (var tr = (TextReader)new StreamReader(response.GetResponseStream()))
            {
                string line;
                while ((line = tr.ReadLine()) != null)
                    fileList.Add(line);
            }

            response.Close();
            return fileList;
        }

        public bool UpdateIfNew(string fileName, string targetDir)
        {
            var info = GetFileInfo(fileName);
            var localPath = Path.Combine(targetDir, fileName);
            if (File.Exists(localPath))
            {
                var localTime = File.GetLastWriteTime(localPath);
                if (info.LastModified < localTime)
                    return false;
            }

            File.WriteAllBytes(localPath, DownloadFile(fileName));
            return true;
        }

        public byte[] DownloadFile(string fileName)
        {
            byte[] file;
            var response = RequestFtp(fileName, WebRequestMethods.Ftp.DownloadFile);
            using (var s = response.GetResponseStream())
            {
                file = new byte[response.ContentLength];
                s.Read(file, 0, (int)response.ContentLength);
            }
            response.Close();
            return file;
        }

        public FileInfo GetFileInfo(string fileName)
        {
            var response = RequestFtp(fileName, WebRequestMethods.Ftp.GetDateTimestamp);
            return new FileInfo
            {
                Name = fileName,
                LastModified = response.LastModified,
                SizeInBytes = response.ContentLength
            };
        }

        private FtpWebResponse RequestFtp(string resource, string method)
        {
            var request = (FtpWebRequest)WebRequest.Create(_baseUri + resource);
            request.Credentials = new NetworkCredential("pistonftp", "piston");
            request.Method = method;

            try { return (FtpWebResponse)request.GetResponse(); }
            catch (WebException e)
            {
                MessageBox.Show($"{e.Message}\r\n{e.StackTrace}");
                throw;
            }
        }

        public struct FileInfo
        {
            public string Name;
            public DateTime LastModified;
            public long SizeInBytes;
        }
    }
}
