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

namespace TestCases.SS.Formula.Functions
{
    using NPOI.HSSF.UserModel;
    using NPOI.SS.Formula;
    using NPOI.SS.Formula.Atp;
    using NPOI.SS.Formula.Eval;
    using NPOI.SS.Formula.Functions;
    using NPOI.SS.UserModel;
    using NUnit.Framework;

    /// <summary>
    /// Tests for <see cref="XlfnSingle"/>, the implementation of Excel's
    /// <c>_xlfn.SINGLE()</c> implicit-intersection operator. SINGLE is what
    /// Excel emits whenever an array reference appears in a scalar context
    /// in a workbook saved for older clients.
    ///
    /// Coverage:
    ///   * registry resolution under both bare and "_xlfn." prefixed names
    ///   * scalar passthrough (NumberEval, StringEval, BoolEval, BlankEval)
    ///   * error propagation
    ///   * RefEval dereferencing
    ///   * AreaEval implicit-intersection rules:
    ///       - 1×N row picks the cell at the calling column
    ///       - N×1 column picks the cell at the calling row
    ///       - calling cell outside the area's row/column → #VALUE!
    ///   * argument-count validation (0 and 2+ args)
    ///   * end-to-end evaluation of <c>_xlfn.SINGLE(...)</c> against an
    ///     HSSFWorkbook so we exercise the full parser → evaluator path that
    ///     was failing before XL-258.
    /// </summary>
    [TestFixture]
    public class TestSingle
    {
        // ---------- registry resolution ----------

        [Test]
        public void TestResolvesUnderBareName()
        {
            FreeRefFunction func = AnalysisToolPak.instance.FindFunction("SINGLE");
            Assert.IsNotNull(func, "SINGLE should be registered");
            Assert.IsInstanceOf<XlfnSingle>(func);
        }

        [Test]
        public void TestResolvesUnderXlfnPrefix()
        {
            // The "_xlfn." prefix is what Microsoft 365 actually writes into
            // the file. AnalysisToolPak.FindFunction must strip it before
            // dictionary lookup. This is the path that was raising
            // NotImplementedFunctionException before XL-258.
            FreeRefFunction func = AnalysisToolPak.instance.FindFunction("_xlfn.SINGLE");
            Assert.IsNotNull(func, "_xlfn.SINGLE should resolve through prefix-stripping");
            Assert.IsInstanceOf<XlfnSingle>(func);
        }

        // ---------- scalar passthrough ----------

        [Test]
        public void TestNumberPassthrough()
        {
            AssertNumberResult(42, Invoke(new NumberEval(42)));
            AssertNumberResult(-3.14, Invoke(new NumberEval(-3.14)));
            AssertNumberResult(0, Invoke(new NumberEval(0)));
        }

        [Test]
        public void TestStringPassthrough()
        {
            AssertStringResult("hello", Invoke(new StringEval("hello")));
            AssertStringResult(string.Empty, Invoke(new StringEval(string.Empty)));
            AssertStringResult("with spaces", Invoke(new StringEval("with spaces")));
        }

        [Test]
        public void TestBoolPassthrough()
        {
            ValueEval r1 = Invoke(BoolEval.TRUE);
            Assert.AreEqual(BoolEval.TRUE, r1);

            ValueEval r2 = Invoke(BoolEval.FALSE);
            Assert.AreEqual(BoolEval.FALSE, r2);
        }

        [Test]
        public void TestBlankPassthrough()
        {
            ValueEval result = Invoke(BlankEval.instance);
            Assert.AreSame(BlankEval.instance, result,
                "BlankEval should be returned unchanged so downstream IFERROR/empty-string checks behave normally");
        }

        // ---------- error propagation ----------

        [Test]
        public void TestErrorArgPropagates()
        {
            // Every Excel error type must round-trip rather than getting
            // swallowed — IFERROR/IFNA wrappers around _xlfn.SINGLE rely on this.
            Assert.AreEqual(ErrorEval.NA, Invoke(ErrorEval.NA));
            Assert.AreEqual(ErrorEval.NUM_ERROR, Invoke(ErrorEval.NUM_ERROR));
            Assert.AreEqual(ErrorEval.VALUE_INVALID, Invoke(ErrorEval.VALUE_INVALID));
            Assert.AreEqual(ErrorEval.DIV_ZERO, Invoke(ErrorEval.DIV_ZERO));
            Assert.AreEqual(ErrorEval.REF_INVALID, Invoke(ErrorEval.REF_INVALID));
            Assert.AreEqual(ErrorEval.NAME_INVALID, Invoke(ErrorEval.NAME_INVALID));
            Assert.AreEqual(ErrorEval.NULL_INTERSECTION, Invoke(ErrorEval.NULL_INTERSECTION));
        }

