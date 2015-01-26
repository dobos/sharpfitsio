using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Jhu.SharpFitsIO
{
    /// <summary>
    /// Implements a write-only memory stream that spills onto the
    /// disk when a given size is reached.
    /// </summary>
    /// <remarks>
    /// This class only implements the write functions of a standard
    /// stream. When all data is written to the stream, it can be
    /// written to another using the WriteTo function.
    /// </remarks>
    internal class SpillMemoryStream : Stream, IDisposable
    {
        #region Private member varibles

        private long position;
        private long spillLimit;
        private string spillPath;

        /// <summary>
        /// Internal memory buffer to store data temporarily until the
        /// spill limit is reached.
        /// </summary>
        private MemoryStream memoryBuffer;

        /// <summary>
        /// External file buffer to store data temporarily after the
        /// spill limit is reached.
        /// </summary>
        private FileStream spillBuffer;

        #endregion
        #region Properties

        /// <summary>
        /// Gets the current position of the stream.
        /// </summary>
        public override long Position
        {
            get { return position; }
            set { throw new InvalidOperationException(); }
        }

        /// <summary>
        /// Gets or sets the size limit at which data is spilled to the disk.
        /// </summary>
        public long SpillLimit
        {
            get { return spillLimit; }
            set
            {
                EnsureNotOpen();
                spillLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets the path of temporary file used when data is spilled
        /// to the disk.
        /// </summary>
        public string SpillPath
        {
            get { return spillPath; }
            set
            {
                EnsureNotOpen();
                spillPath = value;
            }
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanTimeout
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return position; }
        }

        #endregion
        #region Constructors and initializers

        public SpillMemoryStream()
        {
            InitializeMembers();
        }

        public SpillMemoryStream(long spillLimit)
        {
            InitializeMembers();

            this.spillLimit = spillLimit;
        }

        public SpillMemoryStream(long spillLimit, string spillPath)
        {
            InitializeMembers();

            this.spillLimit = spillLimit;
            this.spillPath = spillPath;
        }

        private void InitializeMembers()
        {
            this.position = 0;
            this.spillLimit = 0x100000;          // 1MB
            this.spillPath = null;
            this.memoryBuffer = null;
            this.spillBuffer = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (spillBuffer != null)
                {
                    spillBuffer.Dispose();
                }

                if (memoryBuffer != null)
                {
                    memoryBuffer.Dispose();
                }

                if (spillPath != null && File.Exists(spillPath))
                {
                    File.Delete(spillPath);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
        #region Stream implementation

        public override void Close()
        {
            if (spillBuffer != null)
            {
                spillBuffer.Close();
            }

            if (memoryBuffer != null)
            {
                memoryBuffer.Close();
            }

            base.Close();
        }

        public override void Flush()
        {
            if (spillBuffer != null)
            {
                spillBuffer.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        public override int ReadByte()
        {
            throw new InvalidOperationException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (position + count < spillLimit)
            {
                OpenMemoryBuffer();
                memoryBuffer.Write(buffer, offset, count);
            }
            else
            {
                OpenSpillBuffer();
                spillBuffer.Write(buffer, offset, count);
            }

            position += count;
        }

        public override void WriteByte(byte value)
        {
            if (position + 1 < spillLimit)
            {
                OpenMemoryBuffer();
                memoryBuffer.WriteByte(value);
            }
            else
            {
                OpenSpillBuffer();
                spillBuffer.WriteByte(value);
            }

            position++;
        }

        #endregion

        /// <summary>
        /// Writes the content of both buffers to an output stream.
        /// </summary>
        /// <param name="stream"></param>
        public void WriteTo(Stream stream)
        {
            // TODO: this function could use async copy but that just
            // overcomplicates things

            // Flush memory to stream
            if (memoryBuffer != null)
            {
                memoryBuffer.WriteTo(stream);
            }

            if (spillBuffer != null)
            {
                // Rewind file but remember position
                long pos = spillBuffer.Position;
                spillBuffer.Seek(0, SeekOrigin.Begin);

                // Copy file to output stream
                long i = 0;
                byte[] buffer = new byte[0x10000];     // 64k
                while (i < pos)
                {
                    int count = spillBuffer.Read(buffer, 0, buffer.Length);
                    stream.Write(buffer, 0, count);

                    i += count;
                }

                spillBuffer.Seek(pos, SeekOrigin.Begin);
            }
        }

        private void OpenMemoryBuffer()
        {
            if (memoryBuffer == null && spillLimit > 0)
            {
                memoryBuffer = new MemoryStream();
            }
        }

        private void OpenSpillBuffer()
        {
            if (spillBuffer == null)
            {
                // If path is not set use temp
                if (spillPath == null)
                {
                    spillPath = Path.GetTempFileName();
                }

                spillBuffer = new FileStream(spillPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
        }

        private void EnsureNotOpen()
        {
            if (memoryBuffer != null || spillBuffer != null)
            {
                throw new InvalidOperationException("Stream is already open.");     // TODO ***
            }
        }
    }
}
