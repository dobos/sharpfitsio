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

        private void CreateFitsFileWithTable(out FitsFile fits, out BinaryTableHdu tab)
        {
            fits = CreateFitsFile();

            // Primary header
            var prim = HduBase.Create(fits, true, true, true);
            prim.WriteHeader();

            // Table
            tab = BinaryTableHdu.Create(fits, true);
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
        public void TestMethod1()
        {
            FitsFile fits;
            BinaryTableHdu tab;

            CreateFitsFileWithTable(out fits, out tab);

            tab.CreateColumns(new[]
            {
                FitsTableColumn.Create("test", FitsDataTypes.Byte)
            });

            tab.SetAxisLength(2, 1);

            tab.WriteHeader();

            tab.WriteNextRow(new object[] { (byte)9 });
        }
    }
}
