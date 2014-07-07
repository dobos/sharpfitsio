using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq.Expressions;

namespace Jhu.SharpFitsIO
{
    /// <summary>
    /// Implements function to read and write FITS binary tables in a streaming fashion.
    /// </summary>
    /// <remarks>
    /// The class does not provide storage for table data, it only reads and writes from disk.
    /// </remarks>
    public class BinaryTableHdu : SimpleHdu, ICloneable
    {
        private delegate int BinaryReaderDelegate(BitConverterBase converter, FitsTableColumn column, byte[] buffer, int startIndex, out object value);
        private delegate int BinaryWriterDelegate(BitConverterBase converter, FitsTableColumn column, byte[] buffer, int startIndex, object value);

        #region Private member variables

        /// <summary>
        /// Holds binary reader delegates associated with each column
        /// </summary>
        [NonSerialized]
        private BinaryReaderDelegate[] columnByteReaders;

        /// <summary>
        /// Holds binary writer delegates associates with each column
        /// </summary>
        [NonSerialized]
        private BinaryWriterDelegate[] columnByteWriters;

        /// <summary>
        /// Collection of table columns
        /// </summary>
        [NonSerialized]
        private List<FitsTableColumn> columns;

        #endregion
        #region Properties

        /// <summary>
        /// Gets the collection containing columns of the data file
        /// </summary>
        [IgnoreDataMember]
        public ReadOnlyCollection<FitsTableColumn> Columns
        {
            get
            {
                return new ReadOnlyCollection<FitsTableColumn>(columns);
            }
        }

        #endregion
        #region Keyword accessor properties

        /// <summary>
        /// Gets the number of columns, a value based on the headers.
        /// </summary>
        public int ColumnCount
        {
            get { return Cards[Constants.FitsKeywordTFields].GetInt32(); }
            private set
            {
                EnsureModifiable();

                var card = new Card(Constants.FitsKeywordTFields);
                card.SetValue(value);
                Cards.Set(card);
            }
        }

        /// <summary>
        /// Gets or sets the number of rows in the table.
        /// </summary>
        public int RowCount
        {
            get { return GetAxisLength(2); }
            set { SetAxisLength(2, value); }
        }

        /// <summary>
        /// Gets or sets the offset of the heap storage area. Not used.
        /// </summary>
        public int HeapOffset
        {
            get { return Cards[Constants.FitsKeywordTHeap].GetInt32(); }
        }

        /// <summary>
        /// Gets or sets the number of parameters. Not used.
        /// </summary>
        public int ParameterCount
        {
            get { return Cards[Constants.FitsKeywordPCount].GetInt32(); }
            set
            {
                EnsureModifiable();

                var card = new Card(Constants.FitsKeywordPCount);
                card.SetValue(value);
                Cards.Set(card);
            }
        }

        /// <summary>
        /// Gets or sets the number of data groups. Not used.
        /// </summary>
        public int GroupCount
        {
            get { return Cards[Constants.FitsKeywordGCount].GetInt32(); }
            set
            {
                EnsureModifiable();

                var card = new Card(Constants.FitsKeywordGCount);
                card.SetValue(value);
                Cards.Set(card);
            }
        }

        #endregion
        #region Constructors and initializers

        /// <summary>
        /// Creates and initializes a new binary table associated with a FITS file.
        /// </summary>
        /// <param name="fits"></param>
        internal BinaryTableHdu(FitsFile fits)
            : base(fits)
        {
            InitializeMembers();
        }

        /// <summary>
        /// Creates and initializes a new binary table based on a simple HDU.
        /// </summary>
        /// <param name="hdu"></param>
        /// <remarks>
        /// Used during automatic HDU recognition.
        /// </remarks>
        internal BinaryTableHdu(SimpleHdu hdu)
            : base(hdu)
        {
            InitializeMembers();

            DetectColumns();
        }

