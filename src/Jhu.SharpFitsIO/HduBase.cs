using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Runtime.Serialization;

namespace Jhu.SharpFitsIO
{
    // TODO: rename to SimpleHDU or simply HDU
    public class HduBase : ICloneable
    {
        #region Private member variables

        /// <summary>
        /// Holds a reference to the underlying file
        /// </summary>
        /// <remarks>
        /// This value is set by the constructor when a new data file block
        /// is created based on a data file.
        /// </remarks>
        [NonSerialized]
        protected FitsFile file;

        private bool primary;

        [NonSerialized]
        private bool headerRead;

        [NonSerialized]
        private bool headerWritten;

        [NonSerialized]
        private long headerPosition;

        [NonSerialized]
        private long dataPosition;

        [NonSerialized]
        private CardCollection cards;

        [NonSerialized]
        private byte[] strideBuffer;

        [NonSerialized]
        private int totalStrides;

        [NonSerialized]
        private int strideCounter;

        [NonSerialized]
        private bool longStringsEnabled;

        #endregion
        #region Properties

        [IgnoreDataMember]
        internal FitsFile Fits
        {
            get { return file; }
            set { file = value; }
        }

        [DataMember]
        public bool IsPrimary
        {
            get { return primary; }
            set { primary = value; }
        }

        [IgnoreDataMember]
        public long HeaderPosition
        {
            get { return headerPosition; }
        }

        [IgnoreDataMember]
        public long DataPosition
        {
            get { return dataPosition; }
        }

        [IgnoreDataMember]
        public CardCollection Cards
        {
            get { return cards; }
        }

        [IgnoreDataMember]
        protected byte[] StrideBuffer
        {
            get { return strideBuffer; }
        }

        [IgnoreDataMember]
        public int TotalStrides
        {
            get { return totalStrides; }
        }

        /// <summary>
        /// Gets or sets whether OGIP long text headers are enabled.
        /// </summary>
        [IgnoreDataMember]
        public bool LongStringsEnabled
        {
            get { return longStringsEnabled; }
            set { longStringsEnabled = value; }
        }

        #endregion
        #region Keyword accessor properties and functions

        [IgnoreDataMember]
        public bool Simple
        {
            get
            {
                Card card;
                if (Cards.TryGet(Constants.FitsKeywordSimple, out card))
                {
                    return card.GetBoolean();
                }
                else
                {
                    return false;
                }
            }
            set
            {
                Card card;
                if (!cards.TryGet(Constants.FitsKeywordSimple, out card))
                {
                    card = new Card(Constants.FitsKeywordSimple);
                    cards.Add(card);
                }

                card.SetValue(value);
            }
        }

        [IgnoreDataMember]
        public string Extension
        {
            get
            {
                Card card;
                if (cards.TryGet(Constants.FitsKeywordXtension, out card))
                {
                    return card.GetString().Trim();
                }
                else
                {
                    return null;
                }
            }
            set
            {
                Card card;
                if (!cards.TryGet(Constants.FitsKeywordXtension, out card))
                {
                    card = new Card(Constants.FitsKeywordXtension);
                    cards.Add(card);
                }

                card.SetValue(value);
            }
        }

