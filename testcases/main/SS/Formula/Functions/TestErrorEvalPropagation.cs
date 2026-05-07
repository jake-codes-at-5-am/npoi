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
    using NPOI.SS.Formula.Eval;
    using NPOI.SS.Formula.Functions;
    using NUnit.Framework;

    /// <summary>
    /// Regression tests for the "Invalid arg type" / "Bad range arg type" family
    /// of bugs where formula functions threw RuntimeException / ArgumentException
    /// when an ErrorEval (e.g. #REF!, #N/A, #DIV/0! propagated from another
    /// formula) reached them. Excel propagates such errors; the engine must too,
    /// otherwise <c>EvaluateAll()</c> aborts on the first error in the workbook.
    /// </summary>
    [TestFixture]
    public class TestErrorEvalPropagation
    {
        // ─── COUNTBLANK ───────────────────────────────────────────────────

        [Test]
        public void TestCountblankPropagatesErrorEval()
        {
            ValueEval result = new Countblank().Evaluate(
                new ValueEval[] { ErrorEval.REF_INVALID }, -1, -1);
            Assert.AreEqual(ErrorEval.REF_INVALID, result);
        }

        [Test]
        public void TestCountblankReturnsValueErrorOnUnsupportedArg()
        {
            ValueEval result = new Countblank().Evaluate(
                new ValueEval[] { new NumberEval(42) }, -1, -1);
            Assert.AreEqual(ErrorEval.VALUE_INVALID, result);
        }

        // ─── COUNTIF ──────────────────────────────────────────────────────

        [Test]
        public void TestCountifPropagatesErrorEvalOnRangeArg()
        {
            ValueEval result = new Countif().Evaluate(
                new ValueEval[] { ErrorEval.NA, new NumberEval(0) }, -1, -1);
            Assert.AreEqual(ErrorEval.NA, result);
        }

        [Test]
        public void TestCountifReturnsValueErrorOnUnsupportedRange()
        {
            // Numeric scalar isn't a valid range; pre-fix this would throw
            // ArgumentException("Bad range arg type ...").
            ValueEval result = new Countif().Evaluate(
                new ValueEval[] { new NumberEval(1), new NumberEval(0) }, -1, -1);
            Assert.AreEqual(ErrorEval.VALUE_INVALID, result);
        }

        // ─── SUMPRODUCT (cross-checked here too) ──────────────────────────

        [Test]
        public void TestSumproductPropagatesErrorEvalArg()
        {
            ValueEval result = new Sumproduct().Evaluate(
                new ValueEval[] { ErrorEval.DIV_ZERO, new NumberEval(2) }, -1, -1);
            Assert.AreEqual(ErrorEval.DIV_ZERO, result);
        }

        [Test]
        public void TestSumproductDoesNotThrowRuntimeExceptionOnUnsupportedArg()
        {
            // StringEval as the first arg used to fall through every type check
            // and trigger the RuntimeException("Invalid arg type for SUMPRODUCT").
            // After the fix it returns #VALUE! gracefully.
            ValueEval result = new Sumproduct().Evaluate(
                new ValueEval[] { new StringEval("not a number") }, -1, -1);
            Assert.AreEqual(ErrorEval.VALUE_INVALID, result);
        }
    }
}