        // ---------- RefEval dereferencing ----------

        [Test]
        public void TestRefEvalIsDereferenced()
        {
            RefEval cellA1 = EvalFactory.CreateRefEval("A1", new NumberEval(99));
            ValueEval result = Invoke(cellA1);
            AssertNumberResult(99, result);
        }

        [Test]
        public void TestRefEvalToErrorPropagates()
        {
            RefEval errCell = EvalFactory.CreateRefEval("A1", ErrorEval.DIV_ZERO);
            Assert.AreEqual(ErrorEval.DIV_ZERO, Invoke(errCell));
        }

        // ---------- AreaEval — Excel implicit-intersection rules ----------

        [Test]
        public void TestSingleRowAreaPicksCellAtCallingColumn()
        {
            // A1:E1 holds {10, 20, 30, 40, 50}. SINGLE called from C5 must
            // pick the C-column entry (index 2 → 30).
            AreaEval row = EvalFactory.CreateAreaEval("A1:E1", new ValueEval[]
            {
                new NumberEval(10), new NumberEval(20), new NumberEval(30),
                new NumberEval(40), new NumberEval(50)
            });
            ValueEval result = new XlfnSingle().Evaluate(new ValueEval[] { row }, srcRowIndex: 4, srcColumnIndex: 2);
            AssertNumberResult(30, result);
        }

        [Test]
        public void TestSingleColumnAreaPicksCellAtCallingRow()
        {
            // A1:A4 holds {11, 22, 33, 44}. SINGLE called from B3 picks row 3 → 33.
            AreaEval col = EvalFactory.CreateAreaEval("A1:A4", new ValueEval[]
            {
                new NumberEval(11), new NumberEval(22), new NumberEval(33), new NumberEval(44)
            });
            ValueEval result = new XlfnSingle().Evaluate(new ValueEval[] { col }, srcRowIndex: 2, srcColumnIndex: 1);
            AssertNumberResult(33, result);
        }

        [Test]
        public void TestAreaWhenCallingCellMissesRange()
        {
            // A1:C1 (single row). Calling from row 0/col 5 — col 5 is outside
            // the A:C range, so implicit intersection cannot resolve → #VALUE!.
            AreaEval row = EvalFactory.CreateAreaEval("A1:C1", new ValueEval[]
            {
                new NumberEval(1), new NumberEval(2), new NumberEval(3)
            });
            ValueEval result = new XlfnSingle().Evaluate(new ValueEval[] { row }, srcRowIndex: 0, srcColumnIndex: 5);
            Assert.AreEqual(ErrorEval.VALUE_INVALID, result);
        }

        [Test]
        public void TestMultiRowMultiColumnAreaIsRejected()
        {
            // A 2×2 area cannot be coerced via implicit intersection from
            // a calling cell that doesn't share its row or column → #VALUE!.
            AreaEval area = EvalFactory.CreateAreaEval("A1:B2", new ValueEval[]
            {
                new NumberEval(1), new NumberEval(2),
                new NumberEval(3), new NumberEval(4)
            });
            ValueEval result = new XlfnSingle().Evaluate(new ValueEval[] { area }, srcRowIndex: 10, srcColumnIndex: 10);
            Assert.AreEqual(ErrorEval.VALUE_INVALID, result);
        }

        // ---------- argument count ----------

        [Test]
        public void TestZeroArgsReturnsValueError()
        {
            Assert.AreEqual(ErrorEval.VALUE_INVALID,
                new XlfnSingle().Evaluate(new ValueEval[0], -1, -1));
        }

        [Test]
        public void TestTwoArgsReturnsValueError()
        {
            Assert.AreEqual(ErrorEval.VALUE_INVALID,
                new XlfnSingle().Evaluate(
                    new ValueEval[] { new NumberEval(1), new NumberEval(2) }, -1, -1));
        }

        // ---------- nested invocation ----------

        [Test]
        public void TestNestedSingleIsIdempotent()
        {
            // _xlfn.SINGLE(_xlfn.SINGLE(x)) must equal _xlfn.SINGLE(x). After
            // the first call collapses an arg to a scalar, the second call
            // is just identity.
            ValueEval inner = Invoke(new NumberEval(7));
            ValueEval outer = Invoke(inner);
            AssertNumberResult(7, outer);
        }

        // ---------- end-to-end against HSSFWorkbook ----------
        // These exercise the full parser → operation evaluator → registry path
        // — i.e. the exact path that raised NotImplementedFunctionException
        // before XL-258.

