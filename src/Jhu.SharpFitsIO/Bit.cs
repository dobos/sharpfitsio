using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jhu.SharpFitsIO
{
    public struct Bit
    {
        public bool Value { get; set; }

        public Bit(bool value)
        {
            Value = value;
        }
    }
}
