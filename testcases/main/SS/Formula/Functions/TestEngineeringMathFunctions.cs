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
    /// Tests for SQRTPI, SERIESSUM, MULTINOMIAL, ERF, ERFC and GESTEP.
    /// </summary>
    [TestFixture]
    public class TestEngineeringMathFunctions
    {
        private const double Tolerance = 1e-7;

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

        private static ValueEval InvokeFunction(Function func, params ValueEval[] args)
        {
            return func.Evaluate(args, -1, -1);
        }

        private ValueEval InvokeFreeRef(FreeRefFunction func, params ValueEval[] args)
        {
            return func.Evaluate(args, _ctx);
        }

        private static double AsDouble(ValueEval result)
        {
            Assert.AreEqual(typeof(NumberEval), result.GetType(), "expected NumberEval, got " + result.GetType().Name);
            return ((NumberEval)result).NumberValue;
        }

        [Test]
        public void TestSqrtPi()
        {
            Assert.AreEqual(Math.Sqrt(Math.PI), AsDouble(InvokeFunction(new SqrtPi(), new NumberEval(1))), Tolerance);
            Assert.AreEqual(Math.Sqrt(2 * Math.PI), AsDouble(InvokeFunction(new SqrtPi(), new NumberEval(2))), Tolerance);
            Assert.AreEqual(0, AsDouble(InvokeFunction(new SqrtPi(), new NumberEval(0))), Tolerance);
            Assert.AreEqual(ErrorEval.NUM_ERROR, InvokeFunction(new SqrtPi(), new NumberEval(-1)));
            Assert.AreEqual(ErrorEval.VALUE_INVALID, InvokeFunction(new SqrtPi()));
        }

        [Test]
        public void TestSeriesSumSimple()
        {
            // SERIESSUM(2, 0, 1, {1,1,1}) = 1*2^0 + 1*2^1 + 1*2^2 = 1+2+4 = 7
            ValueEval result = InvokeFunction(new SeriesSum(),
                new NumberEval(2), new NumberEval(0), new NumberEval(1),
                BuildArea(new double[] { 1, 1, 1 }));
            Assert.AreEqual(7, AsDouble(result), Tolerance);
        }

        [Test]
        public void TestSeriesSumExpApprox()
        {
            // Truncated power series for e^x: 1 + x + x^2/2 + x^3/6 + ... ~= e^1
            ValueEval coefficients = BuildArea(new double[]
            {
                1, 1, 1.0 / 2, 1.0 / 6, 1.0 / 24, 1.0 / 120, 1.0 / 720
            });
            ValueEval result = InvokeFunction(new SeriesSum(),
                new NumberEval(1), new NumberEval(0), new NumberEval(1), coefficients);
            Assert.AreEqual(Math.E, AsDouble(result), 1e-3);
        }

        [Test]
        public void TestMultinomial()
        {
            // MULTINOMIAL(2,3,4) = 9! / (2! 3! 4!) = 362880 / (2*6*24) = 1260
            Assert.AreEqual(1260, AsDouble(InvokeFreeRef(Multinomial.instance,
                new NumberEval(2), new NumberEval(3), new NumberEval(4))), Tolerance);
            // MULTINOMIAL(0) = 1
            Assert.AreEqual(1, AsDouble(InvokeFreeRef(Multinomial.instance, new NumberEval(0))), Tolerance);
            // negative -> #NUM!
            Assert.AreEqual(ErrorEval.NUM_ERROR,
                InvokeFreeRef(Multinomial.instance, new NumberEval(-1)));
        }

        [Test]
        public void TestErfKnownValues()
        {
            // erf(0) = 0
            Assert.AreEqual(0, AsDouble(InvokeFunction(new Erf(), new NumberEval(0))), Tolerance);
            // erf(0.5) ~ 0.5204998778
            Assert.AreEqual(0.5204998778, AsDouble(InvokeFunction(new Erf(), new NumberEval(0.5))), 1e-6);
            // erf(1) ~ 0.8427007929
            Assert.AreEqual(0.8427007929, AsDouble(InvokeFunction(new Erf(), new NumberEval(1))), 1e-6);
            // erf is odd: erf(-x) = -erf(x)
            Assert.AreEqual(-0.5204998778, AsDouble(InvokeFunction(new Erf(), new NumberEval(-0.5))), 1e-6);
        }

        [Test]
        public void TestErfBetweenLimits()
        {
            // ERF(0, 1) = ERF(1) - ERF(0) = ERF(1)
            ValueEval result = InvokeFunction(new Erf(), new NumberEval(0), new NumberEval(1));
            Assert.AreEqual(0.8427007929, AsDouble(result), 1e-6);
        }

        [Test]
        public void TestErfc()
        {
            Assert.AreEqual(1.0, AsDouble(InvokeFunction(new ErfC(), new NumberEval(0))), Tolerance);
            // erfc(1) = 1 - erf(1) ~ 0.1572992071
            Assert.AreEqual(0.1572992071, AsDouble(InvokeFunction(new ErfC(), new NumberEval(1))), 1e-6);
        }

        [Test]
        public void TestGeStep()
        {
            Assert.AreEqual(1, AsDouble(InvokeFunction(new GeStep(), new NumberEval(5), new NumberEval(4))));
            Assert.AreEqual(1, AsDouble(InvokeFunction(new GeStep(), new NumberEval(5), new NumberEval(5))));
            Assert.AreEqual(0, AsDouble(InvokeFunction(new GeStep(), new NumberEval(4), new NumberEval(5))));
            // step defaults to 0
            Assert.AreEqual(1, AsDouble(InvokeFunction(new GeStep(), new NumberEval(1))));
            Assert.AreEqual(0, AsDouble(InvokeFunction(new GeStep(), new NumberEval(-1))));
        }

        private static ValueEval BuildArea(double[] values)
        {
            ValueEval[] cells = new ValueEval[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                cells[i] = new NumberEval(values[i]);
            }
            string range = "A1:" + GetCellRef(values.Length) + "1";
            return EvalFactory.CreateAreaEval(range, cells);
        }

        private static string GetCellRef(int columnCount)
        {
            // The SERIESSUM tests stay within the first 26 columns, so single-letter refs are sufficient.
            return ((char)('A' + (columnCount - 1))).ToString();
        }
    }
}
