using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DiskUsageReporter.Library
{
    public class DirStat
    {
        public string DirFullName;

        public long TotalSizeInBytes;

        public long FilesSizeInBytes;

        public long NumberOfDirs;

        public long NumberOfFiles;

        public DateTimeOffset LastScannedTime;

        private DirStat()
        {

        }

        private DirStat(string dirFullName, bool rootDir)
        {
            List<DirStat> ChildDirs = new List<DirStat>();
            this.DirFullName = dirFullName.ToLower();
            DirectoryInfo dirInfo = new DirectoryInfo(dirFullName);
            try
            {
                List<string> excludedFiles = new List<string> { "pagefile.sys", "swapfile.sys" };
                IEnumerable<FileInfo> childFiles = dirInfo.EnumerateFiles().Where(x => !excludedFiles.Contains(x.Name));
                this.NumberOfFiles = childFiles.Count();
                this.FilesSizeInBytes = childFiles.Sum(x => x.Length);
                foreach (DirectoryInfo childDirInfo in dirInfo.EnumerateDirectories())
                {
                    ChildDirs.Add(DirStat.GetDirStat(childDirInfo.FullName, true));
                    this.NumberOfDirs++;
                    if (rootDir)
                    {
                        Console.WriteLine("Done with: {0} Cache size: {1}", childDirInfo.Name, DirStatCache.Instance.NumEntries());
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }

            this.TotalSizeInBytes = this.FilesSizeInBytes + ChildDirs.Sum(x => x.TotalSizeInBytes);
            this.NumberOfFiles += ChildDirs.Sum(x => x.NumberOfFiles);
            this.NumberOfDirs += ChildDirs.Sum(x => x.NumberOfDirs);
            this.LastScannedTime = DateTimeOffset.UtcNow;
        }

        private static DirStat GetDirStat(string dirFullName, bool rescan, bool rootDir = false)
        {
            string dirFullNameLowerCase = dirFullName.ToLower();
            DirStat ret = DirStatCache.Instance[dirFullNameLowerCase];
            if (ret == null || rescan)
            {
                ret = new DirStat(dirFullNameLowerCase, rootDir);
                DirStatCache.Instance[ret.DirFullName] = ret;
            }

            return ret;
        }

        public static List<DirStat> GetStatForDirAndItsChildren(string dirPath, bool rescan = false)
        {
            List<DirStat> list = new List<DirStat>();
            DirStat rootDirStat = DirStat.GetDirStat(dirPath, rescan, true);
            list.Add(rootDirStat);
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
            Console.WriteLine("Root done...");
            list.AddRange(dirInfo.EnumerateDirectories().Select(x => DirStat.GetDirStat(x.FullName, false)).OrderByDescending(x => x.TotalSizeInBytes).ToList());
            DirStatCache.Instance.Flush();
            return list;
        }
    }
}
