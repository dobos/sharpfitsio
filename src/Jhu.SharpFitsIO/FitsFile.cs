using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;

namespace Jhu.SharpFitsIO
{
    [Serializable]
    [DataContract(Namespace = "")]
    public class FitsFile : BinaryDataFile, IDisposable, ICloneable
    {
        #region String handlers

        public static readonly StringComparison Comparision = StringComparison.InvariantCultureIgnoreCase;
        public static readonly StringComparer Comparer = StringComparer.InvariantCultureIgnoreCase;
        public static readonly System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.InvariantCulture;

        #endregion

        internal static readonly byte[] FillZeroBuffer;
        internal static readonly byte[] FillSpaceBuffer;

        static FitsFile()
        {
            FillZeroBuffer = new byte[Constants.FitsBlockSize];
            FillSpaceBuffer = new byte[Constants.FitsBlockSize];
            for (int i = 0; i < FillSpaceBuffer.Length; i++)
            {
                FillZeroBuffer[i] = 0x00;
                FillSpaceBuffer[i] = 0x20;      // ' '
            }
        }

        #region Private member variables

        /// <summary>
        /// If true, HDUs can be written buffered. This option is to be used
        /// when the amount of data to be written to the stream is unknown.
        /// </summary>
        [NonSerialized]
        private bool isBufferingAllowed;

        /// <summary>
        /// Stores the hdus read/written so far.
        /// </summary>
        [NonSerialized]
        private List<SimpleHdu> hdus;

        /// <summary>
        /// Points to the current hdu in the hdus collection
        /// </summary>
        /// <remarks>
        /// This can be different from hdus.Count as blocks can be
        /// predefined by the user or automatically generated as new
        /// hdus are discovered while reading the file.
        /// </remarks>
        [NonSerialized]
        private int hduCounter;

        #endregion
        #region Properties
        
        public bool IsBufferingAllowed
        {
            get { return isBufferingAllowed; }
            set { isBufferingAllowed = value; }
        }

        /// <summary>
        /// Gets a collection of HDUs blocks.
        /// </summary>
        [IgnoreDataMember]
        protected List<SimpleHdu> Hdus
        {
            get { return hdus; }
        }

        #endregion
        #region Constructors and initializers

        public FitsFile()
        {
            InitializeMembers(new StreamingContext());
        }

        public FitsFile(FitsFile old)
        {
            CopyMembers(old);
        }

        public FitsFile(string path, FileAccess fileAccess, Endianness endianness)
            :base(path, fileAccess, endianness)
        {
            InitializeMembers(new StreamingContext());
            Open();
        }

        public FitsFile(string path, FileAccess fileAccess)
            : this(path, fileAccess, Endianness.BigEndian)
        {
            // Overload
        }

        public FitsFile(Stream stream, FileAccess fileAccess, Endianness endianness)
        {
            InitializeMembers(new StreamingContext());
            OpenExternalStream(stream, fileAccess, endianness);
            Open();
        }

        public FitsFile(Stream stream, FileAccess fileAccess)
            : this(stream, fileAccess, Endianness.BigEndian)
        {
            // Overload
        }

        [OnDeserializing]
        private void InitializeMembers(StreamingContext context)
        {
            this.isBufferingAllowed = false;

            this.hdus = new List<SimpleHdu>();
            this.hduCounter = -1;
        }

        private void CopyMembers(FitsFile old)
        {
            this.isBufferingAllowed = old.isBufferingAllowed;

            // Deep copy HDUs
            this.hdus = new List<SimpleHdu>();
            foreach (var hdu in old.hdus)
            {
                this.hdus.Add((SimpleHdu)hdu.Clone());
            }
            this.hduCounter = old.hduCounter;
        }

        public object Clone()
        {
            return new FitsFile(this);
        }

        #endregion
        #region Stream open/close
        
        protected override void WrapStream()
        {
            if (!BaseStream.CanSeek)
            {
                WrappedStream = new SeekForwardStream(new DetachedStream(BaseStream));
            }
            else
            {
                WrappedStream = new DetachedStream(BaseStream);
            }
        }
        
        #endregion

        public SimpleHdu ReadNextHdu()
        {
            return ReadNextHduAsync().Result;
        }

        /// <summary>
        /// Reads the next HDU from the file.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Skips reading the rest of the current HDU, so data will not
        /// be read into memory.
        /// </remarks>
        public async Task<SimpleHdu> ReadNextHduAsync()
        {
            if (hduCounter != -1)
            {
                // If we are not at the beginning of the file, read to the end of the
                // block, read the block footer and position stream on the beginning
                // of the next file block
                await hdus[hduCounter].ReadToFinishAsync();
            }

            try
            {
                hduCounter++;

                SimpleHdu nextHdu;

                // If blocks are created manually, the blocks collection might already
                // contain an object for the next file block. In this case, use the
                // manually created object, otherwise create one automatically.
                if (hduCounter < hdus.Count)
                {
                    nextHdu = await ReadNextHduAsync(hdus[hduCounter]);
                }
                else
                {
                    // Create a new block automatically, if collection is not predefined
                    nextHdu = await ReadNextHduAsync(null);
                    if (nextHdu != null)
                    {
                        hdus.Add(nextHdu);
                    }
                }

                return nextHdu;

                // FITS files don't have footers, so nothing to do here
            }
            catch (EndOfStreamException)
            {
                // Some data formats cannot detect end of blocks and will
                // throw exception at the end of the file instead
                // Eat this exception now. Note, that this behaviour won't
                // occur when block contents are read, so the integrity of
                // reading a block will be kept anyway.
            }

            // No additional blocks found, return with null
            hduCounter = -1;
            return null;
        }

        private async Task<SimpleHdu> ReadNextHduAsync(SimpleHdu prototype)
        {
            SimpleHdu hdu;

            if (prototype != null)
            {
                hdu = prototype;
                hdu.Fits = this;
            }
            else
            {
                hdu = new SimpleHdu(this);
            }

            await hdu.ReadHeaderAsync();

            // Dispatch different types of FITS HDUs
            if (prototype != null)
            {
                return hdu;
            }
            else if (hdu.Simple)
            {
                return new ImageHdu(hdu);
            }
            else
            {
                switch (hdu.Extension)
                {
                    case Constants.FitsExtensionBinTable:
                        return new BinaryTableHdu(hdu);
                    case Constants.FitsExtensionImage:
                        return new ImageHdu(hdu);
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        internal Task SkipBlockAsync()
        {
            // NOT: seek is sync when reading, probably it only sets
            // the file pointer and the async call will only happen when
            // reading from the file.
            // We keep the async signature here for compatibility

            var offset = (int)(Constants.FitsBlockSize * ((WrappedStream.Position + Constants.FitsBlockSize - 1) / Constants.FitsBlockSize) - WrappedStream.Position);
            if (offset > 0)
            {
                WrappedStream.Seek(offset, SeekOrigin.Current);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Advances the stream to the next 2880 byte block
        /// </summary>
        internal async Task SkipBlockAsync(byte[] fill)
        {
            var offset = (int)(Constants.FitsBlockSize * ((WrappedStream.Position + Constants.FitsBlockSize - 1) / Constants.FitsBlockSize) - WrappedStream.Position);

            if (offset > 0)
            {
                await WrappedStream.WriteAsync(fill, 0, offset);
            }
        }
    }
}
