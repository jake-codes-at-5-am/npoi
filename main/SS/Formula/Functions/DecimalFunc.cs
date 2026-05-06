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
    using NPOI.SS.Formula.Eval;

    /**
     * Implementation for Excel DECIMAL() function.
     * Syntax: DECIMAL(text, radix)
     *
     * Inverse of BASE: parses a non-negative integer string written in the given
     * radix (2..36) and returns its decimal value.
     */
    public class DecimalFunc : Fixed2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new DecimalFunc();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval textVE, ValueEval radixVE)
        {
            string text;
            int radix;
            try
            {
                text = OperandResolver.CoerceValueToString(OperandResolver.GetSingleValue(textVE, srcRowIndex, srcColumnIndex));
                radix = (int)Math.Floor(OperandResolver.CoerceValueToDouble(OperandResolver.GetSingleValue(radixVE, srcRowIndex, srcColumnIndex)));
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            if (radix < 2 || radix > 36)
            {
                return ErrorEval.NUM_ERROR;
            }
            text = text.Trim();
            if (text.Length == 0 || text.Length > 255)
            {
                return ErrorEval.NUM_ERROR;
            }

            long value = 0;
            foreach (char ch in text)
            {
                int digit;
                if (ch >= '0' && ch <= '9')
                {
                    digit = ch - '0';
                }
                else if (ch >= 'A' && ch <= 'Z')
                {
                    digit = 10 + (ch - 'A');
                }
                else if (ch >= 'a' && ch <= 'z')
                {
                    digit = 10 + (ch - 'a');
                }
                else
                {
                    return ErrorEval.NUM_ERROR;
                }
                if (digit >= radix)
                {
                    return ErrorEval.NUM_ERROR;
                }
                value = value * radix + digit;
                if (value < 0)
                {
                    return ErrorEval.NUM_ERROR; // overflow
                }
            }
            return new NumberEval(value);
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return args.Length != 2 ? ErrorEval.VALUE_INVALID : Evaluate(ec.RowIndex, ec.ColumnIndex, args[0], args[1]);
        }
    }
}
