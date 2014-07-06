using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jhu.SharpFitsIO
{
    [TestClass]
    public class BinaryTableHduTest : FitsTestBase
    {
        private void CreateFitsFileWithTable(out FitsFile fits, out BinaryTableHdu tab)
        {
            fits = CreateFitsFile();

            // Primary header
            var prim = SimpleHdu.Create(fits, true, true, true);
            prim.WriteHeader();

            // Table
            tab = BinaryTableHdu.Create(fits, true);
        }

        struct ScalarsOnlyStruct
        {
            public Boolean data0;
            public Byte data1;
            public Int16 data2;
            public Int32 data3;
            public Int64 data4;
            public Char data5;
            public Single data6;
            public Double data7;
            public SingleComplex data8;
            public DoubleComplex data9;

            public ScalarsOnlyStruct(int init)
            {
                data0 = true;
                data1 = (byte)(1 * init);
                data2 = (short)(2 * init);
                data3 = 3 * init;
                data4 = 4 * init;
                data5 = '5';
                data6 = 6 * init;
                data7 = 7 * init;
                data8 = new SingleComplex(8 * init, 8 * init);
                data9 = new DoubleComplex(9 * init, 9 * init);
            }
        }

        [TestMethod]
        public void TestColumnsFromStruct_ScalarOnly()
        {
            FitsFile fits;
            BinaryTableHdu tab;

            CreateFitsFileWithTable(out fits, out tab);

            tab.CreateColumns(typeof(ScalarsOnlyStruct));
            tab.RowCount = 3;

            Assert.AreEqual(10, tab.Columns.Count);
            Assert.AreEqual(53, tab.GetAxisLength(1));
            Assert.AreEqual(3, tab.GetAxisLength(2));

            tab.WriteHeader();

            // Now try to write some data
            tab.WriteNextRow(new ScalarsOnlyStruct(1));
            tab.WriteNextRow(new ScalarsOnlyStruct(2));
            tab.WriteNextRow(new ScalarsOnlyStruct(3));
        }

        [TestMethod]
        public void TestColumns_ScalarOnly()
        {
            FitsFile fits;
            BinaryTableHdu tab;

            CreateFitsFileWithTable(out fits, out tab);

            var cols = new FitsTableColumn[]
            {
                FitsTableColumn.Create("col0", FitsDataTypes.Logical),
                FitsTableColumn.Create("col1", FitsDataTypes.Byte),
                FitsTableColumn.Create("col2", FitsDataTypes.Int16),
                FitsTableColumn.Create("col3", FitsDataTypes.Int32),
                FitsTableColumn.Create("col4", FitsDataTypes.Int64),
                FitsTableColumn.Create("col5", FitsDataTypes.Char),
                FitsTableColumn.Create("col6", FitsDataTypes.Single),
                FitsTableColumn.Create("col7", FitsDataTypes.Double),
                FitsTableColumn.Create("col8", FitsDataTypes.SingleComplex),
                FitsTableColumn.Create("col9", FitsDataTypes.DoubleComplex),
            };

            tab.CreateColumns(cols);
            tab.RowCount = 3;

            Assert.AreEqual(10, tab.Columns.Count);
            Assert.AreEqual(53, tab.GetAxisLength(1));
            Assert.AreEqual(3, tab.GetAxisLength(2));

            tab.WriteHeader();

            // Now try to write some data
            for (int init = 1; init <= 3; init++)
            {
                tab.WriteNextRow(
                    true,
                    (byte)(1 * init),
                    (short)(2 * init),
                    3 * init,
                    (long)(4 * init),
                    '5',
                    (float)(6 * init),
                    (double)(7 * init),
                    new SingleComplex(8 * init, 8 * init),
                    new DoubleComplex(9 * init, 9 * init));
            }
        }

        struct StringStruct
        {
            public Int32 data0;
            public Char data1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public Char[] data2;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public String data3;

            public StringStruct(int init)
            {
                data0 = init;
                data1 = (char)('A' + init);
                data2 = new char[] { (char)('C' + init), (char)('D' + init), (char)('E' + init) };
                data3 = new String((char)('F' + init), 5);
            }
        }

        [TestMethod]
        public void TestColumnsFromStruct_String()
        {
            FitsFile fits;
            BinaryTableHdu tab;

            CreateFitsFileWithTable(out fits, out tab);

            tab.CreateColumns(typeof(StringStruct));
            tab.RowCount = 3;

            Assert.AreEqual(4, tab.Columns.Count);
            Assert.AreEqual(13, tab.GetAxisLength(1));
            Assert.AreEqual(3, tab.GetAxisLength(2));

            tab.WriteHeader();

            // Now try to write some data
            tab.WriteNextRow(new StringStruct(1));
            tab.WriteNextRow(new StringStruct(2));
            tab.WriteNextRow(new StringStruct(3));
        }

        [TestMethod]
        public void TestColumns_String()
        {
            FitsFile fits;
            BinaryTableHdu tab;

            CreateFitsFileWithTable(out fits, out tab);

            var cols = new FitsTableColumn[]
            {
                FitsTableColumn.Create("col3", FitsDataTypes.Int32),
                FitsTableColumn.Create("col5", FitsDataTypes.Char),
                FitsTableColumn.Create("col6", FitsDataType.Create(typeof(char), 3)),
            };

            tab.CreateColumns(cols);
            tab.RowCount = 3;

            Assert.AreEqual(3, tab.Columns.Count);
            Assert.AreEqual(8, tab.GetAxisLength(1));
            Assert.AreEqual(3, tab.GetAxisLength(2));

            tab.WriteHeader();

            // Now try to write some data
            tab.WriteNextRow(1, 'A', "ABC".ToCharArray());
            tab.WriteNextRow(2, 'X', "QWE".ToCharArray());
            tab.WriteNextRow(3, 'Y', "ASD".ToCharArray());
        }

        struct ArraysStruct
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Boolean[] data0;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public Byte[] data1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public Int16[] data2;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public Int32[] data3;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public Int64[] data4;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public Char[] data5;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public Single[] data6;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public Double[] data7;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public SingleComplex[] data8;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public DoubleComplex[] data9;

            public ArraysStruct(int init)
            {
                data0 = new bool[] { true, false };
                data1 = new byte[] { (byte)init, (byte)(2 * init), (byte)(3 * init) };
                data2 = new short[] { (short)init, (short)(2 * init), (short)(3 * init) };
                data3 = new int[] { 3 * init, 4 * init, 5 * init };
                data4 = new long[] { 4 * init, 5 * init, 6 * init };
                data5 = "789".ToCharArray();
                data6 = new float[] { 6 * init, 7 * init, 8 * init };
                data7 = new double[] { 7 * init, 8 * init, 9 * init };
                data8 = new SingleComplex[] 
                {
                    new SingleComplex(8 * init, 8 * init),
                    new SingleComplex(9 * init, 10 * init),
                    new SingleComplex(11 * init, 12 * init)
                };
                data9 = new DoubleComplex[] 
                {
                    new DoubleComplex(9 * init, 10 * init),
                    new DoubleComplex(11 * init, 12 * init),
                    new DoubleComplex(13 * init, 14 * init)
                };
            }
        }

        [TestMethod]
        public void TestColumnsFromStruct_Arrays()
        {
            FitsFile fits;
            BinaryTableHdu tab;

            CreateFitsFileWithTable(out fits, out tab);

            tab.CreateColumns(typeof(ArraysStruct));
            tab.RowCount = 3;

            Assert.AreEqual(10, tab.Columns.Count);
            Assert.AreEqual(158, tab.GetAxisLength(1));
            Assert.AreEqual(3, tab.GetAxisLength(2));

            tab.WriteHeader();

            // Now try to write some data
            tab.WriteNextRow(new ArraysStruct(1));
            tab.WriteNextRow(new ArraysStruct(2));
            tab.WriteNextRow(new ArraysStruct(3));
        }

        [TestMethod]
        public void TestColumns_Arrays()
        {
            FitsFile fits;
            BinaryTableHdu tab;

            CreateFitsFileWithTable(out fits, out tab);

            var cols = new FitsTableColumn[]
            {
                FitsTableColumn.Create("col0", FitsDataType.Create(typeof(bool), 2)),
                FitsTableColumn.Create("col1", FitsDataType.Create(typeof(byte), 3)),
                FitsTableColumn.Create("col2", FitsDataType.Create(typeof(short), 3)),
                FitsTableColumn.Create("col3", FitsDataType.Create(typeof(int), 3)),
                FitsTableColumn.Create("col4", FitsDataType.Create(typeof(long), 3)),
                FitsTableColumn.Create("col5", FitsDataType.Create(typeof(char), 3)),
                FitsTableColumn.Create("col6", FitsDataType.Create(typeof(float), 3)),
                FitsTableColumn.Create("col7", FitsDataType.Create(typeof(double), 3)),
                FitsTableColumn.Create("col8", FitsDataType.Create(typeof(SingleComplex), 3)),
                FitsTableColumn.Create("col9", FitsDataType.Create(typeof(DoubleComplex), 3)),
            };

            tab.CreateColumns(cols);
            tab.RowCount = 3;

            Assert.AreEqual(10, tab.Columns.Count);
            Assert.AreEqual(158, tab.GetAxisLength(1));
            Assert.AreEqual(3, tab.GetAxisLength(2));

            tab.WriteHeader();

            // Now try to write some data
            for (int init = 1; init <= 3; init++)
            {
                tab.WriteNextRow(
                    new bool[] { true, false },
                new byte[] { (byte)init, (byte)(2 * init), (byte)(3 * init) },
                new short[] { (short)init, (short)(2 * init), (short)(3 * init) },
                new int[] { 3 * init, 4 * init, 5 * init },
                new long[] { 4 * init, 5 * init, 6 * init },
                "789".ToCharArray(),
                new float[] { 6 * init, 7 * init, 8 * init },
                new double[] { 7 * init, 8 * init, 9 * init },
                new SingleComplex[] 
                {
                    new SingleComplex(8 * init, 8 * init),
                    new SingleComplex(9 * init, 10 * init),
                    new SingleComplex(11 * init, 12 * init)
                },
                new DoubleComplex[] 
                {
                    new DoubleComplex(9 * init, 10 * init),
                    new DoubleComplex(11 * init, 12 * init),
                    new DoubleComplex(13 * init, 14 * init)
                });
            }
        }
    }
}