        /// <summary>
        /// Creates a copy of an existing binary table without copying data.
        /// </summary>
        /// <param name="old"></param>
        internal BinaryTableHdu(BinaryTableHdu old)
            : base(old)
        {
            CopyMembers(old);
        }

        /// <summary>
        /// Initializes member variables
        /// </summary>
        private void InitializeMembers()
        {
            this.columnByteReaders = null;
            this.columnByteWriters = null;
            this.columns = new List<FitsTableColumn>();
        }

        /// <summary>
        /// Copies member variables
        /// </summary>
        /// <param name="old"></param>
        private void CopyMembers(BinaryTableHdu old)
        {
            this.columnByteReaders = null;
            this.columnByteWriters = null;
            this.columns = new List<FitsTableColumn>();
            foreach (var columns in old.columns)
            {
                this.columns.Add((FitsTableColumn)columns.Clone());
            }
        }

        /// <summary>
        /// Creates a deep clone of the object.
        /// </summary>
        /// <returns></returns>
        public override object Clone()
        {
            return new BinaryTableHdu(this);
        }

        #endregion
        #region Static create functions

        /// <summary>
        /// Creates a new binary table associated with a FITS file. Optionally
        /// initializes headers.
        /// </summary>
        /// <param name="fits"></param>
        /// <param name="initializeHeaders"></param>
        /// <returns></returns>
        public static BinaryTableHdu Create(FitsFile fits, bool initializeHeaders)
        {
            var hdu = new BinaryTableHdu(fits);

            if (initializeHeaders)
            {
                hdu.InitializeCards(false, false);
            }

            return hdu;
        }

        #endregion
        #region Card functions

        /// <summary>
        /// Initializes header cards to their default values.
        /// </summary>
        /// <param name="primary"></param>
        /// <param name="hasExtension"></param>
        protected override void InitializeCards(bool primary, bool hasExtension)
        {
            base.InitializeCards(primary, hasExtension);

            Extension = Constants.FitsExtensionBinTable;
            AxisCount = 2;
            SetAxisLength(1, 0);
            SetAxisLength(2, 0);

            ParameterCount = 0;
            GroupCount = 1;

            //ExtensionName = "UNNAMED";
        }

        #endregion
        #region Column functions

        /// <summary>
        /// Returns the length of a single row in bytes.
        /// </summary>
        /// <returns></returns>
        public override int GetStrideLength()
        {
            int res = 0;

            for (int i = 0; i < columns.Count; i++)
            {
                res += columns[i].DataType.TotalBytes;
            }

            return res;
        }

        /// <summary>
        /// Detects columns from header cards.
        /// </summary>
        private void DetectColumns()
        {
            columns.Clear();

            // Loop though header cards and 
            for (int i = 0; i < ColumnCount; i++)
            {
                // FITS column indexes are 1-based!
                columns.Add(FitsTableColumn.CreateFromCards(this, i + 1));
            }

            InitializeColumnBinaryReaders();

            // Verify size
            if (GetAxisLength(1) != GetStrideLength())
            {
                throw new Exception();  // *** TODO
            }
        }

        /// <summary>
        /// Creates columns from a list of predefined columns
        /// </summary>
        /// <param name="columns"></param>
        public void CreateColumns(IList<FitsTableColumn> columns)
        {
            this.columns.Clear();
            // TODO: delete all header column cards

            ColumnCount = columns.Count;

            for (int i = 0; i < columns.Count; i++)
            {
                // Set column index
                columns[i].ID = i + 1;

                // Create header cards
                columns[i].SetCards(this);

                // Append column
                this.columns.Add(columns[i]);
            }

            // Set stride size
            SetAxisLength(1, GetStrideLength());
        }

