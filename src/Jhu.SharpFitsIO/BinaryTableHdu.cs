using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Linq.Expressions;

namespace Jhu.SharpFitsIO
{
    public class BinaryTableHdu : SimpleHdu, ICloneable
    {
        // TODO: cache compiled lambdas to prevent leaks

        private delegate int ByteReaderDelegate(BitConverterBase converter, byte[] buffer, int startIndex, int count, out object value);
        private delegate int ByteWriterDelegate(BitConverterBase converter, byte[] buffer, int startIndex, object value, FitsTableColumn column);
        private delegate int StructWriterDelegate(BitConverterBase converter, byte[] buffer, int startIndex, object value);

        #region Private member variables

        [NonSerialized]
        private ByteReaderDelegate[] columnByteReaders;

        [NonSerialized]
        private ByteWriterDelegate[] columnByteWriters;

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

        public int RowCount
        {
            get { return GetAxisLength(2); }
            set { SetAxisLength(2, value); }
        }

        public int HeapOffset
        {
            get { return Cards[Constants.FitsKeywordTHeap].GetInt32(); }
        }

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

        internal BinaryTableHdu(FitsFile fits)
            : base(fits)
        {
            InitializeMembers();
        }

        internal BinaryTableHdu(SimpleHdu hdu)
            : base(hdu)
        {
            InitializeMembers();

            DetectColumns();
        }

        private BinaryTableHdu(BinaryTableHdu old)
            : base(old)
        {
            CopyMembers(old);
        }

        private void InitializeMembers()
        {
            this.columnByteReaders = null;
            this.columnByteWriters = null;
            this.columns = new List<FitsTableColumn>();
        }

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

        public override object Clone()
        {
            return new BinaryTableHdu(this);
        }

        #endregion
        #region Static create functions

        public static BinaryTableHdu Create(FitsFile fits, bool initialize)
        {
            var hdu = new BinaryTableHdu(fits);

            if (initialize)
            {
                hdu.InitializeCards(false, false);
            }

            return hdu;
        }

        #endregion
        #region Card functions

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

            InitializeColumnByteReaders();

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
                columns[i] = FitsTableColumn.Create(fields[i].Name, FitsDataType.Create(fields[i].FieldType));
            }