        [Test]
        public void TestEndToEndScalar()
        {
            HSSFWorkbook wb = new HSSFWorkbook();
            ISheet sheet = wb.CreateSheet("S");
            sheet.CreateRow(0).CreateCell(0).SetCellValue(46279);

            ICell formulaCell = sheet.CreateRow(20).CreateCell(0);
            formulaCell.SetCellFormula("_xlfn.SINGLE(A1)");

            CellValue result = wb.GetCreationHelper().CreateFormulaEvaluator().Evaluate(formulaCell);
            Assert.AreEqual(CellType.Numeric, result.CellType);
            Assert.AreEqual(46279, result.NumberValue);
        }

        [Test]
        public void TestEndToEndArrayDeref()
        {
            // Mirrors the customer's FORECAST!H2 idiom: an array reference
            // (INDEX over an entire row) coerced to a scalar by _xlfn.SINGLE.
            HSSFWorkbook wb = new HSSFWorkbook();
            ISheet sheet = wb.CreateSheet("S");
            IRow row0 = sheet.CreateRow(0);
            for (int c = 0; c < 5; c++) row0.CreateCell(c).SetCellValue(100 + c);

            ICell formulaCell = sheet.CreateRow(5).CreateCell(0);
            formulaCell.SetCellFormula("_xlfn.SINGLE(INDEX(1:1, 1, 3))");

            CellValue result = wb.GetCreationHelper().CreateFormulaEvaluator().Evaluate(formulaCell);
            Assert.AreEqual(CellType.Numeric, result.CellType);
            Assert.AreEqual(102, result.NumberValue);
        }

        [Test]
        public void TestEndToEndWrappedInIfError()
        {
            // The customer's FORECAST!H2 wraps the array deref in IFERROR(...,"").
            // Confirm the error path falls through to the empty string.
            HSSFWorkbook wb = new HSSFWorkbook();
            ISheet sheet = wb.CreateSheet("S");

            ICell formulaCell = sheet.CreateRow(5).CreateCell(0);
            formulaCell.SetCellFormula("IFERROR(_xlfn.SINGLE(1/0), \"\")");

            CellValue result = wb.GetCreationHelper().CreateFormulaEvaluator().Evaluate(formulaCell);
            Assert.AreEqual(CellType.String, result.CellType);
            Assert.AreEqual(string.Empty, result.StringValue);
        }

        [Test]
        public void TestEndToEndDoesNotThrowNotImplemented()
        {
            // Regression guard: prior to XL-258, this throws
            // NotImplementedFunctionException("_xlfn.SINGLE"). Now it must
            // return cleanly.
            HSSFWorkbook wb = new HSSFWorkbook();
            ISheet sheet = wb.CreateSheet("S");
            sheet.CreateRow(0).CreateCell(0).SetCellValue(123);

            ICell formulaCell = sheet.CreateRow(10).CreateCell(0);
            formulaCell.SetCellFormula("_xlfn.SINGLE(A1)");

            Assert.DoesNotThrow(() =>
                wb.GetCreationHelper().CreateFormulaEvaluator().Evaluate(formulaCell));
        }

        [Test]
        public void TestEndToEndNamedFunctionInExistingWorkbookEvaluator()
        {
            // Drive the FreeRefFunction-shaped Evaluate path via an
            // OperationEvaluationContext so we cover the codepath used during
            // a real workbook EvaluateAll(), not just the cell-direct API.
            using HSSFWorkbook wb = new HSSFWorkbook();
            wb.CreateSheet();
            HSSFEvaluationWorkbook workbook = HSSFEvaluationWorkbook.Create(wb);
            WorkbookEvaluator evaluator = new WorkbookEvaluator(workbook, new AlwaysFinal(), null);
            OperationEvaluationContext ec = new OperationEvaluationContext(evaluator, workbook, 0, 0, 0, null);

            ValueEval result = XlfnSingle.instance.Evaluate(new ValueEval[] { new NumberEval(7) }, ec);
            AssertNumberResult(7, result);
        }

        // ---------- helpers ----------

        private sealed class AlwaysFinal : IStabilityClassifier
        {
            public override bool IsCellFinal(int sheetIndex, int rowIndex, int columnIndex) => true;
        }

        private static ValueEval Invoke(ValueEval arg) =>
            new XlfnSingle().Evaluate(new ValueEval[] { arg }, srcRowIndex: -1, srcColumnIndex: -1);

        private static void AssertNumberResult(double expected, ValueEval result)
        {
            Assert.IsInstanceOf<NumberEval>(result, "expected NumberEval, got " + result.GetType().Name);
            Assert.AreEqual(expected, ((NumberEval)result).NumberValue, 1e-9);
        }

        private static void AssertStringResult(string expected, ValueEval result)
        {
            Assert.IsInstanceOf<StringEval>(result, "expected StringEval, got " + result.GetType().Name);
            Assert.AreEqual(expected, ((StringEval)result).StringValue);
        }
    }
}
