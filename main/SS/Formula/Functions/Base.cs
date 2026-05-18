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
     * Implementation for Excel BASE() function.
     * Syntax: BASE(number, radix, [min_length])
     *
     * Converts a non-negative integer into its representation in the given radix
     * (2..36). When min_length is provided, the result is left-padded with zeros.
     */
    public class Base : Var2or3ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new Base();

        private const long MaxNumber = (long)((1L << 53) - 1);

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval numberVE, ValueEval radixVE)
        {
            return Evaluate(srcRowIndex, srcColumnIndex, numberVE, radixVE, null);
        }

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval numberVE, ValueEval radixVE, ValueEval minLengthVE)
        {
            double number;
            int radix;
            int minLength = 0;
            try
            {
                number = OperandResolver.CoerceValueToDouble(OperandResolver.GetSingleValue(numberVE, srcRowIndex, srcColumnIndex));
                radix = (int)Math.Floor(OperandResolver.CoerceValueToDouble(OperandResolver.GetSingleValue(radixVE, srcRowIndex, srcColumnIndex)));
                if (minLengthVE != null)
                {
                    ValueEval ve = OperandResolver.GetSingleValue(minLengthVE, srcRowIndex, srcColumnIndex);
                    if (!(ve is BlankEval))
                    {
                        minLength = (int)Math.Floor(OperandResolver.CoerceValueToDouble(ve));
                    }
                }
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            if (double.IsNaN(number) || number < 0 || number > MaxNumber)
            {
                return ErrorEval.NUM_ERROR;
            }
            if (radix < 2 || radix > 36)
            {
                return ErrorEval.NUM_ERROR;
            }
            if (minLength < 0 || minLength > 255)
            {
                return ErrorEval.NUM_ERROR;
            }

            long value = (long)Math.Floor(number);
            string encoded = Encode(value, radix);
            if (minLength > 0 && encoded.Length < minLength)
            {
                encoded = encoded.PadLeft(minLength, '0');
            }
            return new StringEval(encoded);
        }

        private static string Encode(long value, int radix)
        {
            if (value == 0)
            {
                return "0";
            }
            StringBuilder sb = new StringBuilder();
            while (value > 0)
            {
                long digit = value % radix;
                char c = digit < 10 ? (char)('0' + digit) : (char)('A' + (digit - 10));
                sb.Insert(0, c);
                value /= radix;
            }
            return sb.ToString();
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            switch (args.Length)
            {
                case 2:
                    return Evaluate(ec.RowIndex, ec.ColumnIndex, args[0], args[1]);
                case 3:
                    return Evaluate(ec.RowIndex, ec.ColumnIndex, args[0], args[1], args[2]);
                default:
                    return ErrorEval.VALUE_INVALID;
            }
        }
    }
}
