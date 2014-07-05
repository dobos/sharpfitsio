using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jhu.SharpFitsIO
{
    /// <summary>
    /// Represents a binary table HDU column
    /// </summary>
    [Serializable]
    public class FitsTableColumn : ICloneable
    {
        private int id;
        private string name;
        private FitsDataType dataType;
        private string unit;
        private string format;
        private string comment;

        #region Properties

        /// <summary>
        /// Gets or sets the ID of the columns
        /// </summary>
        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// Gets or sets the name of the column.
        /// </summary>
        /// <remarks>
        /// Corresponds to the TTYPEn keyword
        /// </remarks>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Get or sets the data type of the column.
        /// </summary>
        /// <remarks>
        /// Corresponds to the TFORMn keyword
        /// </remarks>
        public FitsDataType DataType
        {
            get { return dataType; }
            set { dataType = value; }
        }

        /// <summary>
        /// Gets or sets the unit of the column.
        /// </summary>
        /// <remarks>
        /// Corresponds to the TUNITn keyword
        /// </remarks>
        public string Unit
        {
            get { return unit; }
            set { unit = value; }
        }

        /// <summary>
        /// Gets or sets the display format of the column.
        /// </summary>
        /// <remarks>
        /// Corresponds to the TFORMn keyword
        /// </remarks>
        public string Format
        {
            get { return format; }
            set { format = value; }
        }

        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }

        #endregion
        #region Constructors and initializers

        internal FitsTableColumn()
        {
            InitializeMembers();
        }

        internal FitsTableColumn(FitsTableColumn old)
        {
            CopyMembers(old);
        }

        private void InitializeMembers()
        {
            this.id = 0;
            this.name = null;
            this.dataType = null;
            this.unit = null;
            this.format = null;
        }

        private void CopyMembers(FitsTableColumn old)
        {
            this.id = old.id;
            this.name = old.name;
            this.dataType = new FitsDataType(old.dataType);
            this.unit = old.unit;
            this.format = old.format;
        }

        public object Clone()
        {
            return new FitsTableColumn(this);
        }

        #endregion
        #region Static factory functions

        public static FitsTableColumn Create(string name, FitsDataType dataType)
        {
            return Create(name, dataType, null, null);
        }

        public static FitsTableColumn Create(string name, FitsDataType dataType, string unit, string format)
        {
            var col = new FitsTableColumn();

            col.name = name;
            col.dataType = dataType;
            col.unit = unit;
            col.format = format;

            return col;
        }

        #endregion

        internal static FitsTableColumn CreateFromCards(BinaryTableHdu hdu, int index)
        {
            Card card;

            // Create a new column
            var column = new FitsTableColumn()
            {
                ID = index,
            };

            // Get data type
            if (!hdu.Cards.TryGet(Constants.FitsKeywordTForm, index, out card))
            {
                throw new FitsException("Keyword expected but not found:"); // TODO
            }

            column.DataType = FitsDataType.CreateFromTForm(card.GetString().Trim());

            // Set optional parameters

            // --- Column name
            if (hdu.Cards.TryGet(Constants.FitsKeywordTType, index, out card))
            {
                column.Name = card.GetString().Trim();
            }

            // Unit
            if (hdu.Cards.TryGet(Constants.FitsKeywordTUnit, index, out card))
            {
                column.Unit = card.GetString().Trim();
            }

            // Null value equivalent
            if (hdu.Cards.TryGet(Constants.FitsKeywordTNull, index, out card))
            {
                column.DataType.NullValue = card.GetInt32();
            }

            // Scale
            if (hdu.Cards.TryGet(Constants.FitsKeywordTScal, index, out card))
            {
                column.DataType.Scale = card.GetDouble();
            }

            // Zero offset
            if (hdu.Cards.TryGet(Constants.FitsKeywordTZero, index, out card))
            {
                column.DataType.Zero = card.GetDouble();
            }

            // Format
            if (hdu.Cards.TryGet(Constants.FitsKeywordTDisp, index, out card))
            {
                column.Format = card.GetString().Trim();
            }

            // Dimensions
            // TODO: implement TDIMn

            return column;
        }

        internal void SetCards(BinaryTableHdu hdu)
        {
            Card card;

            // TFORMn
            card = new Card(Constants.FitsKeywordTForm, this.id);
            card.SetValue(dataType.GetTFormString());
            hdu.Cards.Set(card);

            // TTYPEn
            if (name != null)
            {
                card = new Card(Constants.FitsKeywordTType, this.id);
                card.SetValue(name);
                hdu.Cards.Set(card);
            }

            // TUNITn
            if (unit != null)
            {
                card = new Card(Constants.FitsKeywordTUnit, this.id);
                card.SetValue(unit);
                hdu.Cards.Set(card);
            }

            // TNULLn
            if (dataType.NullValue.HasValue)
            {
                card = new Card(Constants.FitsKeywordTNull, this.id);
                card.SetValue(dataType.NullValue.Value);
                hdu.Cards.Set(card);
            }

            // TSCALn
            if (dataType.Scale.HasValue)
            {
                card = new Card(Constants.FitsKeywordTScal, this.id);
                card.SetValue(dataType.Scale.Value);
                hdu.Cards.Set(card);
            }

            // TZEROn
            if (dataType.Zero.HasValue)
            {
                card = new Card(Constants.FitsKeywordTZero, this.id);
                card.SetValue(dataType.Zero.Value);
                hdu.Cards.Set(card);
            }

            // TDISPn
            if (format != null)
            {
                card = new Card(Constants.FitsKeywordTDisp, this.id);
                card.SetValue(format);
                hdu.Cards.Set(card);
            }
        }
    }
}
