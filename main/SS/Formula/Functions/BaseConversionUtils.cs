/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

namespace NPOI.SS.Formula.Functions
{
    using System;
    using System.Text;
    using NPOI.SS.Formula.Eval;

    /// <summary>
    /// Shared helpers for the BIN2HEX / BIN2OCT / HEX2BIN / HEX2OCT / OCT2BIN /
    /// OCT2HEX style conversion functions. All of these read a string in some
    /// source radix (with optional two's-complement sign on a 10-character
    /// width), convert to decimal, then re-encode in the destination radix
    /// with optional zero-padding via the <c>places</c> argument.
    /// </summary>
    internal static class BaseConversionUtils
    {
        internal enum OutputBase
        {
            Binary = 2,
            Octal = 8,
            Hex = 16
        }

        /// <summary>Maximum width Excel accepts for any of these functions, in characters of either base.</summary>
        internal const int MaxNumberOfPlaces = 10;

        /// <summary>The 10-bit signed range for binary outputs.</summary>
        private const long BinaryMin = -512L;
        private const long BinaryMax = 511L;

        /// <summary>The 30-bit signed range for octal outputs (OCT supports 10 octal digits = 30 bits).</summary>
        private const long OctalMin = -536870912L;
        private const long OctalMax = 536870911L;

        /// <summary>The 40-bit signed range for hex outputs (HEX supports 10 hex digits = 40 bits).</summary>
        private const long HexMin = -549755813888L;
        private const long HexMax = 549755813887L;

        /// <summary>
        /// Performs a two-step conversion: source radix (string) -&gt; decimal -&gt;
        /// destination radix (string with optional zero-padding).
        /// </summary>
        internal static ValueEval Convert(
            ValueEval numberVE,
            ValueEval placesVE,
            int srcRowIndex,
            int srcColumnIndex,
            int inputBase,
            int maxInputPlaces,
            OutputBase outputBase)
        {
            ValueEval resolved;
            try
            {
                resolved = OperandResolver.GetSingleValue(numberVE, srcRowIndex, srcColumnIndex);
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            string source = OperandResolver.CoerceValueToString(resolved);
            if (string.IsNullOrEmpty(source))
            {
                return new StringEval("0");
            }

            double decimalValue;
            try
            {
                decimalValue = BaseNumberUtils.ConvertToDecimal(source, inputBase, maxInputPlaces);
            }
            catch (ArgumentException)
            {
                return ErrorEval.NUM_ERROR;
            }

            long longValue = (long)decimalValue;
            if (!IsInRange(longValue, outputBase))
            {
                return ErrorEval.NUM_ERROR;
            }

            int placesNumber;
            ValueEval placesError = ResolvePlaces(placesVE, srcRowIndex, srcColumnIndex, longValue, out placesNumber);
            if (placesError != null)
            {
                return placesError;
            }

            string encoded = Encode(longValue, outputBase);
            if (longValue < 0)
            {
                // negative numbers always use the full 10-character two's complement
                // form; the places argument is ignored, matching Excel's behaviour.
                return new StringEval(encoded);
            }

            if (placesNumber > 0)
            {
                if (encoded.Length > placesNumber)
                {
                    return ErrorEval.NUM_ERROR;
                }
                encoded = encoded.PadLeft(placesNumber, '0');
            }
            return new StringEval(encoded);
        }

        private static ValueEval ResolvePlaces(
            ValueEval placesVE,
            int srcRowIndex,
            int srcColumnIndex,
            long signedValue,
            out int placesNumber)
        {
            placesNumber = 0;
            if (placesVE == null || signedValue < 0)
            {
                return null;
            }

            ValueEval resolved;
            try
            {
                resolved = OperandResolver.GetSingleValue(placesVE, srcRowIndex, srcColumnIndex);
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            if (resolved is BlankEval)
            {
                return null;
            }

            string placesStr = OperandResolver.CoerceValueToString(resolved);
            double parsed = OperandResolver.ParseDouble(placesStr);
            if (double.IsNaN(parsed))
            {
                return ErrorEval.VALUE_INVALID;
            }

            int truncated = (int)Math.Floor(parsed);
            if (truncated < 0 || truncated > MaxNumberOfPlaces)
            {
                return ErrorEval.NUM_ERROR;
            }
            placesNumber = truncated;
            return null;
        }

        private static bool IsInRange(long value, OutputBase outputBase)
        {
            switch (outputBase)
            {
                case OutputBase.Binary:
                    return value >= BinaryMin && value <= BinaryMax;
                case OutputBase.Octal:
                    return value >= OctalMin && value <= OctalMax;
                case OutputBase.Hex:
                    return value >= HexMin && value <= HexMax;
                default:
                    return false;
            }
        }

        private static string Encode(long value, OutputBase outputBase)
        {
            int radix = (int)outputBase;
            if (value >= 0)
            {
                return ToRadixString(value, radix).ToUpperInvariant();
            }

            // Two's complement representation in <see cref="MaxNumberOfPlaces"/>
            // characters of the target radix. We compute (radix^width + value),
            // which corresponds to the unsigned representation of the two's
            // complement bit pattern within that width.
            long modulus = ModulusFor(outputBase);
            long unsigned = modulus + value;
            string raw = ToRadixString(unsigned, radix).ToUpperInvariant();
            return raw.PadLeft(MaxNumberOfPlaces, '0');
        }

        private static long ModulusFor(OutputBase outputBase)
        {
            switch (outputBase)
            {
                case OutputBase.Binary: return 1L << 10; // 2^10
                case OutputBase.Octal: return 1L << 30;  // 8^10
                case OutputBase.Hex: return 1L << 40;    // 16^10
                default: throw new ArgumentOutOfRangeException(nameof(outputBase));
            }
        }

        private static string ToRadixString(long value, int radix)
        {
            if (value == 0)
            {
                return "0";
            }
            // System.Convert.ToString supports base 2/8/16 for Int32 only, so we
            // implement the conversion ourselves to handle the full long range.
            StringBuilder sb = new StringBuilder();
            long remaining = value;
            while (remaining > 0)
            {
                long digit = remaining % radix;
                char c = digit < 10 ? (char)('0' + digit) : (char)('A' + (digit - 10));
                sb.Insert(0, c);
                remaining /= radix;
            }
            return sb.ToString();
        }
    }
}
