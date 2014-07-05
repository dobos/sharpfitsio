using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;

namespace Jhu.SharpFitsIO
{
    [Serializable]
    public class Card : ICloneable, IComparable
    {
        private static readonly Regex StringRegex = new Regex(@"'(?:[^']|'{2})*'");
        private static readonly Regex KeywordRegex = new Regex(@"([a-zA-Z_]+)([0-9]*)");

        private string keyword;
        private string rawValue;
        private string comments;

        public string Keyword
        {
            get { return keyword; }
            set { keyword = value; }
        }

        public string Comments
        {
            get { return comments; }
            set { comments = value; }
        }

        public bool IsComment
        {
            get
            {
                return !String.IsNullOrWhiteSpace(keyword) && Constants.CommentKeywords.Contains(keyword);
            }
        }

        public bool IsContinue
        {
            get
            {
                return
                    FitsFile.Comparer.Compare(keyword, Constants.FitsKeywordContinue) == 0;
            }
        }

        public bool IsEnd
        {
            get { return FitsFile.Comparer.Compare(keyword, Constants.FitsKeywordEnd) == 0; }
        }

        #region Constructors and initializers

        public Card()
        {
            InitializeMembers();
        }

        public Card(string keyword)
        {
            InitializeMembers();

            this.keyword = keyword;
        }

        public Card(string keyword, int index)
            : this(keyword + index.ToString(FitsFile.Culture))
        {
        }

        public Card(string keyword, string rawValue, string comment)
        {
            InitializeMembers();

            this.keyword = keyword;
            this.rawValue = rawValue;
            this.comments = comment;
        }

        public Card(Card old)
        {
            CopyMembers(old);
        }

        private void InitializeMembers()
        {
            this.keyword = null;
            this.rawValue = null;
            this.comments = null;
        }

        private void CopyMembers(Card old)
        {
            this.keyword = old.keyword;
            this.rawValue = old.rawValue;
            this.comments = old.comments;
        }

        public object Clone()
        {
            return new Card(this);
        }

        #endregion
        #region Value accessors

        public String GetString()
        {
            return rawValue.Trim('\'');
        }

        public void SetValue(String value)
        {
            // Quote and escape
            rawValue = "'" + value.Replace("'", "''") + "'";
        }

        public Boolean GetBoolean()
        {
            if (StringComparer.InvariantCultureIgnoreCase.Compare(rawValue.Trim(), "T") == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void SetValue(Boolean value)
        {
            rawValue = value ? "T" : "F";
        }

        public Int32 GetInt32()
        {
            return Int32.Parse(rawValue, CultureInfo.InvariantCulture);
        }

        public void SetValue(Int32 value)
        {
            rawValue = value.ToString(CultureInfo.InvariantCulture);
        }

        public Double GetDouble()
        {
            return Double.Parse(rawValue, CultureInfo.InvariantCulture);
        }

        public void SetValue(Double value)
        {
            rawValue = value.ToString(CultureInfo.InvariantCulture);
        }

        // *** TODO: implement other types of getters and setters

        #endregion
        #region Read functions

        /// <summary>
        /// Reads a single card image from the stream at the current position
        /// </summary>
        /// <param name="stream"></param>
        /// <remarks>
        /// A card image consits of a 80 char long line, first eight characters
        /// containing the keyword. Strings are delimited with 's, commend is
        /// separated by a /.
        /// If no '= ' sequence found at bytes 8 and 9, the entire line is treated
        /// as a comment.
        /// </remarks>
        internal void Read(Stream stream)
        {
            // TODO: handle continue

            var buffer = new byte[80];
            var res = stream.Read(buffer, 0, buffer.Length);

            if (res == 0)
            {
                throw new EndOfStreamException();
            }
            else if (res < buffer.Length)
            {
                throw new FitsException("Unexpected end of stream.");  // *** TODO
            }

            string line = Encoding.ASCII.GetString(buffer);

            // bytes 0-7: keyword name
            this.keyword = line.Substring(0, 8).Trim();

            if (line[8] == '=' && line[9] == ' ')
            {
                // bytes 8-9: "= " sequence if there's value
                ReadValue(line.Substring(10));
            }
            else if (FitsFile.Comparer.Compare(keyword, Constants.FitsKeywordComment) == 0 ||
                FitsFile.Comparer.Compare(keyword, Constants.FitsKeywordHierarch) == 0 ||
                FitsFile.Comparer.Compare(keyword, Constants.FitsKeywordContinue) == 0 ||
                keyword == String.Empty)
            {
                rawValue = null;
                comments = line.Substring(10);
            }
        }

        private void ReadValue(string line)
        {
            // Try to match a string

            Match m = StringRegex.Match(line);

            int ci;
            if (m.Success)
            {
                rawValue = m.Value.Replace("''", "'");     // Handle escapes
                ci = line.IndexOf('/', m.Length);
            }
            else
            {
                ci = line.IndexOf('/', m.Length);
                if (ci > 0)
                {
                    rawValue = line.Substring(0, ci - 1);
                }
                else
                {
                    rawValue = line.Substring(0);
                }
            }

            if (ci >= 0)
            {
                comments = line.Substring(ci + 1);
            }
            else
            {
                comments = null;
            }
        }

        #endregion
        #region Write functions

        public void Write(Stream stream)
        {
            // TODO: implement multi-line values using the CONTINUE keyword

            // Header keyword padded to eight characters
            string line = keyword.PadRight(8);

            // header keyword
            if (!IsComment && !IsContinue && !IsEnd)
            {
                line += "= ";
            }
            else
            {
                line += "  ";
            }

            line += rawValue;

            var buffer = new byte[80];
            Encoding.ASCII.GetBytes(line, 0, Math.Min(line.Length, 80), buffer, 0);

            stream.Write(buffer, 0, buffer.Length);
        }

        #endregion

        private void GetKeywordParts(out string keyword, out int id, out int order)
        {
            // Split into keyword and counter
            var m = KeywordRegex.Match(this.keyword);

            keyword = m.Groups[1].Value;

            if (!int.TryParse(m.Groups[2].Value, out id))
            {
                id = -1;
            }

            if (Constants.KeywordOrder.ContainsKey(keyword))
            {
                order = Constants.KeywordOrder[keyword];
            }
            else
            {
                order = int.MaxValue / 2;
            }
        }

        public int CompareTo(object obj)
        {
            var other = (Card)obj;

            string akey, bkey;
            int aid, bid;
            int aord, bord;

            this.GetKeywordParts(out akey, out aid, out aord);
            other.GetKeywordParts(out bkey, out bid, out bord);

            if (aord != bord)
            {
                // First sort on keyword
                return aord - bord;
            }
            else if (aid != bid)
            {
                // Or on keyword id, if possible
                return aid - bid;
            }
            else
            {
                // Otherwise assume they are equal
                return 0;
            }
        }

        public override string ToString()
        {
            return String.Format("{0:8}= {1}", keyword, rawValue);
        }
    }
}
