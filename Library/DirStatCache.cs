using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;

namespace DiskUsageReporter.Library
{
    public class DirStatCache
    {
        private Dictionary<string, DirStat> _dirStatCache = new Dictionary<string, DirStat>();

        private DataContractJsonSerializer _jsonSerializer;

        private DateTimeOffset _currentCacheFileScanTime;

        private static DirStatCache _instance;

        private DirStatCache()
        {
            _jsonSerializer = new DataContractJsonSerializer(typeof(List<DirStat>));
        }

        public static DirStatCache Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DirStatCache();

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
                        _instance._dirStatCache = ((List<DirStat>)_instance._jsonSerializer.ReadObject(cacheFileStream)).ToDictionary(x => x.DirFullName);
                        _instance._currentCacheFileScanTime = _instance._dirStatCache.Values.Select(x => x.LastScannedTime).Max();
                    }
                }

                return _instance;
            }
        }
        
        public void Flush()
        {
            if (_dirStatCache.Values.Select(x => x.LastScannedTime).Max() > _currentCacheFileScanTime)
            {
                using (FileStream cacheFileStream = new FileStream(@"D:\gitrepos\disk-usage-reporter\CacheFile-" + DateTimeOffset.UtcNow.Ticks + ".json", FileMode.CreateNew))
                {
                    _jsonSerializer.WriteObject(cacheFileStream, _dirStatCache.Select(x => x.Value).ToList());
                }
            }
        }

        public DirStat this[string dirPath]
        {
            get
            {
                DirStat ret;
                this._dirStatCache.TryGetValue(dirPath, out ret);
                return ret;
            }
            set
            {
                this._dirStatCache[dirPath] = value;
            }
        }

        public int NumEntries()
        {
            return _dirStatCache.Count();
        }
    }
}
