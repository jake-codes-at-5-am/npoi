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
    using NPOI.SS.Formula.Eval;
    using NPOI.SS.Formula.Functions;
    using NUnit.Framework;

    /// <summary>
    /// Tests for the GCD and LCM ATP functions, including range arguments.
    /// </summary>
    [TestFixture]
    public class TestGcdLcm
    {
        private OperationEvaluationContext _ctx;

        [OneTimeSetUp]
        public void SetUp()
        {
            HSSFWorkbook wb = new HSSFWorkbook();
            wb.CreateSheet();
            HSSFEvaluationWorkbook workbook = HSSFEvaluationWorkbook.Create(wb);
            WorkbookEvaluator workbookEvaluator = new WorkbookEvaluator(workbook, new AlwaysFinalStability(), null);
            _ctx = new OperationEvaluationContext(workbookEvaluator, workbook, 0, 0, 0, null);
        }

        private class AlwaysFinalStability : IStabilityClassifier
        {
            public override bool IsCellFinal(int sheetIndex, int rowIndex, int columnIndex) => true;
        }

        private double InvokeAsDouble(FreeRefFunction func, params double[] values)
        {
            ValueEval[] args = new ValueEval[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                args[i] = new NumberEval(values[i]);
            }
            ValueEval result = func.Evaluate(args, _ctx);
            Assert.AreEqual(typeof(NumberEval), result.GetType(), "expected NumberEval, got " + result.GetType().Name);
            return ((NumberEval)result).NumberValue;
        }

        private ValueEval Invoke(FreeRefFunction func, params double[] values)
        {
            ValueEval[] args = new ValueEval[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                args[i] = new NumberEval(values[i]);
            }
            return func.Evaluate(args, _ctx);
        }

        [Test]
        public void TestGcdBasic()
        {
            Assert.AreEqual(6, InvokeAsDouble(Gcd.instance, 12, 18));
            Assert.AreEqual(6, InvokeAsDouble(Gcd.instance, 12, 18, 24));
            Assert.AreEqual(1, InvokeAsDouble(Gcd.instance, 7, 13));
            Assert.AreEqual(5, InvokeAsDouble(Gcd.instance, 5, 0));
            Assert.AreEqual(3, InvokeAsDouble(Gcd.instance, 3));
        }

        [Test]
        public void TestGcdTruncatesDecimals()
        {
            // 12.7 truncates to 12, 18.4 to 18 -> GCD = 6
            Assert.AreEqual(6, InvokeAsDouble(Gcd.instance, 12.7, 18.4));
        }

        [Test]
        public void TestGcdNegativeFails()
        {
            ValueEval result = Invoke(Gcd.instance, 12, -6);
            Assert.AreEqual(ErrorEval.NUM_ERROR, result);
        }

        [Test]
        public void TestLcmBasic()
        {
            Assert.AreEqual(36, InvokeAsDouble(Lcm.instance, 12, 18));
            Assert.AreEqual(72, InvokeAsDouble(Lcm.instance, 12, 18, 24));
            Assert.AreEqual(91, InvokeAsDouble(Lcm.instance, 7, 13));
            Assert.AreEqual(5, InvokeAsDouble(Lcm.instance, 5));
        }

        [Test]
        public void TestLcmZeroProducesZero()
        {
            Assert.AreEqual(0, InvokeAsDouble(Lcm.instance, 0, 5));
            Assert.AreEqual(0, InvokeAsDouble(Lcm.instance, 12, 0, 18));
        }

        [Test]
        public void TestLcmNegativeFails()
        {
            ValueEval result = Invoke(Lcm.instance, 12, -6);
            Assert.AreEqual(ErrorEval.NUM_ERROR, result);
        }

        [Test]
        public void TestNoArgsFails()
        {
            Assert.AreEqual(ErrorEval.VALUE_INVALID, Invoke(Gcd.instance));
            Assert.AreEqual(ErrorEval.VALUE_INVALID, Invoke(Lcm.instance));
        }
    }
}
