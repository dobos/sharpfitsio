using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jhu.SharpFitsIO
{
    public abstract class FitsDataTypeMapping
    {
        public abstract Type From { get; }

        public abstract FitsDataType MapType(int repeat, bool nullable);
        public abstract object MapValue(object value);
    }
}