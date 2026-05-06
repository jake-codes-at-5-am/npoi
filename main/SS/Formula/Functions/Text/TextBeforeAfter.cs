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

    /// <summary>
    /// Shared engine for the modern Excel TEXTBEFORE / TEXTAFTER functions.
    /// Both functions use the same argument list and locate logic, only differ
    /// in which side of the matched delimiter they return.
    /// </summary>
    internal static class TextBeforeAfter
    {
        internal enum Mode
        {
            Before,
            After
        }

        internal static ValueEval Evaluate(Mode mode, ValueEval[] args, OperationEvaluationContext ec)
        {
            if (args == null || args.Length < 2 || args.Length > 6)
            {
                return ErrorEval.VALUE_INVALID;
            }

            string text;
            string delimiter;
            int instanceNum = 1;
            int matchMode = 0;
            int matchEnd = 0;
            ValueEval ifNotFound = null;

            try
            {
                text = TextFunction.EvaluateStringArg(args[0], ec.RowIndex, ec.ColumnIndex);
                delimiter = TextFunction.EvaluateStringArg(args[1], ec.RowIndex, ec.ColumnIndex);
                if (args.Length >= 3 && !(args[2] is MissingArgEval))
                {
                    instanceNum = TextFunction.EvaluateIntArg(args[2], ec.RowIndex, ec.ColumnIndex);
                }
                if (args.Length >= 4 && !(args[3] is MissingArgEval))
                {
                    matchMode = TextFunction.EvaluateIntArg(args[3], ec.RowIndex, ec.ColumnIndex);
                }
                if (args.Length >= 5 && !(args[4] is MissingArgEval))
                {
                    matchEnd = TextFunction.EvaluateIntArg(args[4], ec.RowIndex, ec.ColumnIndex);
                }
                if (args.Length >= 6 && !(args[5] is MissingArgEval))
                {
                    ifNotFound = OperandResolver.GetSingleValue(args[5], ec.RowIndex, ec.ColumnIndex);
                }
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            if (instanceNum == 0 || matchMode < 0 || matchMode > 1 || matchEnd < 0 || matchEnd > 1)
            {
                return ErrorEval.VALUE_INVALID;
            }

            if (delimiter.Length == 0)
            {
                return new StringEval(mode == Mode.Before ? string.Empty : text);
            }

            StringComparison comparison = matchMode == 1
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            int matchIndex;
            int matchLen;
            if (!FindMatch(text, delimiter, instanceNum, comparison, matchEnd == 1, out matchIndex, out matchLen))
            {
                return ifNotFound ?? ErrorEval.NA;
            }

            if (mode == Mode.Before)
            {
                return new StringEval(text.Substring(0, matchIndex));
            }
            int afterStart = matchIndex + matchLen;
            return new StringEval(afterStart >= text.Length ? string.Empty : text.Substring(afterStart));
        }

        private static bool FindMatch(
            string text,
            string delimiter,
            int instanceNum,
            StringComparison comparison,
            bool matchEnd,
            out int matchIndex,
            out int matchLen)
        {
            matchIndex = -1;
            matchLen = delimiter.Length;

            if (instanceNum > 0)
            {
                int searchFrom = 0;
                int found = 0;
                while (true)
                {
                    int idx = text.IndexOf(delimiter, searchFrom, comparison);
                    if (idx < 0)
                    {
                        break;
                    }
                    found++;
                    if (found == instanceNum)
                    {
                        matchIndex = idx;
                        return true;
                    }
                    searchFrom = idx + delimiter.Length;
                }
                if (matchEnd && found == instanceNum - 1)
                {
                    matchIndex = text.Length;
                    matchLen = 0;
                    return true;
                }
                return false;
            }
            else
            {
                int searchTo = text.Length;
                int found = 0;
                int target = -instanceNum;
                if (matchEnd)
                {
                    found = 1;
                    if (target == 1)
                    {
                        matchIndex = text.Length;
                        matchLen = 0;
                        return true;
                    }
                }
                while (searchTo >= 0)
                {
                    int idx = text.LastIndexOf(delimiter, searchTo - 1, searchTo, comparison);
                    if (idx < 0)
                    {
                        break;
                    }
                    found++;
                    if (found == target)
                    {
                        matchIndex = idx;
                        return true;
                    }
                    searchTo = idx;
                }
                return false;
            }
        }
    }

    /**
     * Implementation for Excel TEXTBEFORE() function (Microsoft 365 dynamic array text family).
     * Syntax: TEXTBEFORE(text, delimiter, [instance_num], [match_mode], [match_end], [if_not_found])
     */
    public class TextBefore : FreeRefFunction
    {
        public static FreeRefFunction instance = new TextBefore();

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return TextBeforeAfter.Evaluate(TextBeforeAfter.Mode.Before, args, ec);
        }
    }

    /**
     * Implementation for Excel TEXTAFTER() function.
     * Syntax: TEXTAFTER(text, delimiter, [instance_num], [match_mode], [match_end], [if_not_found])
     */
    public class TextAfter : FreeRefFunction
    {
        public static FreeRefFunction instance = new TextAfter();

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return TextBeforeAfter.Evaluate(TextBeforeAfter.Mode.After, args, ec);
        }
    }
}
