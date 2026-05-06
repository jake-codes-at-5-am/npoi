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
    /// Tests for the _xlfn.SINGLE implicit-intersection passthrough.
    /// </summary>
    [TestFixture]
    public class TestSingle
    {
        [Test]
        public void TestPassthroughForScalars()
        {
            ValueEval result = new XlfnSingle().Evaluate(new ValueEval[] { new NumberEval(42) }, -1, -1);
            Assert.AreEqual(typeof(NumberEval), result.GetType());
            Assert.AreEqual(42, ((NumberEval)result).NumberValue);

            result = new XlfnSingle().Evaluate(new ValueEval[] { new StringEval("hello") }, -1, -1);
            Assert.AreEqual(typeof(StringEval), result.GetType());
            Assert.AreEqual("hello", ((StringEval)result).StringValue);
        }

        [Test]
        public void TestErrorPropagation()
        {
            ValueEval result = new XlfnSingle().Evaluate(new ValueEval[] { ErrorEval.NA }, -1, -1);
            Assert.AreEqual(ErrorEval.NA, result);
        }

        [Test]
        public void TestArgCountValidation()
        {
            ValueEval result = new XlfnSingle().Evaluate(new ValueEval[0], -1, -1);
            Assert.AreEqual(ErrorEval.VALUE_INVALID, result);
        }
    }
}
