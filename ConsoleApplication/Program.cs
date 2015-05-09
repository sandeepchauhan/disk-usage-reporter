using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Xml;

namespace ConsoleApplication
{
    public class Program
    {
        public class CacheItem
        {
            [XmlAttribute]
            public string DirFullName;
            
            [XmlAttribute]
            public long DirSize;

            [XmlAttribute]
            public long FilesSize;

            [XmlAttribute]
            public DateTimeOffset LastScannedTime;
        }

        private static Dictionary<string, CacheItem> dirSizeCache = new Dictionary<string, CacheItem>();

        private static FileStream cacheFileStream = null;

        private static DataContractSerializer serializer;

        private static DataContractJsonSerializer jsonSerializer;

        private static XmlWriterSettings xmlWriterSettings;

        private static DateTimeOffset currentCacheFileScanTime = DateTimeOffset.MinValue;

        private static DirectoryInfo dirBeingScanned;

        private static string hashOfDirectoryBeingReported;

        static void Main(string[] args)
        {
            string dirPath = @"C:\Windows\temp";
            Init(dirPath.ToLower());
            List<DirectoryInfo> allDirs = dirBeingScanned.EnumerateDirectories().ToList();
            allDirs.Add(dirBeingScanned);
            List<MyDirInfo> myDirInfos = allDirs.Select(x => new MyDirInfo(x)).ToList();
            foreach(MyDirInfo mdi in myDirInfos)
            {
                // set bool parameter to false if you do not want to include subdirectories.
                Tuple<long, long> sizes = DirectorySize(mdi.dirInfo);
                mdi.sizeInBytes = sizes.Item1;
                mdi.filesSizeInBytes = sizes.Item2;
            }

            myDirInfos = myDirInfos.OrderByDescending(x => x.sizeInBytes).ToList();
            myDirInfos.ForEach(x => PrintSizeOfDir(x));
            Done();
            Console.ReadLine();
        }

        private static void Init(string dirPath)
        {
            dirBeingScanned = new DirectoryInfo(dirPath);
            SHA1 sha1 = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(GetBytes(dirPath));
            hashOfDirectoryBeingReported = BitConverter.ToString(hashBytes).Replace("-", "");
            xmlWriterSettings = new XmlWriterSettings { Indent = true };
            serializer = new DataContractSerializer(typeof(List<CacheItem>));
            jsonSerializer = new DataContractJsonSerializer(typeof(List<CacheItem>));
            DirectoryInfo cacheFilesDirInfo = new DirectoryInfo(@"D:\gitrepos\disk-usage-reporter");
            DateTimeOffset timestampOfLatestCacheFile = DateTimeOffset.MinValue;
            foreach (FileInfo cacheFileInfo in cacheFilesDirInfo.EnumerateFiles("*CacheFile*" + hashOfDirectoryBeingReported + "*"))
            {
                long ticks = long.Parse(cacheFileInfo.Name.Split(new char[] { '-', '.' })[2]);
                DateTimeOffset cacheFileDateTime = new DateTimeOffset(ticks, TimeSpan.Zero);
                if (cacheFileDateTime > timestampOfLatestCacheFile)
                {
                    cacheFileStream = cacheFileInfo.OpenRead();
                    timestampOfLatestCacheFile = cacheFileDateTime;
                }
            }
            if (cacheFileStream != null && cacheFileStream.Length > 0)
            {
                dirSizeCache = ((List<CacheItem>)jsonSerializer.ReadObject(cacheFileStream)).ToDictionary(x => x.DirFullName);
                currentCacheFileScanTime = dirSizeCache.Values.Select(x => x.LastScannedTime).Max();
            }
        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static void Done()
        {
            if (dirSizeCache.Values.Select(x => x.LastScannedTime).Max() > currentCacheFileScanTime)
            {
                using(cacheFileStream = new FileStream(@"D:\gitrepos\disk-usage-reporter\CacheFile-" + hashOfDirectoryBeingReported + "-" + DateTimeOffset.UtcNow.Ticks + ".json", FileMode.CreateNew))
                {
                    //using (XmlWriter xmlWriter = XmlWriter.Create(cacheFileStream, xmlWriterSettings))
                    //{
                        jsonSerializer.WriteObject(cacheFileStream, dirSizeCache.Select(x => x.Value).ToList());
                    //}
                }
            }
        }

        static void PrintSizeOfDir(MyDirInfo myDirInfo)
        {
            Console.WriteLine("{0} : {1:N2} MB, ({2:N2} MB)", myDirInfo.dirInfo.Name, ((double)myDirInfo.sizeInBytes) / (1024 * 1024), ((double)myDirInfo.filesSizeInBytes) / (1024 * 1024));
        }

        static Tuple<long, long> DirectorySize(DirectoryInfo dInfo)
        {
            if (dirSizeCache.ContainsKey(dInfo.FullName))
            {
                return new Tuple<long, long>(dirSizeCache[dInfo.FullName].DirSize, dirSizeCache[dInfo.FullName].FilesSize);
            }

            long totalSize = 0, filesSizeInBytes = 0;
            try
            {
                // Enumerate all the files
                filesSizeInBytes = dInfo.EnumerateFiles().Where(x => !x.Name.Equals("pagefile.sys"))
                             .Sum(file => file.Length);
                totalSize = filesSizeInBytes;
                // Enumerate all sub-directories
                totalSize += dInfo.EnumerateDirectories().Sum(dir => DirectorySize(dir).Item1);
            }
            catch (UnauthorizedAccessException)
            {

            }

            dirSizeCache.Add(dInfo.FullName, new CacheItem { DirFullName = dInfo.FullName, DirSize = totalSize, FilesSize = filesSizeInBytes, LastScannedTime = DateTimeOffset.UtcNow });
            return new Tuple<long, long>(totalSize, filesSizeInBytes);
        }

        private class MyDirInfo
        {
            public DirectoryInfo dirInfo { get; private set; }

            public long sizeInBytes { get; set; }

            public long filesSizeInBytes { get; set; }

            public MyDirInfo(DirectoryInfo dirInfo)
            {
                this.dirInfo = dirInfo;
            }
        }
    }
}
