using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleCore
{
    public interface IStringCache
    {
        public string[] CachedStrings { get; set; }
        public void UpdateString(int index);
        public string CachedString(int index);
    }
}
