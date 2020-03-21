using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

namespace RageHelper
{
    class FileWatcher
    {
        string Root;
        ChangeListener Listener;
        string[] Filter;
        string[] Exclude;

        Dictionary<string, DateTime> FilesLastWriteTime = new Dictionary<string, DateTime>();

        public FileWatcher(string directoryPath, ChangeListener listener, string[] filter, string[] exclude)
        {
            Root = directoryPath;
            Listener = listener;
            Filter = filter;
            Exclude = exclude;

            Init();
        }

        void Init()
        {
            WatchDirectoryFiles(Root);
            CreateInterval();
        }

        void WatchDirectoryFiles(string path)
        {
            var (directories, files) = Utils.GetDirectoryEntities(path);

            foreach (DirectoryInfo directory in directories)
                WatchDirectoryFiles(directory.FullName);

            foreach (FileInfo file in files)
                if (IsSuitableFile(file))
                    FilesLastWriteTime.Add(file.FullName, file.LastWriteTime);
        }

        bool IsSuitableFile(FileInfo file)
        {
            return (Utils.CompareFileExtensionWithArray(file.Extension, Filter) &&
                    !Utils.CompareFilePathWithArray(file.FullName, Exclude));
        }

        void CreateInterval()
        {
            Timer timer = new Timer(1000);

            timer.Elapsed += OnTimeout;
            timer.Start();
        }

        void OnTimeout(object sender, ElapsedEventArgs e)
        {
            if (IsObservableDirectoryChanged())
            {
                UpdateLastWriteTime();
                Listener();
            }
        }

        bool IsObservableDirectoryChanged()
        {
            return AreDirectoryFilesChanged(Root) || IsAnyFileDeleted();
        }

        bool AreDirectoryFilesChanged(string path)
        {
            var (directories, files) = Utils.GetDirectoryEntities(path);

            foreach (DirectoryInfo directory in directories)
            {
                if (AreDirectoryFilesChanged(directory.FullName))
                    return true;
            }

            foreach (FileInfo file in files)
            {
                if (IsSuitableFile(file) &&
                    (IsNewFile(file) ||
                    IsFileLastWriteTimeChanged(file)))
                {
                    return true;
                }
            }

            return false;
        }

        bool IsFileLastWriteTimeChanged(FileInfo file)
        {
            return !FilesLastWriteTime.GetValueOrDefault(file.FullName).Equals(file.LastWriteTime);
        }

        bool IsNewFile(FileInfo file)
        {
            return !FilesLastWriteTime.ContainsKey(file.FullName);
        }

        bool IsAnyFileDeleted()
        {
            foreach (var entry in FilesLastWriteTime)
            {
                if (!File.Exists(entry.Key))
                    return true;
            }

            return false;
        }

        void UpdateLastWriteTime()
        {
            FilesLastWriteTime.Clear();
            WatchDirectoryFiles(Root);
        }

        public delegate void ChangeListener();
    }
}
