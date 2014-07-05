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
        internal enum ObjectState
        {
            Start,
            Header,
            Strides,
            Done
        }

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

        private ObjectState state;

        private bool primary;

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

        [IgnoreDataMember]
        internal ObjectState State
        {
            get { return state; }
        }

        [DataMember]
        public bool IsPrimary
        {
            get { return primary; }
            set
            {
                EnsureModifiable();

                primary = value;
            }
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
            set
            {
                EnsureModifiable();
                longStringsEnabled = value;
            }
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
                EnsureModifiable();

                var card = new Card(Constants.FitsKeywordSimple);
                card.SetValue(value);
                cards.Set(card);
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
                EnsureModifiable();

                var card = new Card(Constants.FitsKeywordXtension);
                card.SetValue(value);
                cards.Set(card);
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
                EnsureModifiable();
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
            EnsureModifiable();

            var keyword = Constants.FitsKeywordNAxis + i.ToString(FitsFile.Culture);

            Card card;
            if (!cards.TryGet(keyword, out card))
            {
                card = new Card(keyword);
                cards.Add(card);            // TODO: observe header order!
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
            set
            {
                EnsureModifiable();

                Card card;
                if (!cards.TryGet(Constants.FitsKeywordXtension, out card))
                {
                    card = new Card(Constants.FitsKeywordXtension);
                    cards.Add(card);
                }

                card.SetValue(value);
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

            this.state = ObjectState.Start;

            this.headerPosition = -1;
            this.dataPosition = -1;

            this.cards = new CardCollection(this);

            this.strideBuffer = null;
            this.totalStrides = 0;
            this.strideCounter = 0;
        }

        private void CopyMembers(HduBase old)
        {
            this.file = old.file;

            this.state = ObjectState.Start;

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

        private void EnsureModifiable()
        {
            if (state != ObjectState.Start)
            {
                throw new InvalidOperationException();  // TODO
            }
        }

        private void EnsureDuringStrides()
        {
            if (state != ObjectState.Header && state != ObjectState.Strides)
            {
                throw new InvalidOperationException();  // TODO
            }
        }

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

            cards.Add(new Card(Constants.FitsKeywordEnd));
        }

        protected virtual void ProcessCard(Card card)
        {
            // Are long strings enabled?
            if (FitsFile.Comparer.Compare(card.Keyword, Constants.FitsKeywordLongStrn) == 0)
            {
                this.longStringsEnabled = true;
            }
        }

        /// <summary>
        /// Sort cards according to the FITS standard
        /// </summary>
        protected virtual void SortCards()
        {
            // TODO: add more constraints

            // Move END to the very end
            for (int i = 0; i < cards.Count - 1; i++)
            {
                if (FitsFile.Comparer.Compare(cards[i].Keyword, Constants.FitsKeywordEnd) == 0)
                {
                    var end = cards[i];
                    cards.RemoveAt(i);
                    cards.Add(end);
                    break;
                }
            }
        }

        #endregion
        #region Header functions

        public void ReadHeader()
        {
            // Make sure file is in read more
            if (file.FileMode != FitsFileMode.Write)
            {
                throw new InvalidOperationException();
            }

            // Make sure header is read only once
            if (state != ObjectState.Start)
            {
                throw new InvalidOperationException();
            }

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

            CreateStrideBuffer();

            state = ObjectState.Header;
        }

        public virtual void WriteHeader()
        {
            // Make sure file is in write more
            if (file.FileMode != FitsFileMode.Write)
            {
                throw new InvalidOperationException();
            }

            // Make sure header is written only once
            if (state != ObjectState.Start)
            {
                throw new InvalidOperationException();
            }

            // Sort cards so that their order conforms to the standard
            SortCards();

            // Save start position
            headerPosition = Fits.WrappedStream.Position;

            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].Write(Fits.WrappedStream);
            }

            // Skip block
            Fits.SkipBlock();
            dataPosition = Fits.WrappedStream.Position;

            state = ObjectState.Header;
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
            get { return strideCounter < totalStrides; }
        }

        protected void CreateStrideBuffer()
        {
            if (strideBuffer == null)
            {
                strideBuffer = new byte[GetStrideLength()];
                totalStrides = GetTotalStrides();
                strideCounter = 0;
            }
        }

        public byte[] ReadStride()
        {
            EnsureDuringStrides();

            CreateStrideBuffer();

            if (strideBuffer.Length != Fits.WrappedStream.Read(strideBuffer, 0, strideBuffer.Length))
            {
                throw new FitsException("Unexpected end of stream.");  // *** TODO
            }

            strideCounter++;

            if (!HasMoreStrides)
            {
                Fits.SkipBlock();
                state = ObjectState.Done;
            }

            return strideBuffer;
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

        public void WriteStride()
        {
            EnsureDuringStrides();

            Fits.WrappedStream.Write(strideBuffer, 0, strideBuffer.Length);

            strideCounter++;

            if (!HasMoreStrides)
            {
                Fits.SkipBlock();
                state = ObjectState.Done;
            }
        }

        #endregion
    }
}
