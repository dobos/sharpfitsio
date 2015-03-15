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

        public static SingleComplex NaN
        {
            get
            {
                return new SingleComplex(Single.NaN, Single.NaN);
            }
        }

        public static SingleComplex Parse(string value)
        {
            return Parse(value, System.Threading.Thread.CurrentThread.CurrentCulture);
        }

        public static SingleComplex Parse(string value, IFormatProvider provider)
        {
            var parts = value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            return new SingleComplex(Single.Parse(parts[0], provider), Single.Parse(parts[1], provider));
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
