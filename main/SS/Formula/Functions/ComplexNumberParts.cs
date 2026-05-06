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
    using System.Globalization;
    using System.Text;
    using NPOI.SS.Formula.Eval;

    /// <summary>
    /// Parses Excel-style complex-number strings (e.g. "3+4i", "-2.5j", "i") into
    /// real and imaginary components, and formats them back. Used by the IM*
    /// family of engineering functions so each function does not have to repeat
    /// the parsing logic.
    /// </summary>
    internal sealed class ComplexNumberParts
    {
        internal double Real { get; }
        internal double Imaginary { get; }
        internal string Suffix { get; }

        internal ComplexNumberParts(double real, double imaginary, string suffix)
        {
            Real = real;
            Imaginary = imaginary;
            Suffix = suffix == "j" ? "j" : "i";
        }

        internal double Magnitude => Math.Sqrt(Real * Real + Imaginary * Imaginary);
        internal double Argument => Math.Atan2(Imaginary, Real);

        internal static ComplexNumberParts Add(ComplexNumberParts a, ComplexNumberParts b, string suffix) =>
            new ComplexNumberParts(a.Real + b.Real, a.Imaginary + b.Imaginary, suffix);

        internal static ComplexNumberParts Subtract(ComplexNumberParts a, ComplexNumberParts b, string suffix) =>
            new ComplexNumberParts(a.Real - b.Real, a.Imaginary - b.Imaginary, suffix);

        internal static ComplexNumberParts Multiply(ComplexNumberParts a, ComplexNumberParts b, string suffix) =>
            new ComplexNumberParts(
                a.Real * b.Real - a.Imaginary * b.Imaginary,
                a.Real * b.Imaginary + a.Imaginary * b.Real,
                suffix);

        internal static ComplexNumberParts Divide(ComplexNumberParts a, ComplexNumberParts b, string suffix)
        {
            double denom = b.Real * b.Real + b.Imaginary * b.Imaginary;
            if (denom == 0)
            {
                return null;
            }
            return new ComplexNumberParts(
                (a.Real * b.Real + a.Imaginary * b.Imaginary) / denom,
                (a.Imaginary * b.Real - a.Real * b.Imaginary) / denom,
                suffix);
        }

        internal static string MergeSuffix(ComplexNumberParts a, ComplexNumberParts b, out bool mismatch)
        {
            // If both arguments use a suffix and they disagree, Excel returns
            // #VALUE!. When one of them is a pure real value (parsed as plain
            // number, defaulted to "i"), we adopt the other's suffix.
            mismatch = false;
            bool aHasImag = a.Imaginary != 0;
            bool bHasImag = b.Imaginary != 0;
            if (aHasImag && bHasImag && a.Suffix != b.Suffix)
            {
                mismatch = true;
                return a.Suffix;
            }
            if (bHasImag) return b.Suffix;
            if (aHasImag) return a.Suffix;
            return a.Suffix;
        }

        /// <summary>
        /// Parse a complex-number string. Returns null if <paramref name="text"/> is
        /// not a valid representation, in which case the caller should return
        /// <c>ErrorEval.NUM_ERROR</c> (matching Excel).
        /// </summary>
        internal static ComplexNumberParts TryParse(string text)
        {
            if (text == null)
            {
                return null;
            }

            string trimmed = text.Trim();
            if (trimmed.Length == 0)
            {
                return new ComplexNumberParts(0, 0, "i");
            }

            string suffix = null;
            char last = trimmed[trimmed.Length - 1];
            if (last == 'i' || last == 'j')
            {
                suffix = last.ToString();
                trimmed = trimmed.Substring(0, trimmed.Length - 1);
            }
            else if (last == 'I' || last == 'J')
            {
                // Excel rejects uppercase suffixes
                return null;
            }

            if (suffix == null)
            {
                if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double real))
                {
                    return null;
                }
                return new ComplexNumberParts(real, 0, "i");
            }

            // suffix present; the remainder is at most "<real><sign><imag>" or "<imag>"
            if (trimmed.Length == 0)
            {
                // input was just "i" or "j"
                return new ComplexNumberParts(0, 1, suffix);
            }
            if (trimmed == "+" || trimmed == "-")
            {
                return new ComplexNumberParts(0, trimmed == "-" ? -1 : 1, suffix);
            }

            int splitIndex = FindImaginarySignSplit(trimmed);
            string realPart;
            string imagPart;
            if (splitIndex < 0)
            {
                realPart = string.Empty;
                imagPart = trimmed;
            }
            else
            {
                realPart = trimmed.Substring(0, splitIndex);
                imagPart = trimmed.Substring(splitIndex);
            }

            double realValue = 0;
            if (realPart.Length > 0)
            {
                if (!double.TryParse(realPart, NumberStyles.Float, CultureInfo.InvariantCulture, out realValue))
                {
                    return null;
                }
            }

            double imagValue;
            if (imagPart.Length == 0 || imagPart == "+" || imagPart == "-")
            {
                imagValue = imagPart == "-" ? -1 : 1;
            }
            else if (!double.TryParse(imagPart, NumberStyles.Float, CultureInfo.InvariantCulture, out imagValue))
            {
                return null;
            }

            return new ComplexNumberParts(realValue, imagValue, suffix);
        }

        private static int FindImaginarySignSplit(string text)
        {
            // Locate the sign that separates the real and imaginary parts. Skip
            // index 0 (that sign belongs to the real part) and skip any sign
            // that immediately follows an exponent marker in scientific notation.
            for (int i = text.Length - 1; i > 0; i--)
            {
                char c = text[i];
                if (c != '+' && c != '-')
                {
                    continue;
                }
                char prev = text[i - 1];
                if (prev == 'e' || prev == 'E')
                {
                    continue;
                }
                return i;
            }
            return -1;
        }

        /// <summary>
        /// Format a complex value the same way Excel's COMPLEX function does.
        /// </summary>
        internal static string Format(double real, double imaginary, string suffix)
        {
            if (suffix != "i" && suffix != "j")
            {
                suffix = "i";
            }
            StringBuilder sb = new StringBuilder();
            if (real != 0 || imaginary == 0)
            {
                sb.Append(FormatNumber(real));
            }
            if (imaginary != 0)
            {
                if (sb.Length != 0 && imaginary > 0)
                {
                    sb.Append('+');
                }
                if (imaginary == -1)
                {
                    sb.Append('-');
                }
                else if (imaginary != 1)
                {
                    sb.Append(FormatNumber(imaginary));
                }
                sb.Append(suffix);
            }
            return sb.ToString();
        }

        private static string FormatNumber(double value)
        {
            if (value == Math.Floor(value) && !double.IsInfinity(value)
                && value <= long.MaxValue && value >= long.MinValue)
            {
                return ((long)value).ToString(CultureInfo.InvariantCulture);
            }
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Pull a single complex-number argument from a ValueEval. Returns
        /// either a parsed ComplexNumberParts (success) or a ValueEval error
        /// to propagate (failure).
        /// </summary>
        internal static bool TryEvaluateArg(
            ValueEval arg,
            int srcRowIndex,
            int srcColumnIndex,
            out ComplexNumberParts parts,
            out ValueEval errorEval)
        {
            parts = null;
            errorEval = null;
            ValueEval resolved;
            try
            {
                resolved = OperandResolver.GetSingleValue(arg, srcRowIndex, srcColumnIndex);
            }
            catch (EvaluationException e)
            {
                errorEval = e.GetErrorEval();
                return false;
            }
            string asText = OperandResolver.CoerceValueToString(resolved);
            parts = TryParse(asText);
            if (parts == null)
            {
                errorEval = ErrorEval.NUM_ERROR;
                return false;
            }
            return true;
        }
    }
}
