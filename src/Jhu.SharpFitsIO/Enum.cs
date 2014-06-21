using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jhu.SharpFitsIO
{
    public enum FitsFileMode
    {
        Unknown,
        Read,
        Write
    }

    public enum Endianness
    {
        LittleEndian,
        BigEndian,
    }
}
