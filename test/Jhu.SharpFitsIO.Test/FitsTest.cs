using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ICSharpCode.SharpZipLib.GZip;
using Jhu.SharpFitsIO;

namespace Jhu.SharpFitsIO
{
    [TestClass]
    public class FitsTest
    {
        /// <summary>
        /// Find the outmost directory with a solution file
        /// </summary>
        /// <returns></returns>
        protected string GetSolutionDir()
        {
            var dir = Environment.CurrentDirectory;
            string best = null;

            while (dir != null)
            {
                var files = Directory.GetFiles(dir, "*.sln");

                if (files != null && files.Length > 0)
                {
                    best = dir;
                }

                dir = Directory.GetParent(dir)?.FullName;
            }

            return best;
        }

        private FitsFile OpenFits(string filename)
        {
            var sln = GetSolutionDir();
            var path = Path.Combine(sln, @"modules\sharpfitsio\test\data", filename);

            FitsFile f;

            if (filename.EndsWith(".gz"))
            {
                var infile = new FileStream(path, FileMode.Open, FileAccess.Read);
                var gz = new GZipInputStream(infile);

                f = new FitsFile(gz, FitsFileMode.Read, Endianness.BigEndian);
            }
            else
            {
                f = new FitsFile(path, FitsFileMode.Read, Endianness.BigEndian);
            }

            return f;
        }

        [TestMethod]
        public void ReadFitsTest()
        {
            var f = OpenFits("sdssdr7_fpC.fit.gz");

            f.Close();
        }

        [TestMethod]
        public void ReadSdssDR7ImageTest()
        {
            var f = OpenFits("sdssdr7_fpC.fit.gz");

            var img = (ImageHdu)f.ReadNextHdu();

            Assert.AreEqual(2, img.AxisCount);
            Assert.AreEqual(2048, img.GetAxisLength(1));
            Assert.AreEqual(1489, img.GetAxisLength(2));

            while (img.HasMoreStrides)
            {
                var row = img.ReadStrideInt16();
            }

            int q = 0;
            SimpleHdu hdu;
            while ((hdu = (SimpleHdu)f.ReadNextHdu()) != null)
            {
                q++;
            }

            Assert.AreEqual(0, q);

            f.Close();
        }

        [TestMethod]
        public void ReadSdssDR7SpectrumTest()
        {
            var f = OpenFits("sdssdr7_spSpec.fit");

            var img = (ImageHdu)f.ReadNextHdu();

            Assert.AreEqual(2, img.AxisCount);
            Assert.AreEqual(3857, img.GetAxisLength(1));
            Assert.AreEqual(5, img.GetAxisLength(2));

            while (img.HasMoreStrides)
            {
                var row = img.ReadStrideSingle();
            }

            var tab = (BinaryTableHdu)f.ReadNextHdu();

            var values = new object[tab.Columns.Count];

            while (tab.HasMoreStrides)
            {
                tab.ReadNextRow(values);
            }

            tab = (BinaryTableHdu)f.ReadNextHdu();

            values = new object[tab.Columns.Count];

            while (tab.HasMoreStrides)
            {
                tab.ReadNextRow(values);
            }

            //
            SimpleHdu hdu;
            while ((hdu = (SimpleHdu)f.ReadNextHdu()) != null)
            {
            }

            f.Close();
        }

        [TestMethod]
        public void ReadSdssDR7SpectrumTest2()
        {
            var f = OpenFits("sdssdr7_spSpec.fit");

            SimpleHdu hdu;
            while ((hdu = (SimpleHdu)f.ReadNextHdu()) != null)
            {
                hdu.ReadToFinish();
            }

            f.Close();
        }

        [TestMethod]
        public void ReadHerschelLevel2Test()
        {
            var f = OpenFits("herschel_level2.fits");

            SimpleHdu hdu;
            while ((hdu = (SimpleHdu)f.ReadNextHdu()) != null)
            {
            }

            f.Close();
        }

    }
}