        /// <summary>
        /// Creates columns from the fields of a structure
        /// </summary>
        /// <param name="structType"></param>
        public void CreateColumns(Type structType)
        {
            // Enumerate fields and generate columns
            var fields = structType.GetFields();
            var columns = new FitsTableColumn[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType.IsValueType)
                {
                    // Primitive types are written as is
                    columns[i] = FitsTableColumn.Create(
                        fields[i].Name,
                        FitsDataType.Create(fields[i].FieldType));
                }
                else if (fields[i].FieldType == typeof(string) || fields[i].FieldType.IsArray)
                {
                    // Only fixed-length arrays are supported, figure out size
                    // from the MarshalAs attribute

                    Type type;
                    int repeat;

                    if (!GetFieldArraySize(fields[i], out type, out repeat))
                    {
                        throw new InvalidOperationException("Array size not specified");  // TODO
                    }

                    columns[i] = FitsTableColumn.Create(
                        fields[i].Name,
                        FitsDataType.Create(type, repeat));
                }
                else
                {
                    throw new InvalidOperationException("Unsupported data type");  // TODO
                }
            }

            CreateColumns(columns);
        }

        /// <summary>
        /// Returns the size of a field of a struct in bytes, based on marshaling attributes.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="type"></param>
        /// <param name="repeat"></param>
        /// <returns></returns>
        private bool GetFieldArraySize(FieldInfo field, out Type type, out int repeat)
        {
            var attr = field.GetCustomAttributes(typeof(MarshalAsAttribute), false);

            if (attr.Length == 1)
            {
                if (field.FieldType == typeof(string))
                {
                    type = typeof(char);
                }
                else
                {
                    type = field.FieldType.GetElementType();
                }

                repeat = ((MarshalAsAttribute)attr[0]).SizeConst;

                return true;
            }
            else
            {
                type = null;
                repeat = 0;
                return false;
            }
        }

        /// <summary>
        /// Initializes binary reader delegates for each column.
        /// </summary>
        private void InitializeColumnBinaryReaders()
        {
            columnByteReaders = new BinaryReaderDelegate[Columns.Count];

            for (int i = 0; i < columnByteReaders.Length; i++)
            {
                columnByteReaders[i] = CreateBinaryReaderDelegate(Columns[i]);
            }
        }

        /// <summary>
        /// Initializes binary writer delegates for each column.
        /// </summary>
        private void InitializeColumnBinaryWriters()
        {
            if (columnByteWriters == null)
            {
                columnByteWriters = new BinaryWriterDelegate[Columns.Count];

                for (int i = 0; i < columnByteWriters.Length; i++)
                {
                    columnByteWriters[i] = CreateBinaryWriterDelegate(Columns[i]);
                }
            }
        }

        #endregion

