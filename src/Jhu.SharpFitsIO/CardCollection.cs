using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jhu.SharpFitsIO
{
    /// <summary>
    /// Implements a specialized collection to store FITS headers.
    /// </summary>
    /// <remarks>
    /// The collection handles both unique and non-unique headers. Unique
    /// headers are also stored in a dictionary for fast lookup by keyword.
    /// </remarks>
    [Serializable]
    public class CardCollection : IList<Card>
    {
        #region Private member variables

        private SimpleHdu hdu;
        private List<Card> cardList;
        private Dictionary<string, Card> cardDictionary;

        #endregion
        #region Properties and indexers

        public Card this[string key]
        {
            get
            {
                return cardDictionary[key];
            }
            set
            {
                if (FitsFile.Comparer.Compare(key, value.Keyword) != 0)
                {
                    throw new InvalidOperationException();  // TODO
                }

                Set(value);
            }
        }

        public Card this[string key, int index]
        {
            get { return this[key + index.ToString(FitsFile.Culture)]; }
            set { this[key + index.ToString(FitsFile.Culture)] = value; }
        }

        public Card this[int index]
        {
            get
            {
                return cardList[index];
            }
            set
            {
                var keyword = cardList[index].Keyword;

                if (!value.IsComment && cardDictionary.ContainsKey(keyword))
                {
                    cardDictionary.Remove(keyword);
                }

                cardList[index] = value;

                if (!value.IsComment)
                {
                    cardDictionary.Add(keyword, value);
                }
            }
        }

        /// <summary>
        /// Gets the collection of all headers.
        /// </summary>
        public ICollection<Card> Values
        {
            get { return cardList; }
        }

        /// <summary>
        /// Gets the collection of unique header keywords.
        /// </summary>
        public ICollection<string> Keys
        {
            get { return cardDictionary.Keys; }
        }

        /// <summary>
        /// Gets number of all headers.
        /// </summary>
        public int Count
        {
            get { return cardList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        #endregion
        #region Constructors and initializers

        internal CardCollection(SimpleHdu hdu)
        {
            InitializeMembers();

            this.hdu = hdu;
        }

        public CardCollection(CardCollection old)
        {
            CopyMembers(old);
        }

        private void InitializeMembers()
        {
            this.hdu = null;
            this.cardList = new List<Card>();
            this.cardDictionary = new Dictionary<string, Card>(FitsFile.Comparer);
        }

        private void CopyMembers(CardCollection old)
        {
            this.hdu = old.hdu;

            // Because cardDictionary doesn't contain all cards (comments, etc. are missing)
            // copy the two collections independently
            this.cardList = new List<Card>(old.cardList);
            this.cardDictionary = new Dictionary<string, Card>(old.cardDictionary, FitsFile.Comparer);
        }

        #endregion

        private void EnsureModifiable()
        {
            if (hdu.State != SimpleHdu.ObjectState.Start)
            {
                throw new InvalidOperationException();  // TODO
            }
        }

        #region Collection interface functions

        public void Add(Card card)
        {
            EnsureModifiable();

            AddInternal(card);
        }

        internal void AddInternal(Card card)
        {
            if (!String.IsNullOrWhiteSpace(card.Keyword) && !card.IsComment)
            {
                cardDictionary.Add(card.Keyword, card);
            }

            cardList.Add(card);
        }

        public void Insert(int index, Card card)
        {
            EnsureModifiable();

            if (!card.IsComment)
            {
                cardDictionary.Add(card.Keyword, card);
            }

            cardList.Insert(index, card);
        }

        public void RemoveAt(int index)
        {
            EnsureModifiable();

            if (!cardList[index].IsComment)
            {
                cardDictionary.Remove(cardList[index].Keyword);
            }

            cardList.RemoveAt(index);
        }

        public bool Remove(string key)
        {
            EnsureModifiable();

            cardList.Remove(cardDictionary[key]);
            return cardDictionary.Remove(key);
        }

        public bool Remove(Card card)
        {
            EnsureModifiable();

            if (!card.IsComment)
            {
                cardDictionary.Remove(card.Keyword);
            }

            return cardList.Remove(card);
        }

        public bool Remove(KeyValuePair<string, Card> item)
        {
            EnsureModifiable();

            return Remove(item.Key);
        }

        public void Clear()
        {
            EnsureModifiable();

            cardList.Clear();
            cardDictionary.Clear();
        }

        public bool ContainsKey(string key)
        {
            return cardDictionary.ContainsKey(key);
        }

        public bool Contains(Card item)
        {
            return cardList.Contains(item);
        }

        public bool Contains(KeyValuePair<string, Card> item)
        {
            return cardDictionary.Contains(item);
        }

        public int IndexOf(Card item)
        {
            return cardList.IndexOf(item);
        }

        public void CopyTo(Card[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region Get and set functions

        /// <summary>
        /// Returns the value a header, if it exists.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGet(string key, out Card value)
        {
            return cardDictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Returns the value of an indexed header, if it exists.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="index"></param>
        /// <param name="card"></param>
        /// <returns></returns>
        public bool TryGet(string key, int index, out Card card)
        {
            key += index.ToString();
            return cardDictionary.TryGetValue(key, out card);
        }

        public void Set(Card card)
        {
            EnsureModifiable();

            if (card.IsComment)
            {
                throw new InvalidOperationException();  // TODO
            }

            if (cardDictionary.ContainsKey(card.Keyword))
            {
                var index = cardList.IndexOf(cardDictionary[card.Keyword]);

                cardDictionary[card.Keyword] = card;
                cardList[index] = card;
            }
            else
            {
                cardDictionary.Add(card.Keyword, card);
                cardList.Add(card);
            }
        }

        #endregion
        #region Enumerator functions

        IEnumerator<Card> IEnumerable<Card>.GetEnumerator()
        {
            return cardList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IDictionary<string, Card>)cardDictionary).GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Sort cards according to the FITS standard
        /// </summary>
        public void Sort()
        {
            cardList.Sort();         
        }
    }
}
