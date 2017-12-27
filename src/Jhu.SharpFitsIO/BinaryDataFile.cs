using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;

namespace Jhu.SharpFitsIO
{
    public abstract class BinaryDataFile : IDisposable
    {

        #region Private member variables

        /// <summary>
        /// Base stream to read from/write to
        /// </summary>
        /// <remarks>
        /// Either set by the constructor (in this case the stream is not owned)
        /// or opened internally (owned)
        /// </remarks>
        [NonSerialized]
        private Stream baseStream;

        /// <summary>
        /// If true, baseStream was opened by the object and will need
        /// to be closed when disposing.
        /// </summary>
        [NonSerialized]
        private bool ownsBaseStream;

        /// <summary>
        /// Wrapped version of the base stream (ForwardStream and/or DetachedStream)
        /// </summary>
        [NonSerialized]
        private Stream wrappedStream;

        /// <summary>
        /// Read or write
        /// </summary>
        [NonSerialized]
        private FileAccess fileAccess;

        /// <summary>
        /// Path to the file. If set, the class can open it internally.
        /// </summary>
        [NonSerialized]
        private string path;

        /// <summary>
        /// Endianness. Many FITS files are big-endian
        /// </summary>
        private Endianness endianness;

        /// <summary>
        /// Little-endian or big-endian bit converter.
        /// </summary>
        [NonSerialized]
        private BitConverterBase bitConverter;

        #endregion
        #region Properties

        /// <summary>
        /// Gets the stream that can be used to read data
        /// </summary>
        [IgnoreDataMember]
        public virtual Stream BaseStream
        {
            get { return baseStream; }
            set { baseStream = value; }
        }

        /// <summary>
        /// Gets the stream data is read from or written to. Used internally.
        /// </summary>
        /// <remarks>
        /// Depending whether compression is turned on or not, we need to use
        /// the baseStream or the wrapper stream.
        /// </remarks>
        [IgnoreDataMember]
        protected internal Stream WrappedStream
        {
            get { return wrappedStream; }
            set { wrappedStream = value; }
        }

        /// <summary>
        /// Gets or sets file mode (read or write)
        /// </summary>
        [IgnoreDataMember]
        public FileAccess FileAccess
        {
            get { return fileAccess; }
            set
            {
                EnsureNotOpen();
                fileAccess = value;
            }
        }

        /// <summary>
        /// Gets or sets the location of the file
        /// </summary>
        [DataMember]
        public string Path
        {
            get { return path; }
            set
            {
                EnsureNotOpen();
                path = value;
            }
        }

        /// <summary>
        /// Gets or sets the endianness of the file.
        /// </summary>
        public Endianness Endianness
        {
            get { return endianness; }
            set
            {
                EnsureNotOpen();
                endianness = value;
            }
        }

        /// <summary>
        /// Gets the BitConverter for byte order swapping. Used internally.
        /// </summary>
        public BitConverterBase BitConverter
        {
            get { return bitConverter; }
        }

        /// <summary>
        /// Gets if the underlying data file is closed
        /// </summary>
        [IgnoreDataMember]
        public virtual bool IsClosed
        {
            get { return baseStream == null; }
        }

        #endregion
        #region Constructors and initializers

        protected BinaryDataFile()
        {
            InitializeMembers(new StreamingContext());
        }

        protected BinaryDataFile(FitsFile old)
        {
            CopyMembers(old);
        }

        protected BinaryDataFile(string path, FileAccess fileAccess, Endianness endianness)
        {
            InitializeMembers(new StreamingContext());

            this.path = path;
            this.fileAccess = fileAccess;
            this.endianness = endianness;
        }

        protected BinaryDataFile(Stream stream, FileAccess fileAccess, Endianness endianness)
        {
            InitializeMembers(new StreamingContext());
            OpenExternalStream(stream, fileAccess, endianness);
        }

        [OnDeserializing]
        private void InitializeMembers(StreamingContext context)
        {
            this.baseStream = null;
            this.ownsBaseStream = false;
            this.wrappedStream = null;

            this.fileAccess = FileAccess.Read;
            this.path = null;

            this.endianness = Endianness.BigEndian;
            this.bitConverter = null;
        }