        /// <summary>
        /// Reads the next row from the binary table into an array of objects.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool ReadNextRow(object[] values)
        {
            if (HasMoreStrides)
            {
                ReadStride();

                int startIndex = 0;
                for (int i = 0; i < Columns.Count; i++)
                {
                    var res = columnByteReaders[i](
                                Fits.BitConverter,
                                Columns[i],
                                StrideBuffer,
                                startIndex,
                                out values[i]);

                    startIndex += res;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Writes the next row into a binary table. Parameter types must match columns.
        /// </summary>
        /// <param name="values"></param>
        public void WriteNextRow(params object[] values)
        {
            if (StrideBuffer == null)
            {
                CreateStrideBuffer();
            }

            InitializeColumnBinaryWriters();

            int startIndex = 0;
            for (int i = 0; i < Columns.Count; i++)
            {
                var res = columnByteWriters[i](
                    Fits.BitConverter,
                    Columns[i],
                    StrideBuffer,
                    startIndex,
                    values[i]);

                startIndex += res;
            }

            WriteStride();
        }

        #region Read delegate generator functions

        /// <summary>
        /// Returns a delegate to read the specific column into a boxed 
        /// strongly typed variable
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private BinaryReaderDelegate CreateBinaryReaderDelegate(FitsTableColumn column)
        {
            // Complex types firts, then scalar and arrays
            if (column.DataType.Type == typeof(String))
            {
                return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                {
                    value = Encoding.ASCII.GetString(bytes, startIndex, col.DataType.Repeat);
                    return col.DataType.Repeat;
                };
            }
            else if (column.DataType.Repeat == 1)
            {
                // Scalars
                if (column.DataType.Type == typeof(Boolean))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Boolean val;
                        var res = converter.ToBoolean(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(SByte))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        SByte val;
                        var res = converter.ToSByte(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Byte))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Byte val;
                        var res = converter.ToByte(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Char))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Char val;
                        var res = converter.ToChar(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Int16))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Int16 val;
                        var res = converter.ToInt16(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(UInt16))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        UInt16 val;
                        var res = converter.ToUInt16(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Int32))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Int32 val;
                        var res = converter.ToInt32(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(UInt32))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        UInt32 val;
                        var res = converter.ToUInt32(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Int64))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Int64 val;
                        var res = converter.ToInt64(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(UInt64))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        UInt64 val;
                        var res = converter.ToUInt64(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Single))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Single val;
                        var res = converter.ToSingle(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Double))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Double val;
                        var res = converter.ToDouble(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(SingleComplex))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        SingleComplex val;
                        var res = converter.ToSingleComplex(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(DoubleComplex))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        DoubleComplex val;
                        var res = converter.ToDoubleComplex(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                // Arrays
                if (column.DataType.Type == typeof(Boolean))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Boolean[] val;
                        var res = converter.ToBoolean(bytes, startIndex, col.DataType.Repeat, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(SByte))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        SByte[] val;
                        var res = converter.ToSByte(bytes, startIndex, col.DataType.Repeat, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Byte))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Byte[] val;
                        var res = converter.ToByte(bytes, startIndex, col.DataType.Repeat, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Char))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Char[] val;
                        var res = converter.ToChar(bytes, startIndex, col.DataType.Repeat, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Int16))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Int16[] val;
                        var res = converter.ToInt16(bytes, startIndex, col.DataType.Repeat, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(UInt16))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        UInt16[] val;
                        var res = converter.ToUInt16(bytes, startIndex, col.DataType.Repeat, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Int32))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Int32[] val;
                        var res = converter.ToInt32(bytes, startIndex, col.DataType.Repeat, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(UInt32))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        UInt32[] val;
                        var res = converter.ToUInt32(bytes, startIndex, col.DataType.Repeat, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Int64))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Int64[] val;
                        var res = converter.ToInt64(bytes, startIndex, col.DataType.Repeat, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(UInt64))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        UInt64[] val;
                        var res = converter.ToUInt64(bytes, startIndex, col.DataType.Repeat, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Single))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Single[] val;
                        var res = converter.ToSingle(bytes, startIndex, col.DataType.Repeat, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Double))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        Double[] val;
                        var res = converter.ToDouble(bytes, startIndex, col.DataType.Repeat, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(SingleComplex))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        SingleComplex[] val;
                        var res = converter.ToSingleComplex(bytes, startIndex, col.DataType.Repeat, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(DoubleComplex))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, out object value)
                    {
                        DoubleComplex[] val;
                        var res = converter.ToDoubleComplex(bytes, startIndex, col.DataType.Repeat, out val);
                        value = val;
                        return res;
                    };
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }


        #endregion
        #region Write delegate generator functions

        /// <summary>
        /// Returns a delegate to write a boxed, strongly typed variable into
        /// binary format.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private BinaryWriterDelegate CreateBinaryWriterDelegate(FitsTableColumn column)
        {
            // Complex types firts, then scalar and arrays
            if (column.DataType.Type == typeof(String))
            {
                return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                {
                    Encoding.ASCII.GetBytes((string)value, 0, col.DataType.Repeat, bytes, startIndex);
                    return col.DataType.Repeat;
                };
            }
            else if (column.DataType.Repeat == 1)
            {
                // Scalars
                if (column.DataType.Type == typeof(Boolean))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((Boolean)value, bytes, startIndex);
                    };
                }
                else if (column.DataType.Type == typeof(SByte))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((SByte)value, bytes, startIndex);
                    };
                }
                else if (column.DataType.Type == typeof(Byte))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((Byte)value, bytes, startIndex);
                    };
                }
                else if (column.DataType.Type == typeof(Char))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((Char)value, bytes, startIndex);
                    };
                }
                else if (column.DataType.Type == typeof(Int16))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((Int16)value, bytes, startIndex);
                    };
                }
                else if (column.DataType.Type == typeof(UInt16))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((UInt16)value, bytes, startIndex);
                    };
                }
                else if (column.DataType.Type == typeof(Int32))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((Int32)value, bytes, startIndex);
                    };
                }
                else if (column.DataType.Type == typeof(UInt32))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((UInt32)value, bytes, startIndex);
                    };
                }
                else if (column.DataType.Type == typeof(Int64))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((Int64)value, bytes, startIndex);
                    };
                }
                else if (column.DataType.Type == typeof(UInt64))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((UInt64)value, bytes, startIndex);
                    };
                }
                else if (column.DataType.Type == typeof(Single))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((Single)value, bytes, startIndex);
                    };
                }
                else if (column.DataType.Type == typeof(Double))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((Double)value, bytes, startIndex);
                    };
                }
                else if (column.DataType.Type == typeof(SingleComplex))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((SingleComplex)value, bytes, startIndex);
                    };
                }
                else if (column.DataType.Type == typeof(DoubleComplex))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((DoubleComplex)value, bytes, startIndex);
                    };
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                // Arrays
                if (column.DataType.Type == typeof(Boolean))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((Boolean[])value, bytes, startIndex, col.DataType.Repeat);
                    };
                }
                else if (column.DataType.Type == typeof(SByte))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((SByte[])value, bytes, startIndex, col.DataType.Repeat);
                    };
                }
                else if (column.DataType.Type == typeof(Byte))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((Byte[])value, bytes, startIndex, col.DataType.Repeat);
                    };
                }
                else if (column.DataType.Type == typeof(Char))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        if (value is String)
                        {
                            return converter.GetBytes((String)value, bytes, startIndex, col.DataType.Repeat);
                        }
                        else
                        {
                            return converter.GetBytes((Char[])value, bytes, startIndex, col.DataType.Repeat);
                        }
                    };
                }
                else if (column.DataType.Type == typeof(Int16))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((Int16[])value, bytes, startIndex, col.DataType.Repeat);
                    };
                }
                else if (column.DataType.Type == typeof(UInt16))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((UInt16[])value, bytes, startIndex, col.DataType.Repeat);
                    };
                }
                else if (column.DataType.Type == typeof(Int32))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((Int32[])value, bytes, startIndex, col.DataType.Repeat);
                    };
                }
                else if (column.DataType.Type == typeof(UInt32))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((UInt32[])value, bytes, startIndex, col.DataType.Repeat);
                    };
                }
                else if (column.DataType.Type == typeof(Int64))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((Int64[])value, bytes, startIndex, col.DataType.Repeat);
                    };
                }
                else if (column.DataType.Type == typeof(UInt64))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((UInt64[])value, bytes, startIndex, col.DataType.Repeat);
                    };
                }
                else if (column.DataType.Type == typeof(Single))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((Single[])value, bytes, startIndex, col.DataType.Repeat);
                    };
                }
                else if (column.DataType.Type == typeof(Double))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((Double[])value, bytes, startIndex, col.DataType.Repeat);
                    };
                }
                else if (column.DataType.Type == typeof(SingleComplex))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((SingleComplex[])value, bytes, startIndex, col.DataType.Repeat);
                    };
                }
                else if (column.DataType.Type == typeof(DoubleComplex))
                {
                    return delegate(BitConverterBase converter, FitsTableColumn col, byte[] bytes, int startIndex, object value)
                    {
                        return converter.GetBytes((DoubleComplex[])value, bytes, startIndex, col.DataType.Repeat);
                    };
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        #endregion
    }
}
