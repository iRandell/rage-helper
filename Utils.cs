using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace RageHelper
{
    class Utils
    {
        public static void CopyFiles(string src, string dest, string[] filter, string[] exclude)
        {
            var (directories, files) = GetDirectoryEntities(src);

            foreach (DirectoryInfo directory in directories)
            {
                CopyFiles(directory.FullName, Path.Combine(dest, directory.Name), filter, exclude);
            }

            foreach (FileInfo file in files)
            {
                if (CompareFileExtensionWithArray(file.Extension, filter) && 
                    !CompareFilePathWithArray(file.FullName, exclude))
                {
                    Directory.CreateDirectory(dest);
                    File.SetAttributes(file.FullName, FileAttributes.Normal);
                    file.CopyTo(Path.Combine(dest, file.Name), true);
                }
            }
        }

        public static void ClearDirectory(string path)
        {
            if (!Directory.Exists(path))
                return;

            var (directories, files) = GetDirectoryEntities(path);

            foreach (DirectoryInfo directory in directories)
                directory.Delete(true);

            foreach (FileInfo file in files)
                file.Delete();
        }

        public static (DirectoryInfo[], FileInfo[]) GetDirectoryEntities(string path)
        {
            DirectoryInfo info = new DirectoryInfo(path);

            return (info.GetDirectories(), info.GetFiles());
        }

        public static bool CompareFileExtensionWithArray(string extension, string[] array)
        {
            StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase;

            return Array.Exists(array, item => extension.Equals(item, comparisonType));
        }

        public static bool CompareFilePathWithArray(string path, string[] array)
        {
            return Array.Exists(array, item => ComparePaths(path, item));
        }

        public static bool ComparePaths(string first, string second)
        {
            char separator = Path.DirectorySeparatorChar;
            string finalFirst = Path.GetFullPath(first).TrimEnd(separator);
            string finalSecond = Path.GetFullPath(second).TrimEnd(separator);

            return finalFirst.Equals(finalSecond, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
