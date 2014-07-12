using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jhu.SharpFitsIO
{
    public static class FitsDataTypes
    {
        public static FitsDataType Logical
        {
            get
            {
                return new FitsDataType()
                {
                    Name = Constants.FitsTypeNameLogical,
                    Type = typeof(Boolean),
                    Code = 'L',
                    ByteSize = sizeof(Byte),
                    NullValue = (Byte)0x80,
                };
            }
        }

        public static FitsDataType Bit
        {
            get
            {
                return new FitsDataType()
                {
                    Name = Constants.FitsTypeNameBit,
                    Type = typeof(Byte),
                    Code = 'X',
                    ByteSize = sizeof(Byte),
                    NullValue = (Byte)0x80,
                };
            }
        }

        public static FitsDataType Byte
        {
            get
            {
                return new FitsDataType()
                {
                    Name = Constants.FitsTypeNameByte,
                    Type = typeof(Byte),
                    Code = 'B',
                    ByteSize = sizeof(Byte),
                    NullValue = (Byte)0x80,
                };
            }
        }

        public static FitsDataType Int16
        {
            get
            {
                return new FitsDataType()
                {
                    Name = Constants.FitsTypeNameInt16,
                    Type = typeof(Int16),
                    Code = 'I',
                    ByteSize = sizeof(Int16),
                    NullValue = System.Int16.MinValue,
                };
            }
        }

        public static FitsDataType Int32
        {
            get
            {
                return new FitsDataType()
                {
                    Name = Constants.FitsTypeNameInt32,
                    Type = typeof(Int32),
                    Code = 'J',
                    ByteSize = sizeof(Int32),
                    NullValue = System.Int32.MinValue,
                };
            }
        }

        public static FitsDataType Int64
        {
            get
            {
                return new FitsDataType()
                {
                    Name = Constants.FitsTypeNameInt64,
                    Type = typeof(Int64),
                    Code = 'K',
                    ByteSize = sizeof(Int64),
                    NullValue = System.Int64.MinValue,
                };
            }
        }

        public static FitsDataType Char
        {
            get
            {
                return new FitsDataType()
                {
                    Name = Constants.FitsTypeNameChar,
                    Type = typeof(String),
                    Code = 'A',
                    ByteSize = sizeof(Byte),
                    NullValue = "\x00",
                };
            }
        }

        public static FitsDataType Single
        {
            get
            {
                return new FitsDataType()
                {
                    Name = Constants.FitsTypeNameSingle,
                    Type = typeof(Single),
                    Code = 'E',
                    ByteSize = sizeof(Single),
                    NullValue = System.Single.NaN,
                };
            }
        }

        public static FitsDataType Double
        {
            get
            {
                return new FitsDataType()
                {
                    Name = Constants.FitsTypeNameDouble,
                    Type = typeof(Double),
                    Code = 'D',
                    ByteSize = sizeof(Double),
                    NullValue = System.Double.NaN,
                };
            }
        }

        public static FitsDataType SingleComplex
        {
            get
            {
                return new FitsDataType()
                {
                    Name = Constants.FitsTypeNameSingleComplex,
                    Type = typeof(SingleComplex),
                    Code = 'C',
                    ByteSize = 2 * sizeof(Single),
                    NullValue = SharpFitsIO.SingleComplex.NaN,
                };
            }
        }

        public static FitsDataType DoubleComplex
        {
            get
            {
                return new FitsDataType()
                {
                    Name = Constants.FitsTypeNameDoubleComplex,
                    Type = typeof(DoubleComplex),
                    Code = 'M',
                    ByteSize = 2 * sizeof(Double),
                    NullValue = SharpFitsIO.DoubleComplex.NaN,
                };
            }
        }
    }
}
