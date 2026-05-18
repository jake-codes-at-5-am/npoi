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
    using System;
    using NPOI.HSSF.UserModel;
    using NPOI.SS.Formula;
    using NPOI.SS.Formula.Eval;
    using NPOI.SS.Formula.Functions;
    using NUnit.Framework;

    /// <summary>
    /// Tests for the EFFECT, NOMINAL, CUMIPMT and CUMPRINC ATP functions.
    /// </summary>
    [TestFixture]
    public class TestFinancialAtpFunctions
    {
        private const double Tolerance = 1e-6;

        private OperationEvaluationContext _ctx;

        [OneTimeSetUp]
        public void SetUp()
        {
            HSSFWorkbook wb = new HSSFWorkbook();
            wb.CreateSheet();
            HSSFEvaluationWorkbook workbook = HSSFEvaluationWorkbook.Create(wb);
            WorkbookEvaluator evaluator = new WorkbookEvaluator(workbook, new AlwaysFinalStability(), null);
            _ctx = new OperationEvaluationContext(evaluator, workbook, 0, 0, 0, null);
        }

        private class AlwaysFinalStability : IStabilityClassifier
        {
            public override bool IsCellFinal(int sheetIndex, int rowIndex, int columnIndex) => true;
        }

        [Test]
        public void TestEffectKnownValues()
        {
            // EFFECT(0.0525, 4) ~ 0.05354266
            ValueEval result = new Effect().Evaluate(
                new ValueEval[] { new NumberEval(0.0525), new NumberEval(4) }, -1, -1);
            Assert.AreEqual(0.05354266, AsDouble(result), Tolerance);
        }

        [Test]
        public void TestEffectErrors()
        {
            ValueEval result = new Effect().Evaluate(
                new ValueEval[] { new NumberEval(-0.05), new NumberEval(4) }, -1, -1);
            Assert.AreEqual(ErrorEval.NUM_ERROR, result);

            result = new Effect().Evaluate(
                new ValueEval[] { new NumberEval(0.05), new NumberEval(0) }, -1, -1);
            Assert.AreEqual(ErrorEval.NUM_ERROR, result);
        }

        [Test]
        public void TestNominalRoundTripsWithEffect()
        {
            double effectRate = 0.05354266;
            int periods = 4;
            ValueEval result = new Nominal().Evaluate(
                new ValueEval[] { new NumberEval(effectRate), new NumberEval(periods) }, -1, -1);
            Assert.AreEqual(0.0525, AsDouble(result), Tolerance);
        }

        [Test]
        public void TestNominalErrors()
        {
            ValueEval result = new Nominal().Evaluate(
                new ValueEval[] { new NumberEval(-0.01), new NumberEval(4) }, -1, -1);
            Assert.AreEqual(ErrorEval.NUM_ERROR, result);
        }

        [Test]
        public void TestCumIPmtFullScheduleEqualsTotalInterest()
        {
            // For a fully amortising loan, CUMIPMT over [1, nper] equals
            // total payments minus principal, i.e. nper * PMT + pv (sign-aware).
            double rate = 0.05 / 12;
            int nper = 12;
            double pv = 1000;
            ValueEval result = CumIPmt.instance.Evaluate(BuildArgs(rate, nper, pv, 1, nper, 0), _ctx);
            double total = AsDouble(result);
            double pmt = Finance.PMT(rate, nper, pv);
            double expected = nper * pmt + pv;
            Assert.AreEqual(expected, total, 1e-4);
        }

        [Test]
        public void TestCumPrincFullScheduleRecoversPrincipal()
        {
            // Sum of principal payments across all periods should equal -pv (loan repaid).
            double rate = 0.05 / 12;
            int nper = 12;
            double pv = 1000;
            ValueEval result = CumPrinc.instance.Evaluate(BuildArgs(rate, nper, pv, 1, nper, 0), _ctx);
            Assert.AreEqual(-pv, AsDouble(result), 1e-4);
        }

        [Test]
        public void TestCumIPmtBoundaryErrors()
        {
            // start_period > end_period
            ValueEval result = CumIPmt.instance.Evaluate(BuildArgs(0.05, 12, 1000, 6, 5, 0), _ctx);
            Assert.AreEqual(ErrorEval.NUM_ERROR, result);
            // bad type
            result = CumIPmt.instance.Evaluate(BuildArgs(0.05, 12, 1000, 1, 12, 2), _ctx);
            Assert.AreEqual(ErrorEval.NUM_ERROR, result);
            // negative rate
            result = CumIPmt.instance.Evaluate(BuildArgs(-0.05, 12, 1000, 1, 12, 0), _ctx);
            Assert.AreEqual(ErrorEval.NUM_ERROR, result);
        }

        private static ValueEval[] BuildArgs(double rate, int nper, double pv, int start, int end, int type)
        {
            return new ValueEval[]
            {
                new NumberEval(rate),
                new NumberEval(nper),
                new NumberEval(pv),
                new NumberEval(start),
                new NumberEval(end),
                new NumberEval(type)
            };
        }

        private static double AsDouble(ValueEval result)
        {
            Assert.AreEqual(typeof(NumberEval), result.GetType(), "expected NumberEval, got " + result.GetType().Name);
            return ((NumberEval)result).NumberValue;
        }
    }
}
