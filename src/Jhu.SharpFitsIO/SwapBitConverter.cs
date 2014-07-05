﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jhu.SharpFitsIO
{
    internal unsafe class SwapBitConverter : BitConverterBase
    {
        public override bool IsLittleEndian
        {
            get { return !System.BitConverter.IsLittleEndian; }
        }

        protected override unsafe void PutBytes(byte* dst, byte[] src, int startIndex, int count)
        {
            byte* pd = dst + count;

            for (int i = 0; i < count; i++)
            {
                *(--pd) = src[startIndex + i];
            }
        }

        protected override unsafe int GetBytes(byte* src, byte[] dst, int startIndex, int count)
        {
            byte* ps = src + count;

            for (int i = 0; i < count; i++)
            {
                dst[startIndex + i] = *(--ps);
            }

            return count;
        }        
    }
}
