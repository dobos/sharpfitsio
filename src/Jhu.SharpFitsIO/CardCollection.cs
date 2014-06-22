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
    public class CardCollection : IList<Card>
    {
        #region Private member variables

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
                throw new NotImplementedException();
            }
        }

        public Card this[int index]
        {
            get
            {
                return cardList[index];
            }
            set
            {
                throw new NotImplementedException();
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

        public CardCollection()
        {
            InitializeMembers();
        }

        public CardCollection(CardCollection old)
        {
            CopyMembers(old);
        }

        private void InitializeMembers()
        {
            this.cardList = new List<Card>();
            this.cardDictionary = new Dictionary<string, Card>(FitsFile.Comparer);
        }

        private void CopyMembers(CardCollection old)
        {
            this.cardList = new List<Card>();
            this.cardDictionary = new Dictionary<string, Card>(FitsFile.Comparer);

            foreach (var card in old.cardList)
            {
                var ncard = (Card)card.Clone();

                this.cardList.Add(ncard);
                if (ncard.IsUnique)
                {
                    this.cardDictionary.Add(ncard.Keyword, ncard);
                }
            }
        }

        #endregion
        #region Collection interface functions

        public void Add(Card card)
        {
            if (card.IsUnique)
            {
                cardDictionary.Add(card.Keyword, card);
            }

            cardList.Add(card);
        }

        public void Insert(int index, Card card)
        {
            if (card.IsUnique)
            {
                cardDictionary.Add(card.Keyword, card);
            }

            cardList.Insert(index, card);
        }

        public void RemoveAt(int index)
        {
            if (cardList[index].IsUnique)
            {
                cardDictionary.Remove(cardList[index].Keyword);
            }

            cardList.RemoveAt(index);
        }

        public bool Remove(string key)
        {
            cardList.Remove(cardDictionary[key]);
            return cardDictionary.Remove(key);
        }

        public bool Remove(Card card)
        {
            if (card.IsUnique)
            {
                cardDictionary.Remove(card.Keyword);
            }

            return cardList.Remove(card);
        }

        public bool Remove(KeyValuePair<string, Card> item)
        {
            return Remove(item.Key);
        }

        public void Clear()
        {
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
        #region Value get functions

        /// <summary>
        /// Returns the value a header, if it exists.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string key, out Card value)
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
        public bool TryGetValue(string key, int index, out Card card)
        {
            key += index.ToString();
            return cardDictionary.TryGetValue(key, out card);
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
    }
}
