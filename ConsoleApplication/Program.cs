using System;
using DiskUsageReporter.Library;
using System.Collections.Generic;

namespace ConsoleApplication
{
    public class Program
    {
        static void Main(string[] args)
        {
            string dirPath = @"C:\users\sanch\appdata\local\microsoft";
            DirStat.GetStatForDirAndItsChildren(dirPath).ForEach(x => PrintSingleDirInfo(x));
            Console.ReadLine();
        }

        private static void PrintSingleDirInfo(DirStat dirStat)
        {
            Console.WriteLine("{0} : {1:N2} MB, ({2:N2} MB)", dirStat.DirFullName, ((double)dirStat.TotalSizeInBytes) / (1024 * 1024), ((double)dirStat.FilesSizeInBytes) / (1024 * 1024));
        }
    }
}
