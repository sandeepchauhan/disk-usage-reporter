using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiskUsageReporter.Library
{
    public class DirStat
    {
        public string DirFullName;

        public long TotalSizeInBytes;

        public long FilesSizeInBytes;

        public DateTimeOffset LastScannedTime;

        private DirStat()
        {

        }

        private DirStat(string dirFullName)
        {
            List<DirStat> ChildDirs = new List<DirStat>();
            this.DirFullName = dirFullName.ToLower();
            DirectoryInfo dirInfo = new DirectoryInfo(dirFullName);
            try
            {
                List<string> excludedFiles = new List<string> { "pagefile.sys", "swapfile.sys" };
                this.FilesSizeInBytes = dirInfo.EnumerateFiles().Where(x => !excludedFiles.Contains(x.Name)).Sum(x => x.Length);
                foreach (DirectoryInfo childDirInfo in dirInfo.EnumerateDirectories())
                {
                    ChildDirs.Add(DirStat.GetDirStat(childDirInfo.FullName, true));
                }
            }
            catch (UnauthorizedAccessException)
            {
            }

            this.TotalSizeInBytes = this.FilesSizeInBytes + ChildDirs.Sum(x => x.TotalSizeInBytes);
            this.LastScannedTime = DateTimeOffset.UtcNow;
        }

        private static DirStat GetDirStat(string dirFullName, bool rescan)
        {
            DirStat ret = null;
            string dirFullNameLowerCase = dirFullName.ToLower();
            if (!rescan && DirStatCache.Instance.dirStatCache.ContainsKey(dirFullNameLowerCase))
            {
                ret = DirStatCache.Instance.dirStatCache[dirFullNameLowerCase];
            }
            else
            {
                ret = new DirStat(dirFullNameLowerCase);
                if (rescan && DirStatCache.Instance.dirStatCache.ContainsKey(ret.DirFullName))
                {
                    DirStatCache.Instance.dirStatCache[ret.DirFullName] = ret;
                }
                else
                {
                    DirStatCache.Instance.dirStatCache.Add(ret.DirFullName, ret);
                }
            }

            return ret;
        }

        public static List<DirStat> GetStatForDirAndItsChildren(string dirPath, bool rescan = false)
        {
            List<DirStat> list = new List<DirStat>();
            DirStat rootDirStat = DirStat.GetDirStat(dirPath, rescan);
            list.Add(rootDirStat);
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
            list.AddRange(dirInfo.EnumerateDirectories().Select(x => DirStat.GetDirStat(x.FullName, false)).OrderByDescending(x => x.TotalSizeInBytes).ToList());
            DirStatCache.Instance.Flush();
            return list;
        }
    }
}
