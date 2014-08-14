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
    internal class SpillMemoryStream : Stream
    {
        #region Private member varibles

        private long position;
        private long spillLimit;
        private string spillPath;
        private MemoryStream memory;
        private FileStream spill;

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
            this.spillLimit = 0x100000;     // 1MB
            this.spillPath = null;
            this.memory = new MemoryStream();
            this.spill = null;
        }

        #endregion

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

        public override long Position
        {
            get { return position; }
            set { throw new InvalidOperationException(); }
        }

        public override void Close()
        {
            if (spill != null)
            {
                spill.Close();
            }

            if (memory != null)
            {
                memory.Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (spill != null)
                {
                    spill.Dispose();
                }

                if (memory != null)
                {
                    memory.Dispose();
                }
            }

            if (spillPath != null && File.Exists(spillPath))
            {
                File.Delete(spillPath);
            }
        }

        public override void Flush()
        {
            if (spill != null)
            {
                spill.Flush();
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
            if (spill == null && position + count < spillLimit)
            {
                memory.Write(buffer, offset, count);
            }
            else
            {
                OpenSpillFile();

                spill.Write(buffer, offset, count);
            }

            position += count;
        }

        public override void WriteByte(byte value)
        {
            if (spill == null && position + 1 < spillLimit)
            {
                memory.WriteByte(value);
            }
            else
            {
                OpenSpillFile();

                spill.WriteByte(value);
            }
        }

        public void WriteTo(Stream stream)
        {
            // TODO: this function could use async copy but that just
            // overcomplicates things

            // Flush memory to stream
            if (memory != null)
            {
                memory.WriteTo(stream);
            }

            if (spill != null)
            {
                // Rewind file but remember position
                var pos = spill.Position;
                spill.Seek(0, SeekOrigin.Begin);

                // Copy file to output stream
                var i = 0;
                var buffer = new byte[0x10000];     // 64k
                while (i < pos)
                {
                    var count = spill.Read(buffer, 0, buffer.Length);
                    stream.Write(buffer, 0, count);

                    i += count;
                }

                spill.Seek(pos, SeekOrigin.Begin);
            }
        }

        private void OpenSpillFile()
        {
            if (spill != null)
            {
                // If path is not set use temp
                if (spillPath == null)
                {
                    spillPath = Path.GetTempFileName();
                }

                spill = new FileStream(spillPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
            }
        }
    }
}
