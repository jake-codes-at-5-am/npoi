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
    using NPOI.SS.Formula.Eval;

    /**
     * Implementation for Excel UNICODE() function (Excel 2013+).
     * Syntax: UNICODE(text)
     *
     * Returns the Unicode code point of the first character in <c>text</c>.
     * Empty input yields #VALUE!. Surrogate pairs return the full code point so
     * UNICODE/UNICHAR round-trip correctly for supplementary characters.
     */
    public class Unicode : Fixed1ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new Unicode();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0)
        {
            string text;
            try
            {
                text = TextFunction.EvaluateStringArg(arg0, srcRowIndex, srcColumnIndex);
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }
            if (string.IsNullOrEmpty(text))
            {
                return ErrorEval.VALUE_INVALID;
            }
            int codePoint = char.ConvertToUtf32(text, 0);
            return new NumberEval(codePoint);
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return args.Length != 1 ? ErrorEval.VALUE_INVALID : Evaluate(ec.RowIndex, ec.ColumnIndex, args[0]);
        }
    }

    /**
     * Implementation for Excel UNICHAR() function (Excel 2013+).
     * Syntax: UNICHAR(number)
     *
     * Returns the Unicode character whose code point is <c>number</c>. The
     * value must be a positive integer in the valid Unicode range; surrogate
     * code points are rejected with #VALUE! to match Excel.
     */
    public class UniChar : Fixed1ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new UniChar();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0)
        {
            int codePoint;
            try
            {
                codePoint = OperandResolver.CoerceValueToInt(OperandResolver.GetSingleValue(arg0, srcRowIndex, srcColumnIndex));
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }
            if (codePoint < 1 || codePoint > 0x10FFFF || (codePoint >= 0xD800 && codePoint <= 0xDFFF))
            {
                return ErrorEval.VALUE_INVALID;
            }
            return new StringEval(char.ConvertFromUtf32(codePoint));
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return args.Length != 1 ? ErrorEval.VALUE_INVALID : Evaluate(ec.RowIndex, ec.ColumnIndex, args[0]);
        }
    }
}
