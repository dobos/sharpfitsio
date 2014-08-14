using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jhu.SharpFitsIO
{
    internal abstract class BitConverterBase
    {
        protected delegate int ConvertToTypeDelegate(byte[] bytes, int startIndex, out object value);
        protected delegate int ConvertToArrayTypeDelegate(byte[] bytes, int startIndex, int count, out object value);

        public abstract bool IsLittleEndian { get; }

        unsafe protected abstract void PutBytes(byte* dst, byte[] src, int startIndex, int count);

        unsafe protected abstract int GetBytes(byte* src, byte[] dst, int startIndex, int count);

        protected Dictionary<Type, ConvertToTypeDelegate> convertToTypes;
        protected Dictionary<Type, ConvertToArrayTypeDelegate> convertToArrayTypes;

        protected BitConverterBase()
        {
            convertToTypes = new Dictionary<Type, ConvertToTypeDelegate>()
            {
                { typeof(Boolean), ToBoolean },
                { typeof(Byte), ToByte },
                { typeof(SByte), ToSByte },
                { typeof(Char), ToChar },
                { typeof(String), ToString },
                { typeof(Int16), ToInt16 },
                { typeof(UInt16), ToUInt16 },
                { typeof(Int32), ToInt32 },
                { typeof(UInt32), ToUInt32 },
                { typeof(Int64), ToInt64 },
                { typeof(UInt64), ToUInt64 },
                { typeof(Single), ToSingle },
                { typeof(Double), ToDouble },
                { typeof(SingleComplex), ToSingleComplex },
                { typeof(DoubleComplex), ToDoubleComplex },
            };

            convertToArrayTypes = new Dictionary<Type, ConvertToArrayTypeDelegate>()
            {
                { typeof(Boolean), ToBoolean },
                { typeof(Byte), ToByte },
                { typeof(SByte), ToSByte },
                { typeof(Char), ToChar },
                { typeof(String), ToString },
                { typeof(Int16), ToInt16 },
                { typeof(UInt16), ToUInt16 },
                { typeof(Int32), ToInt32 },
                { typeof(UInt32), ToUInt32 },
                { typeof(Int64), ToInt64 },
                { typeof(UInt64), ToUInt64 },
                { typeof(Single), ToSingle },
                { typeof(Double), ToDouble },
                { typeof(SingleComplex), ToSingleComplex },
                { typeof(DoubleComplex), ToDoubleComplex },
            };
        }

        #region Dynamically dispatched converter functions

        public int ToValue(Type type, byte[] bytes, int startIndex, out object value)
        {
            return convertToTypes[type](bytes, startIndex, out value);
        }

        public int ToValue(Type type, byte[] bytes, int startIndex, int count, out object value)
        {
            return convertToArrayTypes[type](bytes, startIndex, count, out value);
        }

        public int GetBytes(object value, byte[] bytes, int startIndex)
        {
            return GetBytes((dynamic)value, bytes, startIndex);
        }

        public int GetBytes(object value, byte[] bytes, int startIndex, int count)
        {
            return GetBytes((dynamic)value, bytes, startIndex, count);
        }

        #endregion
        #region Boolean

        public Boolean ToBoolean(byte[] bytes, int startIndex)
        {
            // It's true if not 0, 'f' or 'F'
            return bytes[startIndex] != 0 && bytes[startIndex] != Constants.FitsLogicalFalse && bytes[startIndex] != Constants.FitsLogicalFalseAlternate;
        }

        public int ToBoolean(byte[] bytes, int startIndex, out Boolean value)
        {
            value = ToBoolean(bytes, startIndex);
            return sizeof(byte);
        }

        public int ToBoolean(byte[] bytes, int startIndex, out object value)
        {
            value = ToBoolean(bytes, startIndex);
            return sizeof(byte);
        }

        public int ToBoolean(byte[] bytes, int startIndex, int count, out Boolean[] values)
        {
            values = new Boolean[count];

            for (int i = 0; i < count; i++)
            {
                ToBoolean(bytes, startIndex + i, out values[i]);
            }

            return count;
        }

        public int ToBoolean(byte[] bytes, int startIndex, int count, out object values)
        {
            Boolean[] v;
            var res = ToBoolean(bytes, startIndex, count, out v);
            values = v;
            return res;
        }

        unsafe public int GetBytes(Boolean value, byte[] bytes, int startIndex)
        {
            // True: 'T', False: 'F'
            bytes[startIndex] = value ? Constants.FitsLogicalTrue : Constants.FitsLogicalFalse;
            return sizeof(Byte);
        }

        unsafe public int GetBytes(Boolean[] value, byte[] bytes, int startIndex, int count)
        {
            int offset = 0;
            for (int i = 0; i < count; i++)
            {
                bytes[startIndex + offset] = value[i] ? (Byte)0x01 : (Byte)0x00;
                offset += sizeof(Byte);
            }

            return offset;
        }

        #endregion
        #region Byte

        public Byte ToByte(byte[] bytes, int startIndex)
        {
            return bytes[startIndex];
        }

        public int ToByte(byte[] bytes, int startIndex, out Byte value)
        {
            value = ToByte(bytes, startIndex);
            return sizeof(byte);
        }

        public int ToByte(byte[] bytes, int startIndex, out object value)
        {
            value = ToByte(bytes, startIndex);
            return sizeof(byte);
        }

        public int ToByte(byte[] bytes, int startIndex, int count, out byte[] values)
        {
            values = new byte[count];

            Array.Copy(bytes, startIndex, values, 0, count);

            return count;
        }

        public int ToByte(byte[] bytes, int startIndex, int count, out object values)
        {
            Byte[] v;
            var res = ToByte(bytes, startIndex, count, out v);
            values = v;
            return res;
        }

        unsafe public int GetBytes(Byte value, byte[] bytes, int startIndex)
        {
            return GetBytes((byte*)&value, bytes, startIndex, sizeof(Byte));
        }

        unsafe public int GetBytes(Byte[] value, byte[] bytes, int startIndex, int count)
        {
            fixed (byte* vp = value)
            {
                return GetBytes(vp, bytes, startIndex, count);
            }
        }

        #endregion
        #region SByte

        unsafe public SByte ToSByte(byte[] bytes, int startIndex)
        {
            SByte res;
            PutBytes((byte*)&res, bytes, startIndex, sizeof(byte));
            return res;
        }

        public int ToSByte(byte[] bytes, int startIndex, out SByte value)
        {
            value = ToSByte(bytes, startIndex);
            return sizeof(byte);
        }

        public int ToSByte(byte[] bytes, int startIndex, out object value)
        {
            value = ToSByte(bytes, startIndex);
            return sizeof(byte);
        }

        public int ToSByte(byte[] bytes, int startIndex, int count, out SByte[] values)
        {
            values = new SByte[count];

            for (int i = 0; i < count; i++)
            {
                ToSByte(bytes, startIndex + i, out values[i]);
            }

            return count;
        }

        public int ToSByte(byte[] bytes, int startIndex, int count, out object values)
        {
            SByte[] v;
            var res = ToSByte(bytes, startIndex, count, out v);
            values = v;
            return res;
        }

        unsafe public int GetBytes(SByte value, byte[] bytes, int startIndex)
        {
            return GetBytes((byte*)&value, bytes, startIndex, sizeof(SByte));
        }

        unsafe public int GetBytes(SByte[] value, byte[] bytes, int startIndex, int count)
        {
            fixed (SByte* vp = value)
            {
                return GetBytes((byte*)vp, bytes, startIndex, count);
            }
        }

        #endregion
        #region Char

        public Char ToChar(byte[] bytes, int startIndex)
        {
            return Convert.ToChar(bytes[startIndex]);
        }

        public int ToChar(byte[] bytes, int startIndex, out Char value)
        {
            value = ToChar(bytes, startIndex);
            return sizeof(byte);
        }

        public int ToChar(byte[] bytes, int startIndex, out object value)
        {
            value = ToChar(bytes, startIndex);
            return sizeof(byte);
        }

        public int ToChar(byte[] bytes, int startIndex, int count, out Char[] values)
        {
            values = ASCIIEncoding.ASCII.GetChars(bytes, startIndex, count);
            return count;
        }

        public int ToChar(byte[] bytes, int startIndex, int count, out object values)
        {
            Char[] v;
            var res = ToChar(bytes, startIndex, count, out v);
            values = v;
            return res;
        }

        public int GetBytes(Char value, byte[] bytes, int startIndex)
        {
            bytes[startIndex] = (byte)value;
            return sizeof(byte);
        }

        public int GetBytes(Char[] value, byte[] bytes, int startIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                bytes[startIndex] = (byte)value[i];
                startIndex++;
            }
            return sizeof(byte) * count;
        }

        #endregion
        #region String

        public String ToString(byte[] bytes, int startIndex)
        {
            return ASCIIEncoding.ASCII.GetString(bytes, startIndex, sizeof(byte));
        }

        public int ToString(byte[] bytes, int startIndex, out String value)
        {
            value = ToString(bytes, startIndex);
            return sizeof(byte);
        }

        public int ToString(byte[] bytes, int startIndex, out object value)
        {
            value = ToString(bytes, startIndex);
            return sizeof(byte);
        }

        public int ToString(byte[] bytes, int startIndex, int count, out String value)
        {
            value = ASCIIEncoding.ASCII.GetString(bytes, startIndex, count);
            return count;
        }

        public int ToString(byte[] bytes, int startIndex, int count, out object value)
        {
            String v;
            var res = ToString(bytes, startIndex, count, out v);
            value = v;
            return res;
        }

        public int GetBytes(String value, byte[] bytes, int startIndex)
        {
            bytes[startIndex] = (byte)value[0];
            return sizeof(byte);
        }

        public int GetBytes(String value, byte[] bytes, int startIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (i < value.Length)
                {
                    bytes[startIndex] = (byte)value[i];
                }
                else
                {
                    bytes[startIndex] = (byte)0x00;
                }

                startIndex++;
            }

            return sizeof(byte) * count;
        }

        #endregion
        #region Int16

        unsafe public Int16 ToInt16(byte[] bytes, int startIndex)
        {
            Int16 res;
            PutBytes((byte*)&res, bytes, startIndex, sizeof(Int16));
            return res;
        }

        unsafe public int ToInt16(byte[] bytes, int startIndex, out Int16 value)
        {
            value = ToInt16(bytes, startIndex);
            return sizeof(Int16);
        }

        unsafe public int ToInt16(byte[] bytes, int startIndex, out object value)
        {
            value = ToInt16(bytes, startIndex);
            return sizeof(Int16);
        }

        public int ToInt16(byte[] bytes, int startIndex, int count, out Int16[] values)
        {
            values = new Int16[count];

            for (int i = 0; i < count; i++)
            {
                ToInt16(bytes, startIndex + i * sizeof(Int16), out values[i]);
            }

            return count * sizeof(Int16);
        }

        public int ToInt16(byte[] bytes, int startIndex, int count, out object values)
        {
            Int16[] v;
            var res = ToInt16(bytes, startIndex, count, out v);
            values = v;
            return res;
        }

        unsafe public int GetBytes(Int16 value, byte[] bytes, int startIndex)
        {
            return GetBytes((byte*)&value, bytes, startIndex, sizeof(Int16));
        }

        unsafe public int GetBytes(Int16[] value, byte[] bytes, int startIndex, int count)
        {
            fixed (Int16* vp = value)
            {
                return GetBytes((byte*)vp, bytes, startIndex, count);
            }
        }

        #endregion
        #region UInt16

        unsafe public UInt16 ToUInt16(byte[] bytes, int startIndex)
        {
            UInt16 res;
            PutBytes((byte*)&res, bytes, startIndex, sizeof(UInt16));
            return res;
        }

        public int ToUInt16(byte[] bytes, int startIndex, out UInt16 value)
        {
            value = ToUInt16(bytes, startIndex);
            return sizeof(UInt16);
        }

        public int ToUInt16(byte[] bytes, int startIndex, out object value)
        {
            value = ToUInt16(bytes, startIndex);
            return sizeof(UInt16);
        }

        public int ToUInt16(byte[] bytes, int startIndex, int count, out UInt16[] values)
        {
            values = new UInt16[count];

            for (int i = 0; i < count; i++)
            {
                ToUInt16(bytes, startIndex + i * 2, out values[i]);
            }

            return count * sizeof(UInt16);
        }

        public int ToUInt16(byte[] bytes, int startIndex, int count, out object values)
        {
            UInt16[] v;
            var res = ToUInt16(bytes, startIndex, count, out v);
            values = v;
            return res;
        }

        unsafe public int GetBytes(UInt16 value, byte[] bytes, int startIndex)
        {
            return GetBytes((byte*)&value, bytes, startIndex, sizeof(UInt16));
        }

        unsafe public int GetBytes(UInt16[] value, byte[] bytes, int startIndex, int count)
        {
            fixed (UInt16* vp = value)
            {
                return GetBytes((byte*)vp, bytes, startIndex, count);
            }
        }

        #endregion
        #region Int32

        unsafe public Int32 ToInt32(byte[] bytes, int startIndex)
        {
            Int32 res;
            PutBytes((byte*)&res, bytes, startIndex, sizeof(Int32));
            return res;
        }

        public int ToInt32(byte[] bytes, int startIndex, out Int32 value)
        {
            value = ToInt32(bytes, startIndex);
            return sizeof(Int32);
        }

        public int ToInt32(byte[] bytes, int startIndex, out object value)
        {
            value = ToInt32(bytes, startIndex);
            return sizeof(Int32);
        }

        public int ToInt32(byte[] bytes, int startIndex, int count, out Int32[] values)
        {
            values = new Int32[count];

            for (int i = 0; i < count; i++)
            {
                ToInt32(bytes, startIndex + i * sizeof(Int32), out values[i]);
            }

            return count * sizeof(Int32);
        }

        public int ToInt32(byte[] bytes, int startIndex, int count, out object values)
        {
            Int32[] v;
            var res = ToInt32(bytes, startIndex, count, out v);
            values = v;
            return res;
        }

        unsafe public int GetBytes(Int32 value, byte[] bytes, int startIndex)
        {
            return GetBytes((byte*)&value, bytes, startIndex, sizeof(Int32));
        }

        unsafe public int GetBytes(Int32[] value, byte[] bytes, int startIndex, int count)
        {
            fixed (Int32* vp = value)
            {
                return GetBytes((byte*)vp, bytes, startIndex, count);
            }
        }

        #endregion
        #region UInt32

        unsafe public UInt32 ToUInt32(byte[] bytes, int startIndex)
        {
            UInt32 res;
            PutBytes((byte*)&res, bytes, startIndex, sizeof(UInt32));
            return res;
        }

        public int ToUInt32(byte[] bytes, int startIndex, out UInt32 value)
        {
            value = ToUInt32(bytes, startIndex);
            return sizeof(UInt32);
        }

        public int ToUInt32(byte[] bytes, int startIndex, out object value)
        {
            value = ToUInt32(bytes, startIndex);
            return sizeof(UInt32);
        }

        public int ToUInt32(byte[] bytes, int startIndex, int count, out UInt32[] values)
        {
            values = new UInt32[count];

            for (int i = 0; i < count; i++)
            {
                ToUInt32(bytes, startIndex + i * sizeof(UInt32), out values[i]);
            }

            return count * sizeof(UInt32);
        }

        public int ToUInt32(byte[] bytes, int startIndex, int count, out object values)
        {
            UInt32[] v;
            var res = ToUInt32(bytes, startIndex, count, out v);
            values = v;
            return res;
        }

        unsafe public int GetBytes(UInt32 value, byte[] bytes, int startIndex)
        {
            return GetBytes((byte*)&value, bytes, startIndex, sizeof(UInt32));
        }

        unsafe public int GetBytes(UInt32[] value, byte[] bytes, int startIndex, int count)
        {
            fixed (UInt32* vp = value)
            {
                return GetBytes((byte*)vp, bytes, startIndex, count);
            }
        }

        #endregion
        #region Int64

        unsafe public Int64 ToInt64(byte[] bytes, int startIndex)
        {
            Int64 res;
            PutBytes((byte*)&res, bytes, startIndex, sizeof(Int64));
            return res;
        }

        public int ToInt64(byte[] bytes, int startIndex, out Int64 value)
        {
            value = ToInt64(bytes, startIndex);
            return sizeof(Int64);
        }

        public int ToInt64(byte[] bytes, int startIndex, out object value)
        {
            value = ToInt64(bytes, startIndex);
            return sizeof(Int64);
        }

        public int ToInt64(byte[] bytes, int startIndex, int count, out Int64[] values)
        {
            values = new Int64[count];

            for (int i = 0; i < count; i++)
            {
                ToInt64(bytes, startIndex + i * sizeof(Int64), out values[i]);
            }

            return count * sizeof(Int64);
        }

        public int ToInt64(byte[] bytes, int startIndex, int count, out object values)
        {
            Int64[] v;
            var res = ToInt64(bytes, startIndex, count, out v);
            values = v;
            return res;
        }

        unsafe public int GetBytes(Int64 value, byte[] bytes, int startIndex)
        {
            return GetBytes((byte*)&value, bytes, startIndex, sizeof(Int64));
        }

        unsafe public int GetBytes(Int64[] value, byte[] bytes, int startIndex, int count)
        {
            fixed (Int64* vp = value)
            {
                return GetBytes((byte*)vp, bytes, startIndex, count);
            }
        }

        #endregion
        #region UInt64

        unsafe public UInt64 ToUInt64(byte[] bytes, int startIndex)
        {
            UInt64 res;
            PutBytes((byte*)&res, bytes, startIndex, sizeof(UInt64));
            return res;
        }

        public int ToUInt64(byte[] bytes, int startIndex, out UInt64 value)
        {
            value = ToUInt64(bytes, startIndex);
            return sizeof(UInt64);
        }

        public int ToUInt64(byte[] bytes, int startIndex, out object value)
        {
            value = ToUInt64(bytes, startIndex);
            return sizeof(UInt64);
        }

        public int ToUInt64(byte[] bytes, int startIndex, int count, out UInt64[] values)
        {
            values = new UInt64[count];

            for (int i = 0; i < count; i++)
            {
                ToUInt64(bytes, startIndex + i * sizeof(UInt64), out values[i]);
            }

            return count * sizeof(UInt64);
        }

        public int ToUInt64(byte[] bytes, int startIndex, int count, out object values)
        {
            UInt64[] v;
            var res = ToUInt64(bytes, startIndex, count, out v);
            values = v;
            return res;
        }

        unsafe public int GetBytes(UInt64 value, byte[] bytes, int startIndex)
        {
            return GetBytes((byte*)&value, bytes, startIndex, sizeof(UInt64));
        }

        unsafe public int GetBytes(UInt64[] value, byte[] bytes, int startIndex, int count)
        {
            fixed (UInt64* vp = value)
            {
                return GetBytes((byte*)vp, bytes, startIndex, count);
            }
        }

        #endregion
        #region Single

        unsafe public Single ToSingle(byte[] bytes, int startIndex)
        {
            Single res;
            PutBytes((byte*)&res, bytes, startIndex, sizeof(Single));
            return res;
        }

        public int ToSingle(byte[] bytes, int startIndex, out Single value)
        {
            value = ToSingle(bytes, startIndex);
            return sizeof(Single);
        }

        public int ToSingle(byte[] bytes, int startIndex, out object value)
        {
            value = ToSingle(bytes, startIndex);
            return sizeof(Single);
        }

        public int ToSingle(byte[] bytes, int startIndex, int count, out Single[] values)
        {
            values = new Single[count];

            for (int i = 0; i < count; i++)
            {
                ToSingle(bytes, startIndex + i * sizeof(Single), out values[i]);
            }

            return count * sizeof(Single);
        }

        public int ToSingle(byte[] bytes, int startIndex, int count, out object values)
        {
            Single[] v;
            var res = ToSingle(bytes, startIndex, count, out v);
            values = v;
            return res;
        }

        unsafe public int GetBytes(Single value, byte[] bytes, int startIndex)
        {
            return GetBytes((byte*)&value, bytes, startIndex, sizeof(Single));
        }

        unsafe public int GetBytes(Single[] value, byte[] bytes, int startIndex, int count)
        {
            fixed (Single* vp = value)
            {
                return GetBytes((byte*)vp, bytes, startIndex, count);
            }
        }

        #endregion
        #region Double

        unsafe public Double ToDouble(byte[] bytes, int startIndex)
        {
            Double res;
            PutBytes((byte*)&res, bytes, startIndex, sizeof(Double));
            return res;
        }

        unsafe public int ToDouble(byte[] bytes, int startIndex, out Double value)
        {
            value = ToDouble(bytes, startIndex);
            return sizeof(Double);
        }

        unsafe public int ToDouble(byte[] bytes, int startIndex, out object value)
        {
            value = ToDouble(bytes, startIndex);
            return sizeof(Double);
        }

        public int ToDouble(byte[] bytes, int startIndex, int count, out Double[] values)
        {
            values = new Double[count];

            for (int i = 0; i < count; i++)
            {
                ToDouble(bytes, startIndex + i * sizeof(Double), out values[i]);
            }

            return count * sizeof(Double);
        }

        public int ToDouble(byte[] bytes, int startIndex, int count, out object values)
        {
            Double[] v;
            var res = ToDouble(bytes, startIndex, count, out v);
            values = v;
            return res;
        }

        unsafe public int GetBytes(Double value, byte[] bytes, int startIndex)
        {
            return GetBytes((byte*)&value, bytes, startIndex, sizeof(Double));
        }

        unsafe public int GetBytes(Double[] value, byte[] bytes, int startIndex, int count)
        {
            fixed (Double* vp = value)
            {
                return GetBytes((byte*)vp, bytes, startIndex, count);
            }
        }

        #endregion
        #region SingleComplex

        unsafe public SingleComplex ToSingleComplex(byte[] bytes, int startIndex)
        {
            SingleComplex res;
            PutBytes((byte*)&res, bytes, startIndex, 2 * sizeof(Single));
            return res;
        }

        public int ToSingleComplex(byte[] bytes, int startIndex, out SingleComplex value)
        {
            value = ToSingleComplex(bytes, startIndex);
            return 2 * sizeof(Single);
        }

        public int ToSingleComplex(byte[] bytes, int startIndex, out object value)
        {
            value = ToSingleComplex(bytes, startIndex);
            return 2 * sizeof(Single);
        }

        public int ToSingleComplex(byte[] bytes, int startIndex, int count, out SingleComplex[] values)
        {
            values = new SingleComplex[count];

            for (int i = 0; i < count; i++)
            {
                ToSingleComplex(bytes, startIndex + i * 2 * sizeof(Single), out values[i]);
            }

            return count * 2 * sizeof(Single);
        }

        public int ToSingleComplex(byte[] bytes, int startIndex, int count, out object values)
        {
            SingleComplex[] v;
            var res = ToSingleComplex(bytes, startIndex, count, out v);
            values = v;
            return res;
        }

        unsafe public int GetBytes(SingleComplex value, byte[] bytes, int startIndex)
        {
            return GetBytes((byte*)&value, bytes, startIndex, sizeof(SingleComplex));
        }

        unsafe public int GetBytes(SingleComplex[] value, byte[] bytes, int startIndex, int count)
        {
            fixed (SingleComplex* vp = value)
            {
                return GetBytes((byte*)vp, bytes, startIndex, count);
            }
        }

        #endregion
        #region DoubleComplex

        unsafe public DoubleComplex ToDoubleComplex(byte[] bytes, int startIndex)
        {
            DoubleComplex res;
            PutBytes((byte*)&res, bytes, startIndex, 2 * sizeof(Double));
            return res;
        }

        public int ToDoubleComplex(byte[] bytes, int startIndex, out DoubleComplex value)
        {
            value = ToDoubleComplex(bytes, startIndex);
            return 2 * sizeof(Double);
        }

        public int ToDoubleComplex(byte[] bytes, int startIndex, out object value)
        {
            value = ToDoubleComplex(bytes, startIndex);
            return 2 * sizeof(Double);
        }

        public int ToDoubleComplex(byte[] bytes, int startIndex, int count, out DoubleComplex[] values)
        {
            values = new DoubleComplex[count];

            for (int i = 0; i < count; i++)
            {
                ToDoubleComplex(bytes, startIndex + i * 2 * sizeof(Double), out values[i]);
            }

            return count * 2 * sizeof(Double);
        }

        public int ToDoubleComplex(byte[] bytes, int startIndex, int count, out object values)
        {
            DoubleComplex[] v;
            var res = ToDoubleComplex(bytes, startIndex, count, out v);
            values = v;
            return res;
        }

        unsafe public int GetBytes(DoubleComplex value, byte[] bytes, int startIndex)
        {
            return GetBytes((byte*)&value, bytes, startIndex, sizeof(DoubleComplex));
        }

        unsafe public int GetBytes(DoubleComplex[] value, byte[] bytes, int startIndex, int count)
        {
            fixed (DoubleComplex* vp = value)
            {
                return GetBytes((byte*)vp, bytes, startIndex, count);
            }
        }

        #endregion
    }
}
