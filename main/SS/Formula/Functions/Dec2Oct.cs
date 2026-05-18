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

    /**
     * Implementation for Excel DEC2OCT() function.
     * Syntax: DEC2OCT(number, [places])
     *
     * Converts a decimal integer in [-2^29, 2^29-1] to its octal representation.
     * Negative numbers use 30-bit two's-complement notation across 10 octal
     * characters; positive numbers use the minimum representation, optionally
     * padded to <c>places</c> characters.
     */
    public class Dec2Oct : Var1or2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new Dec2Oct();

        private const long MinValue = -536870912L;   // -2^29
        private const long MaxValue = 536870911L;    //  2^29 - 1
        private const long Modulus = 1L << 30;       //  8^10
        private const int FullWidth = 10;

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval numberVE)
        {
            return Evaluate(srcRowIndex, srcColumnIndex, numberVE, null);
        }

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval numberVE, ValueEval placesVE)
        {
            double number;
            try
            {
                number = OperandResolver.CoerceValueToDouble(OperandResolver.GetSingleValue(numberVE, srcRowIndex, srcColumnIndex));
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }
            if (double.IsNaN(number))
            {
                return ErrorEval.VALUE_INVALID;
            }
            if (number < MinValue || number > MaxValue)
            {
                return ErrorEval.NUM_ERROR;
            }
            long value = (long)number;

            int places = 0;
            if (placesVE != null && value >= 0)
            {
                try
                {
                    ValueEval ve = OperandResolver.GetSingleValue(placesVE, srcRowIndex, srcColumnIndex);
                    if (!(ve is BlankEval))
                    {
                        double parsed = OperandResolver.CoerceValueToDouble(ve);
                        if (double.IsNaN(parsed))
                        {
                            return ErrorEval.VALUE_INVALID;
                        }
                        places = (int)Math.Floor(parsed);
                        if (places < 0 || places > FullWidth)
                        {
                            return ErrorEval.NUM_ERROR;
                        }
                    }
                }
                catch (EvaluationException e)
                {
                    return e.GetErrorEval();
                }
            }

            string encoded = ToOctal(value < 0 ? Modulus + value : value);
            if (value < 0)
            {
                encoded = encoded.PadLeft(FullWidth, '0');
            }
            else if (places > 0)
            {
                if (encoded.Length > places)
                {
                    return ErrorEval.NUM_ERROR;
                }
                encoded = encoded.PadLeft(places, '0');
            }
            return new StringEval(encoded);
        }

        private static string ToOctal(long value)
        {
            if (value == 0)
            {
                return "0";
            }
            StringBuilder sb = new StringBuilder();
            while (value > 0)
            {
                sb.Insert(0, (char)('0' + value % 8));
                value /= 8;
            }
            return sb.ToString();
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            switch (args.Length)
            {
                case 1:
                    return Evaluate(ec.RowIndex, ec.ColumnIndex, args[0]);
                case 2:
                    return Evaluate(ec.RowIndex, ec.ColumnIndex, args[0], args[1]);
                default:
                    return ErrorEval.VALUE_INVALID;
            }
        }
    }
}
