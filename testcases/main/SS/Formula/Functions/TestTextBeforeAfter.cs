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
    /// Tests for the modern Excel TEXTBEFORE / TEXTAFTER functions.
    /// </summary>
    [TestFixture]
    public class TestTextBeforeAfter
    {
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

        private string ExpectString(FreeRefFunction func, params ValueEval[] args)
        {
            ValueEval result = func.Evaluate(args, _ctx);
            Assert.AreEqual(typeof(StringEval), result.GetType(), "expected StringEval, got " + result.GetType().Name);
            return ((StringEval)result).StringValue;
        }

        private ValueEval Invoke(FreeRefFunction func, params ValueEval[] args)
        {
            return func.Evaluate(args, _ctx);
        }

        [Test]
        public void TestTextBeforeBasic()
        {
            Assert.AreEqual("Hello", ExpectString(TextBefore.instance,
                new StringEval("Hello World"), new StringEval(" ")));
            Assert.AreEqual("a-b", ExpectString(TextBefore.instance,
                new StringEval("a-b-c-d"), new StringEval("-"), new NumberEval(2)));
            Assert.AreEqual("a-b", ExpectString(TextBefore.instance,
                new StringEval("a-b-c-d"), new StringEval("-"), new NumberEval(-2)));
        }

        [Test]
        public void TestTextAfterBasic()
        {
            Assert.AreEqual("World", ExpectString(TextAfter.instance,
                new StringEval("Hello World"), new StringEval(" ")));
            Assert.AreEqual("c-d", ExpectString(TextAfter.instance,
                new StringEval("a-b-c-d"), new StringEval("-"), new NumberEval(2)));
            Assert.AreEqual("d", ExpectString(TextAfter.instance,
                new StringEval("a-b-c-d"), new StringEval("-"), new NumberEval(-1)));
        }

        [Test]
        public void TestCaseSensitivity()
        {
            // default (match_mode=0) is case sensitive — "WORLD" not found
            Assert.AreEqual(ErrorEval.NA, Invoke(TextBefore.instance,
                new StringEval("Hello World"), new StringEval("WORLD")));
            // match_mode=1 is case insensitive
            Assert.AreEqual("Hello ", ExpectString(TextBefore.instance,
                new StringEval("Hello World"), new StringEval("WORLD"),
                MissingArgEval.instance, new NumberEval(1)));
        }

        [Test]
        public void TestIfNotFound()
        {
            Assert.AreEqual("MISSING", ExpectString(TextBefore.instance,
                new StringEval("abc"), new StringEval("z"),
                MissingArgEval.instance, MissingArgEval.instance, MissingArgEval.instance,
                new StringEval("MISSING")));
        }

        [Test]
        public void TestEmptyDelimiter()
        {
            Assert.AreEqual(string.Empty, ExpectString(TextBefore.instance,
                new StringEval("abc"), new StringEval(string.Empty)));
            Assert.AreEqual("abc", ExpectString(TextAfter.instance,
                new StringEval("abc"), new StringEval(string.Empty)));
        }

        [Test]
        public void TestZeroInstanceFails()
        {
            Assert.AreEqual(ErrorEval.VALUE_INVALID, Invoke(TextBefore.instance,
                new StringEval("abc"), new StringEval("b"), new NumberEval(0)));
        }

        [Test]
        public void TestNotFoundReturnsNA()
        {
            Assert.AreEqual(ErrorEval.NA, Invoke(TextAfter.instance,
                new StringEval("abc"), new StringEval("z")));
        }

        [Test]
        public void TestMatchEndTreatsEosAsDelimiter()
        {
            // With match_end=1, the end-of-string acts as an additional delimiter.
            // TEXTAFTER("a-b", "-", 2, 0, 1) -> empty string (after the synthetic end delimiter)
            Assert.AreEqual(string.Empty, ExpectString(TextAfter.instance,
                new StringEval("a-b"), new StringEval("-"),
                new NumberEval(2), new NumberEval(0), new NumberEval(1)));
            // TEXTBEFORE("a-b", "-", 2, 0, 1) -> "a-b" because the second delimiter is end-of-string
            Assert.AreEqual("a-b", ExpectString(TextBefore.instance,
                new StringEval("a-b"), new StringEval("-"),
                new NumberEval(2), new NumberEval(0), new NumberEval(1)));
        }
    }
}
