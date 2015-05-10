using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace DiskUsageReporter.Library
{
    public class DirStatCacheWithList
    {
        private List<DirStat> _dirStatCache = new List<DirStat>();

        private DataContractJsonSerializer _jsonSerializer;

        private DateTimeOffset _currentCacheFileScanTime;

        private static DirStatCacheWithList _instance;

        private DirStatCacheWithList()
        {
            _jsonSerializer = new DataContractJsonSerializer(typeof(List<DirStat>));
        }

        public static DirStatCacheWithList Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DirStatCacheWithList();

                    FileStream cacheFileStream = null;
                    DirectoryInfo cacheFilesDirInfo = new DirectoryInfo(@"D:\gitrepos\disk-usage-reporter");
                    DateTimeOffset timestampOfLatestCacheFile = DateTimeOffset.MinValue;
                    foreach (FileInfo cacheFileInfo in cacheFilesDirInfo.EnumerateFiles("*CacheFile*"))
                    {
                        long ticks = long.Parse(cacheFileInfo.Name.Split(new char[] { '-', '.' })[1]);
                        DateTimeOffset cacheFileDateTime = new DateTimeOffset(ticks, TimeSpan.Zero);
                        if (cacheFileDateTime > timestampOfLatestCacheFile)
                        {
                            cacheFileStream = cacheFileInfo.OpenRead();
                            timestampOfLatestCacheFile = cacheFileDateTime;
                        }
                    }
                    if (cacheFileStream != null && cacheFileStream.Length > 0)
                    {
                        _instance._dirStatCache = ((List<DirStat>)_instance._jsonSerializer.ReadObject(cacheFileStream));
                        _instance._currentCacheFileScanTime = _instance._dirStatCache.Select(x => x.LastScannedTime).Max();
                    }
                }

                return _instance;
            }
        }
        
        public void Flush()
        {
            if (_dirStatCache.Select(x => x.LastScannedTime).Max() > _currentCacheFileScanTime)
            {
                using (FileStream cacheFileStream = new FileStream(@"D:\gitrepos\disk-usage-reporter\CacheFile-" + DateTimeOffset.UtcNow.Ticks + ".json", FileMode.CreateNew))
                {
                    _jsonSerializer.WriteObject(cacheFileStream, _dirStatCache.ToList());
                }
            }
        }

        public DirStat this[string dirPath]
        {
            get
            {
                return this._dirStatCache.Find(x => x.DirFullName.Equals(dirPath));
            }
            set
            {
                this._dirStatCache.Add(value);
            }
        }

        public int NumEntries()
        {
            return _dirStatCache.Count();
        }
    }
}
