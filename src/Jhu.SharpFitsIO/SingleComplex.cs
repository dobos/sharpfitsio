using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jhu.SharpFitsIO
{
    public struct SingleComplex
    {
        public Single A;
        public Single B;

        public SingleComplex(Single a, Single b)
        {
            A = a;
            B = b;
        }
    }
}
