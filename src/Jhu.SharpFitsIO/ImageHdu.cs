using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jhu.SharpFitsIO
{
    public class ImageHdu : SimpleHdu, ICloneable
    {
        internal ImageHdu(FitsFile fits)
            : base(fits)
        {
            InitializeMembers();
        }

        internal ImageHdu(SimpleHdu hdu)
            :base(hdu)
        {
            InitializeMembers();
        }

        private ImageHdu(ImageHdu old)
            : base(old)
        {
            CopyMembers(old);
        }

        private void InitializeMembers()
        {
        }

        private void CopyMembers(ImageHdu old)
        {
        }

        public override object Clone()
        {
            return new ImageHdu(this);
        }

        #region Stride functions

        public Int16[] ReadStrideInt16()
        {
            return ReadStrideInt16Async().Result;
        }

        public async Task<Int16[]> ReadStrideInt16Async()
        {
            await ReadStrideAsync();

            Int16[] values;
            Fits.BitConverter.ToInt16(StrideBuffer, 0, StrideBuffer.Length / 2, out values);
            return values;
        }

        public Int32[] ReadStrideInt32()
        {
            return ReadStrideInt32Async().Result;
        }

        public async Task<Int32[]> ReadStrideInt32Async()
        {
            await ReadStrideAsync();

            Int32[] values;
            Fits.BitConverter.ToInt32(StrideBuffer, 0, StrideBuffer.Length / 4, out values);
            return values;
        }

        public Single[] ReadStrideSingle()
        {
            return ReadStrideSingleAsync().Result;
        }

        public async Task<Single[]> ReadStrideSingleAsync()
        {
            await ReadStrideAsync();

            Single[] values;
            Fits.BitConverter.ToSingle(StrideBuffer, 0, StrideBuffer.Length / 4, out values);
            return values;
        }

        public Double[] ReadStrideDouble()
        {
            return ReadStrideDoubleAsync().Result;
        }

        public async Task<Double[]> ReadStrideDoubleAsync()
        {
            await ReadStrideAsync();

            Double[] values;
            Fits.BitConverter.ToDouble(StrideBuffer, 0, StrideBuffer.Length / 8, out values);
            return values;
        }

        #endregion
    }
}