        private void CopyMembers(BinaryDataFile old)
        {
            this.baseStream = null;
            this.ownsBaseStream = false;
            this.wrappedStream = null;

            this.fileAccess = old.fileAccess;
            this.path = old.path;

            this.endianness = old.endianness;
            this.bitConverter = old.bitConverter;
        }

        public void Dispose()
        {
            Close();
        }

        #endregion
        #region Stream open/close

        /// <summary>
        /// Makes sure that the base stream is not open, if
        /// stream is owned by the class.
        /// </summary>
        protected virtual void EnsureNotOpen()
        {
            if (ownsBaseStream && baseStream != null)
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Opens the file by opening a stream to the resource
        /// identified by the Uri property.
        /// </summary>
        public void Open()
        {
            EnsureNotOpen();

            switch (fileAccess)
            {
                case FileAccess.Read:
                    OpenForRead();
                    break;
                case FileAccess.Write:
                    OpenForWrite();
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Opens a file by wrapping an external file stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fileMode"></param>
        public void Open(Stream stream, FileAccess fileAccess, Endianness endianness)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");  // TODO
            }

            OpenExternalStream(stream, fileAccess, endianness);
            Open();
        }

        /// <summary>
        /// Opens a file by opening a new stream.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="fileMode"></param>
        public void Open(string path, FileAccess fileAccess, Endianness endianness)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path"); // TODO
            }

            this.path = path;
            this.fileAccess = fileAccess;
            this.endianness = endianness;

            Open();
        }

        /// <summary>
        /// Opens a file by wrapping a stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="mode"></param>
        /// <param name="compression"></param>
        protected void OpenExternalStream(Stream stream, FileAccess fileAccess, Endianness endianness)
        {
            this.baseStream = stream;
            this.ownsBaseStream = false;

            this.fileAccess = fileAccess;
            this.endianness = endianness;
        }

        /// <summary>
        /// Opens the underlying stream, if it is not set externally via
        /// a constructor or the OpenStream method.
        /// </summary>
        private void OpenOwnStream()
        {
            switch (fileAccess)
            {
                case FileAccess.Read:
                    baseStream = new FileStream(path, System.IO.FileMode.Open, FileAccess.Read, FileShare.Read);
                    break;
                case FileAccess.Write:
                    baseStream = new FileStream(path, System.IO.FileMode.Create, FileAccess.Write, FileShare.Read);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            ownsBaseStream = true;
        }

        protected virtual void OpenForRead()
        {
            if (FileAccess != FileAccess.Read)
            {
                throw new InvalidOperationException();
            }

            if (baseStream == null)
            {
                OpenOwnStream();
            }

            WrapStream();
            CreateBitConverter();
        }

        protected virtual void OpenForWrite()
        {
            if (fileAccess != FileAccess.Write)
            {
                throw new InvalidOperationException();
            }

            if (baseStream == null)
            {
                OpenOwnStream();
            }

            WrapStream();
            CreateBitConverter();
        }

        protected virtual void WrapStream()
        {
            wrappedStream = baseStream;
        }

        /// <summary>
        /// Closes the data file
        /// </summary>
        public virtual void Close()
        {
            if (wrappedStream != null && wrappedStream != baseStream)
            {
                wrappedStream.Flush();
                wrappedStream.Close();
                wrappedStream.Dispose();
            }

            if (ownsBaseStream && baseStream != null)
            {
                baseStream.Flush();
                baseStream.Close();
                baseStream.Dispose();
                ownsBaseStream = false;
            }

            wrappedStream = null;
            baseStream = null;
        }

        private void CreateBitConverter()
        {
            // Create bit converter
            switch (endianness)
            {
                case Endianness.LittleEndian:
                    bitConverter = new StraightBitConverter();
                    break;
                case Endianness.BigEndian:
                    bitConverter = new SwapBitConverter();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion
    }
}
