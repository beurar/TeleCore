using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleCore
{
    public struct CellValueData
    {
        //Index of the cell
        public uint index;
        //Actual value
        public uint value;
        //CPU value input
        public uint inputValue;

        public CellValueData(uint index, uint value)
        {
            this.index = index;
            this.value = value;
            this.inputValue = 0;
        }
    }
}
