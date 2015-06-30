using System;
using DiskUsageReporter.Library;
using System.Diagnostics;

namespace ConsoleApplication
{
    public class Program
    {
        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string dirPath = @"C:\";
            DirStat.GetStatForDirAndItsChildren(dirPath).ForEach(x => PrintSingleDirInfo(x));
            sw.Stop();
            Console.WriteLine("Time taken: " + sw.ElapsedMilliseconds);
            Console.ReadLine();
        }

        private static void PrintSingleDirInfo(DirStat dirStat)
        {
            Console.WriteLine("{0} : {1:N2} MB, ({2:N2} MB)", dirStat.DirFullName, ((double)dirStat.TotalSizeInBytes) / (1024 * 1024), ((double)dirStat.FilesSizeInBytes) / (1024 * 1024));
        }
    }
}
