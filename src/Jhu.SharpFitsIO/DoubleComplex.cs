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

        public static DoubleComplex NaN
        {
            get
            {
                return new DoubleComplex(Double.NaN, Double.NaN);
            }
        }

        public static DoubleComplex Parse(string value)
        {
            return Parse(value, System.Threading.Thread.CurrentThread.CurrentCulture);
        }

        public static DoubleComplex Parse(string value, IFormatProvider provider)
        {
            var parts = value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            return new DoubleComplex(Double.Parse(parts[0], provider), Double.Parse(parts[1], provider));
        }

        public override string ToString()
        {
            return ToString("G", System.Threading.Thread.CurrentThread.CurrentCulture);
        }

        public string ToString(IFormatProvider provider)
        {
            return ToString("G", provider);
        }

        public string ToString(string format, IFormatProvider provider)
        {
            return A.ToString(format, provider) + " " + B.ToString(format, provider);
        }
    }
}
