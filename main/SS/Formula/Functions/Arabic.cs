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
    using NPOI.SS.Formula.Eval;

    /**
     * Implementation for Excel ARABIC() function (Excel 2013+).
     * Syntax: ARABIC(text)
     *
     * Converts a Roman numeral string to its Arabic integer value.  Excel
     * accepts a leading minus sign and is case-insensitive.  Unrecognised input
     * yields #VALUE!.
     */
    public class Arabic : Fixed1ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new Arabic();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0)
        {
            string text;
            try
            {
                text = OperandResolver.CoerceValueToString(OperandResolver.GetSingleValue(arg0, srcRowIndex, srcColumnIndex));
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }
            if (text == null)
            {
                return ErrorEval.VALUE_INVALID;
            }
            text = text.Trim();
            if (text.Length == 0)
            {
                return new NumberEval(0);
            }

            int sign = 1;
            int start = 0;
            if (text[0] == '-')
            {
                sign = -1;
                start = 1;
            }

            int total = 0;
            int previous = 0;
            for (int i = text.Length - 1; i >= start; i--)
            {
                int digit = RomanDigit(text[i]);
                if (digit < 0)
                {
                    return ErrorEval.VALUE_INVALID;
                }
                if (digit < previous)
                {
                    total -= digit;
                }
                else
                {
                    total += digit;
                    previous = digit;
                }
            }
            if (total > 255000)
            {
                // ARABIC is bounded to 255000 in Excel
                return ErrorEval.VALUE_INVALID;
            }
            return new NumberEval(sign * total);
        }

        private static int RomanDigit(char c)
        {
            switch (c)
            {
                case 'I': case 'i': return 1;
                case 'V': case 'v': return 5;
                case 'X': case 'x': return 10;
                case 'L': case 'l': return 50;
                case 'C': case 'c': return 100;
                case 'D': case 'd': return 500;
                case 'M': case 'm': return 1000;
                default: return -1;
            }
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return args.Length != 1 ? ErrorEval.VALUE_INVALID : Evaluate(ec.RowIndex, ec.ColumnIndex, args[0]);
        }
    }
}