            CreateColumns(columns);
        }

        private void InitializeColumnByteReaders()
        {
            columnByteReaders = new ByteReaderDelegate[Columns.Count];

            for (int i = 0; i < columnByteReaders.Length; i++)
            {
                columnByteReaders[i] = CreateByteReaderDelegate(Columns[i]);
            }
        }

        private void InitializeColumnByteWriters()
        {
            if (columnByteWriters == null)
            {
                columnByteWriters = new ByteWriterDelegate[Columns.Count];

                for (int i = 0; i < columnByteWriters.Length; i++)
                {
                    columnByteWriters[i] = CreateByteWriterDelegate(Columns[i]);
                }
            }
        }

        private void InitializeColumnByteWriters(Type structType)
        {
            if (columnByteWriters == null)
            {
                columnByteWriters = new ByteWriterDelegate[Columns.Count];

                for (int i = 0; i < columnByteWriters.Length; i++)
                {
                    columnByteWriters[i] = CreateStructWriterDelegate(structType, Columns[i]);
                }
            }
        }

        #endregion

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
                                StrideBuffer,
                                startIndex,
                                columns[i].DataType.TotalBytes,
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

        public bool ReadNextRow<T>(out T values)
            where T : struct
        {
            // TODO

            values = default(T);

            return false;
        }

        public void WriteNextRow(object[] values)
        {
            if (StrideBuffer == null)
            {
                CreateStrideBuffer();
            }

            InitializeColumnByteWriters();

            int startIndex = 0;
            for (int i = 0; i < Columns.Count; i++)
            {
                var res = columnByteWriters[i](
                    Fits.BitConverter,
                    StrideBuffer,
                    startIndex,
                    values[i],
                    Columns[i]);

                startIndex += res;
            }

            WriteStride();
        }

        public void WriteNextRow<T>(T values)
            where T : struct
        {
            if (StrideBuffer == null)
            {
                CreateStrideBuffer();
            }

            InitializeColumnByteWriters(typeof(T));

            int startIndex = 0;
            for (int i = 0; i < Columns.Count; i++)
            {
                var res = columnByteWriters[i](
                    Fits.BitConverter,
                    StrideBuffer,
                    startIndex,
                    values,
                    Columns[i]);

                startIndex += res;
            }

            WriteStride();
        }

        /// <summary>
        /// Returns a delegate to read the specific column into a boxed 
        /// strongly typed variable
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private ByteReaderDelegate CreateByteReaderDelegate(FitsTableColumn column)
        {
            // Complex types firts, then scalar and arrays
            if (column.DataType.Type == typeof(String))
            {
                return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                {
                    value = Encoding.ASCII.GetString(bytes, startIndex, count);
                    return count;
                };
            }
            else if (column.DataType.Repeat == 1)
            {
                // Scalars
                if (column.DataType.Type == typeof(Boolean))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Boolean val;
                        var res = converter.ToBoolean(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(SByte))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        SByte val;
                        var res = converter.ToSByte(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Byte))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Byte val;
                        var res = converter.ToByte(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Char))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Char val;
                        var res = converter.ToChar(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Int16))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Int16 val;
                        var res = converter.ToInt16(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(UInt16))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        UInt16 val;
                        var res = converter.ToUInt16(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Int32))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Int32 val;
                        var res = converter.ToInt32(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(UInt32))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        UInt32 val;
                        var res = converter.ToUInt32(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Int64))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Int64 val;
                        var res = converter.ToInt64(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(UInt64))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        UInt64 val;
                        var res = converter.ToUInt64(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Single))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Single val;
                        var res = converter.ToSingle(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Double))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Double val;
                        var res = converter.ToDouble(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(SingleComplex))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        SingleComplex val;
                        var res = converter.ToSingleComplex(bytes, startIndex, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(DoubleComplex))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
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
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Boolean[] val;
                        var res = converter.ToBoolean(bytes, startIndex, count, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(SByte))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        SByte[] val;
                        var res = converter.ToSByte(bytes, startIndex, count, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Byte))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Byte[] val;
                        var res = converter.ToByte(bytes, startIndex, count, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Char))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Char[] val;
                        var res = converter.ToChar(bytes, startIndex, count, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Int16))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Int16[] val;
                        var res = converter.ToInt16(bytes, startIndex, count, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(UInt16))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        UInt16[] val;
                        var res = converter.ToUInt16(bytes, startIndex, count, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Int32))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Int32[] val;
                        var res = converter.ToInt32(bytes, startIndex, count, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(UInt32))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        UInt32[] val;
                        var res = converter.ToUInt32(bytes, startIndex, count, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Int64))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Int64[] val;
                        var res = converter.ToInt64(bytes, startIndex, count, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(UInt64))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        UInt64[] val;
                        var res = converter.ToUInt64(bytes, startIndex, count, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Single))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Single[] val;
                        var res = converter.ToSingle(bytes, startIndex, count, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(Double))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        Double[] val;
                        var res = converter.ToDouble(bytes, startIndex, count, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(SingleComplex))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        SingleComplex[] val;
                        var res = converter.ToSingleComplex(bytes, startIndex, count, out val);
                        value = val;
                        return res;
                    };
                }
                else if (column.DataType.Type == typeof(DoubleComplex))
                {
                    return delegate(BitConverterBase converter, byte[] bytes, int startIndex, int count, out object value)
                    {
                        DoubleComplex[] val;
                        var res = converter.ToDoubleComplex(bytes, startIndex, count, out val);
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

        /// <summary>
        /// Generates code to write a specific type of column
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private ByteWriterDelegate CreateByteWriterDelegate(FitsTableColumn column, ParameterExpression value, Expression unbox)
        {
            // The generated code is approximately the following:

            // return delegate(BitConverterBase converter, byte[] bytes, int startIndex, object value, FitsTableColumn col)
            // {
            //    var val = (Boolean)value;
            //    return converter.GetBytes(val, bytes, startIndex);
            //};

            // Delegate type
            var delegateType = typeof(ByteWriterDelegate);

            // Function parameters
            var converter = Expression.Parameter(typeof(BitConverterBase), "converter");
            var bytes = Expression.Parameter(typeof(byte[]), "bytes");
            var startIndex = Expression.Parameter(typeof(int), "startIndex");
            var col = Expression.Parameter(typeof(FitsTableColumn), "column");

            var getBytesMethod = typeof(BitConverterBase).GetMethod(
                "GetBytes",
                new[] {
                    column.DataType.Type,
                    typeof(byte[]),
                    typeof(int)
                });

            var fc = Expression.Call(
                converter,
                getBytesMethod,
                unbox,
                bytes,
                startIndex);

            var lambda = Expression.Lambda(
                delegateType,
                fc,
                new ParameterExpression[]
                { 
                    converter,
                    bytes,
                    startIndex,
                    value,
                    col
                });

            var fn = lambda.Compile();

            return (ByteWriterDelegate)fn;
        }

        private ByteWriterDelegate CreateByteWriterDelegate(FitsTableColumn column)
        {
            var value = Expression.Parameter(typeof(object), "value");
            var unbox = Expression.Unbox(value, column.DataType.Type);

            return CreateByteWriterDelegate(column, value, unbox);
        }

        private ByteWriterDelegate CreateStructWriterDelegate(Type structType, FitsTableColumn column)
        {
            var value = Expression.Parameter(typeof(object), "value");
            var unbox = Expression.Unbox(value, structType);
            var field = Expression.Field(unbox, column.Name);

            return CreateByteWriterDelegate(column, value, field);
        }
    }
}
