using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jhu.SharpFitsIO
{
    public struct DoubleComplex
    {
        public Double A;
        public Double B;

        public DoubleComplex(Double a, Double b)
        {
            A = a;
            B = b;
        }
    }
}
