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

namespace TestCases.SS.Formula.Atp
{
    using NPOI.SS.Formula.Atp;
    using NPOI.SS.Formula.Functions;
    using NUnit.Framework;

    /// <summary>
    /// Verifies that the new XL-258 functions are registered (and resolvable
    /// via the <c>_xlfn.</c> prefix) so that workbooks emitted by Excel 365 no
    /// longer raise NotImplementedFunctionException.
    /// </summary>
    [TestFixture]
    public class TestAnalysisToolPakRegistration
    {
        [TestCase("BIN2HEX")]
        [TestCase("BIN2OCT")]
        [TestCase("HEX2BIN")]
        [TestCase("HEX2OCT")]
        [TestCase("OCT2BIN")]
        [TestCase("OCT2HEX")]
        [TestCase("GCD")]
        [TestCase("LCM")]
        [TestCase("SQRTPI")]
        [TestCase("SERIESSUM")]
        [TestCase("MULTINOMIAL")]
        [TestCase("ERF")]
        [TestCase("ERFC")]
        [TestCase("GESTEP")]
        [TestCase("IMABS")]
        [TestCase("IMSIN")]
        [TestCase("IMCOS")]
        [TestCase("EFFECT")]
        [TestCase("NOMINAL")]
        [TestCase("CUMIPMT")]
        [TestCase("CUMPRINC")]
        [TestCase("TEXTBEFORE")]
        [TestCase("TEXTAFTER")]
        [TestCase("SINGLE")]
        // second-wave additions
        [TestCase("ISFORMULA")]
        [TestCase("BITAND")]
        [TestCase("BITOR")]
        [TestCase("BITXOR")]
        [TestCase("BITLSHIFT")]
        [TestCase("BITRSHIFT")]
        [TestCase("BASE")]
        [TestCase("DECIMAL")]
        [TestCase("DEC2OCT")]
        [TestCase("ARABIC")]
        [TestCase("DAYS")]
        [TestCase("ISOWEEKNUM")]
        [TestCase("XOR")]
        [TestCase("UNICODE")]
        [TestCase("UNICHAR")]
        [TestCase("GAMMA")]
        [TestCase("GAMMALN.PRECISE")]
        [TestCase("COMBINA")]
        [TestCase("PERMUTATIONA")]
        // full IM* family
        [TestCase("IMSUM")]
        [TestCase("IMSUB")]
        [TestCase("IMPRODUCT")]
        [TestCase("IMDIV")]
        [TestCase("IMCONJUGATE")]
        [TestCase("IMARGUMENT")]
        [TestCase("IMSQRT")]
        [TestCase("IMPOWER")]
        [TestCase("IMEXP")]
        [TestCase("IMLN")]
        [TestCase("IMLOG10")]
        [TestCase("IMLOG2")]
        [TestCase("IMTAN")]
        [TestCase("IMSEC")]
        [TestCase("IMCSC")]
        [TestCase("IMCOT")]
        [TestCase("IMSINH")]
        [TestCase("IMCOSH")]
        [TestCase("IMTANH")]
        [TestCase("IMSECH")]
        [TestCase("IMCSCH")]
        public void TestFunctionResolves(string name)
        {
            FreeRefFunction func = AnalysisToolPak.instance.FindFunction(name);
            Assert.IsNotNull(func, name + " should be registered");
            Assert.IsFalse(func is NotImplemented, name + " should not be a NotImplemented stub");
        }

        [TestCase("_xlfn.SINGLE")]
        [TestCase("_xlfn.GCD")]
        [TestCase("_xlfn.TEXTBEFORE")]
        [TestCase("_xlfn.IMABS")]
        [TestCase("_xlfn.ISFORMULA")]
        [TestCase("_xlfn.BITAND")]
        [TestCase("_xlfn.UNICODE")]
        [TestCase("_xlfn.GAMMA")]
        public void TestXlfnPrefixedNamesResolve(string name)
        {
            FreeRefFunction func = AnalysisToolPak.instance.FindFunction(name);
            Assert.IsNotNull(func, name + " should be resolvable through the _xlfn. prefix path");
            Assert.IsFalse(func is NotImplemented);
        }
    }
}
