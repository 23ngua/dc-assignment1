using System;
using System.Collections.Generic;
using System.Linq;

namespace Database
{
    public class SharedFiles
    {
        private static readonly SharedFiles instance = new SharedFiles();
        public static SharedFiles Instance => instance; // Access singleton

        private readonly Dictionary<Guid, FileStruct> files = new Dictionary<Guid, FileStruct>();
        private readonly object filesLock = new object();

        public void AddFile(FileStruct file)
        {
            lock (filesLock) 
            { 
                files[file.GetFileID()] = file; 
            }
        }

        public FileStruct GetFile(Guid fileId)
        {
            lock (filesLock)
            {
                files.TryGetValue(fileId, out FileStruct file);
                return file;
            }
        }

        public List<FileStruct> GetFilesForChannel(string channelName)
        {
            lock (filesLock)
            {
                return files.Values.Where(f => f.GetChannelName() == channelName).ToList();
            }
        }
    }
}