using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

namespace Jhu.SharpFitsIO
{
    public class FitsDataType : ICloneable
    {
        private static readonly Regex FormatRegex = new Regex(@"([0-9]*)([LXBIJKAEDCMP]+)");

        #region Private member variables

        /// <summary>
        /// Fits type name
        /// </summary>
        [NonSerialized]
        private string name;

        /// <summary>
        /// Corresponding .Net type
        /// </summary>
        [NonSerialized]
        private Type type;

        /// <summary>
        /// One character code of data type
        /// </summary>
        [NonSerialized]
        private char code;

        /// <summary>
        /// Repeat value
        /// </summary>
        [NonSerialized]
        private int repeat;

        /// <summary>
        /// Size of the primitive type in bytes
        /// </summary>
        [NonSerialized]
        private int byteSize;

        private Int32? nullValue;
        private double? scale;
        private double? zero;
        private string dimensions;

        #endregion
        #region Properties

        /// <summary>
        /// Gets or sets the name of the data type.
        /// </summary>
        [DataMember]
        public string Name
        {
            get { return name; }
            internal set { name = value; }
        }

        /// <summary>
        /// Gets the corresponding .Net type
        /// </summary>
        [IgnoreDataMember]
        public Type Type
        {
            get { return type; }
            internal set { type = value; }
        }

        public Char Code
        {
            get { return code; }
            internal set { code = value; }
        }

        /// <summary>
        /// Gets or sets the repeat value of the data type.
        /// </summary>
        [DataMember]
        public int Repeat
        {
            get { return repeat; }
            set { repeat = value; }
        }

        /// <summary>
        /// Gets the size of the primitive type in bytes
        /// </summary>
        [DataMember]
        public int ByteSize
        {
            get { return byteSize; }
            internal set { byteSize = value; }
        }

        [IgnoreDataMember]
        public int TotalBytes
        {
            get { return byteSize * repeat; }
        }

        /// <summary>
        /// Gets or sets the value indicating null.
        /// </summary>
        /// <remarks>
        /// Corresponds to the TNULLn keyword
        /// </remarks>
        public Int32? NullValue
        {
            get { return nullValue; }
            set { nullValue = value; }
        }

        /// <summary>
        /// Gets or sets the scale of the column.
        /// </summary>
        /// <remarks>
        /// Corresponds to the TSCALn keyword
        /// </remarks>
        public double? Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        /// <summary>
        /// Gets or sets the zero offset of the column.
        /// </summary>
        /// <remarks>
        /// Corresponds to the TZEROn keyword
        /// </remarks>
        public double? Zero
        {
            get { return zero; }
            set { zero = value; }
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        public string Dimensions
        {
            get { return dimensions; }
            set { dimensions = value; }
        }

        #endregion
        #region Constructors and initializers

        internal FitsDataType()
        {
            InitializeMembers();
        }

        public FitsDataType(FitsDataType old)
        {
            CopyMembers(old);
        }

        private void InitializeMembers()
        {
            this.name = null;
            this.type = null;
            this.repeat = 1;
            this.byteSize = 0;
            this.nullValue = null;
            this.scale = null;
            this.zero = null;
            this.dimensions = null;
        }

        private void CopyMembers(FitsDataType old)
        {
            this.name = old.name;
            this.type = old.type;
            this.repeat = old.repeat;
            this.byteSize = old.byteSize;
            this.nullValue = old.nullValue;
            this.scale = old.scale;
            this.zero = old.zero;
            this.dimensions = old.dimensions;
        }

        public object Clone()
        {
            return new FitsDataType(this);
        }

        #endregion

        internal static FitsDataType CreateFromTForm(string tform)
        {
            char type;
            int repeat;
            GetFitsDataTypeDetails(tform, out type, out repeat);

            FitsDataType dt;
            switch (char.ToUpperInvariant(type))
            {
                case 'L':
                    dt = FitsDataTypes.Logical;
                    break;
                case 'X':
                    dt = FitsDataTypes.Bit;           // *** This is union bit!
                    break;
                case 'B':
                    dt = FitsDataTypes.Byte;
                    break;
                case 'I':
                    dt = FitsDataTypes.Int16;
                    break;
                case 'J':
                    dt = FitsDataTypes.Int32;
                    break;
                case 'K':
                    dt = FitsDataTypes.Int64;
                    break;
                case 'A':
                    dt = FitsDataTypes.Char;
                    break;
                case 'E':
                    dt = FitsDataTypes.Single;
                    break;
                case 'D':
                    dt = FitsDataTypes.Double;
                    break;
                case 'C':
                    dt = FitsDataTypes.SingleComplex;
                    break;
                case 'M':
                    dt = FitsDataTypes.DoubleComplex;
                    break;
                case 'P': // Array, not implemented
                default:
                    throw new NotImplementedException();
            }

            dt.Repeat = repeat;

            return dt;
        }

        private static void GetFitsDataTypeDetails(string tform, out char type, out int repeat)
        {
            var m = FormatRegex.Match(tform);

            if (!m.Success)
            {
                throw new FitsException("Invalid column type descriptor.");      // *** TODO
            }

            type = m.Groups[2].Value.ToUpper()[0];

            // Item repeated a number of times
            var repeatstr = m.Groups[1].Value;
            if (!String.IsNullOrEmpty(repeatstr))
            {
                repeat = int.Parse(repeatstr, FitsFile.Culture);
            }
            else
            {
                repeat = 1;
            }
        }

        public string GetTFormString()
        {
            return repeat.ToString(FitsFile.Culture) + code;
        }
    }
}
