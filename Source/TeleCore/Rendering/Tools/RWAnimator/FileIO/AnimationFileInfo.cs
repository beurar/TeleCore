using System;
using System.IO;

namespace TeleCore
{
    internal class AnimationFileInfo
    {
        private string fileName;
        private bool loaded;

        private FileInfo fileInfo;
        private DateTime lastWriteTime;

        private object lockObject = new object();

        public FileInfo FileInfo => fileInfo;

        public AnimationFileInfo(FileInfo fileInfo)
        {
            this.fileInfo = fileInfo;
            fileName = fileInfo.Name;
            lastWriteTime = fileInfo.LastWriteTime;
        }

        public void LoadData()
        {
            var obj = lockObject;
            lock (obj)
            {
                loaded = true;
            }
        }
    }
}
