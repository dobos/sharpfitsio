﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jhu.SharpFitsIO
{
    internal static class Constants
    {
        public const int FitsBlockSize = 2880;

        public const string FitsKeywordSimple = "SIMPLE";
        public const string FitsKeywordExtend = "EXTEND";
        public const string FitsKeywordXtension = "XTENSION";
        public const string FitsKeywordExtName = "EXTNAME";
        public const string FitsKeywordBitPix = "BITPIX";
        public const string FitsKeywordNAxis = "NAXIS";
        public const string FitsKeywordTFields = "TFIELDS";
        public const string FitsKeywordPCount = "PCOUNT";
        public const string FitsKeywordGCount = "GCOUNT";
        public const string FitsKeywordTHeap = "THEAP";
        public const string FitsKeywordTForm = "TFORM";
        public const string FitsKeywordTType = "TTYPE";
        public const string FitsKeywordTUnit = "TUNIT";
        public const string FitsKeywordTNull = "TNULL";
        public const string FitsKeywordTScal = "TSCAL";
        public const string FitsKeywordTZero = "TZERO";
        public const string FitsKeywordTDisp = "TDISP";
        public const string FitsKeywordLongStrn = "LONGSTRN";
        public const string FitsKeywordComment = "COMMENT";
        public const string FitsKeywordContinue = "CONTINUE";
        public const string FitsKeywordHierarch = "HIERARCH";
        public const string FitsKeywordEnd = "END";

        public const string FitsExtensionBinTable = "BINTABLE";
        public const string FitsExtensionImage = "IMAGE";

        public const string FitsTypeNameLogical = "Logical";
        public const string FitsTypeNameBit = "Bit";
        public const string FitsTypeNameByte = "Byte";
        public const string FitsTypeNameInt16 = "Integer16";
        public const string FitsTypeNameInt32 = "Integer32";
        public const string FitsTypeNameInt64 = "Integer64";
        public const string FitsTypeNameChar = "Character";
        public const string FitsTypeNameSingle = "Single";
        public const string FitsTypeNameDouble = "Double";
        public const string FitsTypeNameSingleComplex = "SingleComplex";
        public const string FitsTypeNameDoubleComplex = "DoubleComplex";
        public const string FitsTypeNameArray = "Array";

        public const string HeaderValueFormat = "{0,20}";
        public const byte FitsLogicalNull = 0x00;
        public const byte FitsLogicalTrue = 0x54;               // 'T'
        public const byte FitsLogicalTrueAlternate = 0x74;      // 't'
        public const byte FitsLogicalFalse = 0x46;              // 'F'
        public const byte FitsLogicalFalseAlternate = 0x66;     // 'f'

        /// <summary>
        /// Gets a set of keywords that are not to be treated as unique.
        /// </summary>
        public static readonly HashSet<string> CommentKeywords = new HashSet<string>(FitsFile.Comparer)
        {
            Constants.FitsKeywordComment,
            Constants.FitsKeywordContinue,
            Constants.FitsKeywordHierarch
        };

        public static readonly Dictionary<string, int> KeywordOrder = new Dictionary<string, int>(FitsFile.Comparer)
        {
            {FitsKeywordSimple, 0},
            {FitsKeywordXtension, 1},
            {FitsKeywordBitPix, 2},
            {FitsKeywordNAxis, 3},
            {FitsKeywordPCount, 4},
            {FitsKeywordGCount, 5},
            {FitsKeywordTFields, 6},

            {FitsKeywordEnd, int.MaxValue},
        };
    }
}