        [IgnoreDataMember]
        public int AxisCount
        {
            get
            {
                return cards[Constants.FitsKeywordNAxis].GetInt32();
            }
            set
            {
                cards[Constants.FitsKeywordNAxis].SetValue(value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        /// <remarks>Attention! FITS image axes use 1-based indexing.</remarks>
        public int GetAxisLength(int i)
        {
            return cards[Constants.FitsKeywordNAxis + i.ToString(FitsFile.Culture)].GetInt32();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <param name="value"></param>
        /// <remarks>Attention! FITS image axes use 1-based indexing.</remarks>
        public void SetAxisLength(int i, int value)
        {
            var keyword = Constants.FitsKeywordNAxis + i.ToString(FitsFile.Culture);

            Card card;
            if (!cards.TryGet(keyword, out card))
            {
                card = new Card(keyword);
                cards.Add(card);            // TODO: observer header order!
            }

            card.SetValue(value);
        }

        [IgnoreDataMember]
        public int BitsPerPixel
        {
            get
            {
                return cards[Constants.FitsKeywordBitPix].GetInt32();
            }
        }

        /// <summary>
        /// Gets if this HDU has any extensions.
        /// </summary>
        /// <remarks>
        /// This is typically used in the primary header only.
        /// </remarks>
        [IgnoreDataMember]
        public bool HasExtension
        {
            get
            {
                Card card;
                if (Cards.TryGet(Constants.FitsKeywordExtend, out card))
                {
                    return card.GetBoolean();
                }
                else if (AxisCount == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        #endregion
        #region Constructors and initializers

        internal HduBase(FitsFile file)
        {
            InitializeMembers(new StreamingContext());

            this.file = file;
        }

        internal HduBase(HduBase old)
        {
            CopyMembers(old);
        }

        [OnDeserializing]
        private void InitializeMembers(StreamingContext context)
        {
            this.file = null;

            this.headerRead = false;
            this.headerWritten = false;
            this.headerPosition = -1;
            this.dataPosition = -1;

            this.cards = new CardCollection();

            this.strideBuffer = null;
            this.totalStrides = 0;
            this.strideCounter = 0;
        }

        private void CopyMembers(HduBase old)
        {
            this.file = old.file;

            this.headerRead = old.headerRead;
            this.headerWritten = old.headerWritten;
            this.headerPosition = old.headerPosition;
            this.dataPosition = old.dataPosition;

            this.cards = new CardCollection(old.cards);

            this.strideBuffer = null;
            this.totalStrides = 0;
            this.strideCounter = 0;
        }

        public virtual object Clone()
        {
            return new HduBase(this);
        }

        #endregion
        #region Static create functions

        public static HduBase Create(FitsFile fits, bool initialize, bool primary, bool hasExtensions)
        {
            var hdu = new HduBase(fits);

            if (initialize)
            {
                hdu.InitializeCards(primary, hasExtensions);
            }

            return hdu;
        }

        #endregion
        #region Card functions

        protected virtual void InitializeCards(bool primary, bool hasExtension)
        {
            // Mandatory keywords for primary and extension HDUs
            if (primary)
            {
                cards.Add(new Card(Constants.FitsKeywordSimple, "T", "conforms to FITS standard"));
            }
            else
            {
                cards.Add(new Card(Constants.FitsKeywordXtension, String.Empty, "extension type"));
            }

            // Mandatory for all HDUs
            cards.Add(new Card(Constants.FitsKeywordNAxis, "0", "number of array dimensions"));
            cards.Add(new Card(Constants.FitsKeywordBitPix, "8", "array data type"));

            // Primary HDUs may have this keyword if there are additional HDUs
            if (hasExtension)
            {
                cards.Add(new Card(Constants.FitsKeywordExtend, "F", "has extensions"));
            }
        }

        protected virtual void ProcessCard(Card card)
        {
            // Are long strings enabled?
            if (FitsFile.Comparer.Compare(card.Keyword, Constants.FitsKeywordLongStrn) == 0)
            {
                this.longStringsEnabled = true;
            }
        }

        #endregion
        #region Stride functions

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Last axis length determines stride length
        /// </remarks>
        public virtual int GetStrideLength()
        {
            return Math.Abs(BitsPerPixel) / 8 * GetAxisLength(1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Last axis length determines stride length
        /// </remarks>
        public virtual int GetTotalStrides()
        {
            int total = 1;

            for (int i = 0; i < AxisCount - 1; i++)
            {
                total *= GetAxisLength(i + 1);
            }

            return total;
        }

        public long GetTotalSize()
        {
            return GetStrideLength() * GetTotalStrides();
        }

        public bool HasMoreStrides
        {
            get { return strideBuffer == null || strideCounter < totalStrides; }
        }

        public byte[] ReadStride()
        {
            if (strideBuffer == null)
            {
                strideBuffer = new byte[GetStrideLength()];
                totalStrides = GetTotalStrides();
                strideCounter = 0;
            }

            if (strideBuffer.Length != Fits.WrappedStream.Read(strideBuffer, 0, strideBuffer.Length))
            {
                throw new FitsException("Unexpected end of stream.");  // *** TODO
            }

            strideCounter++;

            if (!HasMoreStrides)
            {
                Fits.SkipBlock();
            }

            return strideBuffer;
        }

        #endregion
        #region Read functions

        public void ReadHeader()
        {
            // Make sure header is read only once
            if (!headerRead)
            {
                // Save start position
                headerPosition = Fits.WrappedStream.Position;

                Card card;

                do
                {
                    card = new Card();
                    card.Read(Fits.WrappedStream);

                    ProcessCard(card);

                    cards.Add(card);
                }
                while (!card.IsEnd);

                // Skip block
                Fits.SkipBlock();
                dataPosition = Fits.WrappedStream.Position;

                headerRead = true;
            }
        }

        internal void ReadToFinish()
        {
            // Check if this is a header-only HDU. If not, we
            // mush skip the data parts, otherwise skip the header padding only

            if (AxisCount != 0)
            {
                var sl = GetStrideLength();
                var sc = GetTotalStrides();

                long offset = sl * (sc - strideCounter);
                Fits.WrappedStream.Seek(offset, SeekOrigin.Current);
            }

            Fits.SkipBlock();
        }

        #endregion
        #region Write functions

        public virtual void WriteHeader()
        {
            // Make sure header is written only once
            if (!headerWritten)
            {
                // Save start position
                headerPosition = Fits.WrappedStream.Position;

                for (int i = 0; i < cards.Count; i++)
                {
                    cards[i].Write(Fits.WrappedStream);
                }

                // Skip block
                Fits.SkipBlock();
                dataPosition = Fits.WrappedStream.Position;

                headerWritten = true;
            }
        }

        #endregion
    }
}
