using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

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
