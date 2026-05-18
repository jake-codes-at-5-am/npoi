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
    using NPOI.SS.Formula;
    using NPOI.SS.Formula.Eval;
    using NPOI.SS.UserModel;

    /**
     * Implementation for Excel ISFORMULA() function (Excel 2013+, also written
     * as <c>_xlfn.ISFORMULA</c> in workbooks saved for older clients).
     *
     * Syntax: ISFORMULA(reference)
     *
     * Returns TRUE when <c>reference</c> points at a cell that holds a formula,
     * FALSE for any other reference, and #VALUE! when the argument is not a
     * reference at all (matching Excel's documented behaviour).
     */
    public class IsFormula : FreeRefFunction
    {
        public static FreeRefFunction instance = new IsFormula();

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            if (args == null || args.Length != 1)
            {
                return ErrorEval.VALUE_INVALID;
            }

            ValueEval arg = args[0];
            int rowIndex;
            int columnIndex;
            int sheetIndex;
            if (arg is RefEval refEval)
            {
                rowIndex = refEval.Row;
                columnIndex = refEval.Column;
                sheetIndex = refEval.FirstSheetIndex;
            }
            else if (arg is AreaEval areaEval)
            {
                // Excel resolves the implicit-intersection cell when an area is supplied.
                rowIndex = areaEval.FirstRow;
                columnIndex = areaEval.FirstColumn;
                sheetIndex = areaEval.FirstSheetIndex;
            }
            else
            {
                return ErrorEval.VALUE_INVALID;
            }

            IEvaluationWorkbook workbook = ec.GetWorkbook();
            if (workbook == null)
            {
                return ErrorEval.VALUE_INVALID;
            }
            IEvaluationSheet sheet = workbook.GetSheet(sheetIndex);
            if (sheet == null)
            {
                return BoolEval.FALSE;
            }
            IEvaluationCell cell = sheet.GetCell(rowIndex, columnIndex);
            if (cell == null)
            {
                return BoolEval.FALSE;
            }
            return cell.CellType == CellType.Formula ? BoolEval.TRUE : BoolEval.FALSE;
        }
    }
}
