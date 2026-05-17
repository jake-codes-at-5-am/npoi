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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using NPOI.SS.Formula.Function;

namespace NPOI.SS.Formula.Atp
{
    using System;
    using System.Collections;
    using NPOI.SS.Formula;
    using NPOI.SS.Formula.Eval;
    using NPOI.SS.Formula.Functions;
    using NPOI.SS.Formula.UDF;

    public class NotImplemented : FreeRefFunction
    {
        private String _functionName;

        public NotImplemented(String functionName)
        {
            _functionName = functionName;
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            throw new NotImplementedFunctionException(_functionName);
        }
    }

    public class AnalysisToolPak : UDFFinder
    {
        public static UDFFinder instance = new AnalysisToolPak();

        private static Dictionary<String, FreeRefFunction> _functionsByName = CreateFunctionsMap();

        private AnalysisToolPak()
        {
            // no instances of this class
        }

        public override FreeRefFunction FindFunction(String name)
        {
            // functions that are available in Excel 2007+ have a prefix _xlfn.
            // if you save such a .xlsx workbook as .xls
            String prefix = "_xlfn.";
            if (name.StartsWith(prefix)) name = name.Substring(prefix.Length);

            string key = name.ToUpper();
            if (_functionsByName.ContainsKey(key))
                return _functionsByName[key];

            return null;
        }

        // Note on _xlfn.-prefixed names: FindFunction strips the "_xlfn." prefix
        // before this dictionary is consulted, so every function — whether or not
        // it has a bare-name Excel surface — must be registered under its bare
        // name only. The most surprising example is SINGLE (the implicit-
        // intersection "@" operator), which has no user-facing bare-name form.
        private static Dictionary<String, FreeRefFunction> CreateFunctionsMap()
        {
            Dictionary<String, FreeRefFunction> m = new Dictionary<String, FreeRefFunction>(120);

            r(m, "ACCRINT", null);
            r(m, "ACCRINTM", null);
            r(m, "AMORDEGRC", null);
            r(m, "AMORLINC", null);
            r(m, "ARABIC", Arabic.instance);
            r(m, "AVERAGEIF", AverageIf.instance);
            r(m, "AVERAGEIFS", AverageIfs.instance);
            r(m, "BAHTTEXT", null);
            r(m, "BASE", Base.instance);
            r(m, "BESSELI", null);
            r(m, "BESSELJ", BesselJ.instance);
            r(m, "BESSELK", null);
            r(m, "BESSELY", null);
            r(m, "BIN2DEC", Bin2Dec.instance);
            r(m, "BIN2HEX", Bin2Hex.instance);
            r(m, "BIN2OCT", Bin2Oct.instance);
            r(m, "BITAND", BitAnd.instance);
            r(m, "BITLSHIFT", BitLShift.instance);
            r(m, "BITOR", BitOr.instance);
            r(m, "BITRSHIFT", BitRShift.instance);
            r(m, "BITXOR", BitXor.instance);
            r(m, "COMPLEX", Complex.Instance);
            r(m, "CONVERT", null);
            r(m, "CONCAT", TextFunction.CONCAT);
            r(m, "COUNTIFS", Countifs.instance);
            r(m, "COUPDAYBS", null);
            r(m, "COUPDAYS", null);
            r(m, "COUPDAYSNC", null);
            r(m, "COUPNCD", null);
            r(m, "COUPNUM", null);
            r(m, "COUPPCD", null);
            r(m, "CUBEKPIMEMBER", null);
            r(m, "CUBEMEMBER", null);
            r(m, "CUBEMEMBERPROPERTY", null);
            r(m, "CUBERANKEDMEMBER", null);
            r(m, "CUBESET", null);
            r(m, "CUBESETCOUNT", null);
            r(m, "CUBEVALUE", null);
            r(m, "CUMIPMT", CumIPmt.instance);
            r(m, "CUMPRINC", CumPrinc.instance);
            r(m, "COMBINA", Combina.instance);
            r(m, "DAYS", Days.instance);
            r(m, "DEC2BIN", Dec2Bin.instance);
            r(m, "DEC2HEX", Dec2Hex.instance);
            r(m, "DEC2OCT", Dec2Oct.instance);
            r(m, "DECIMAL", DecimalFunc.instance);
            r(m, "DELTA", Delta.instance);
            r(m, "DISC", null);
            r(m, "DOLLARDE", DollarDe.instance);
            r(m, "DOLLARFR", DollarFr.instance);
            r(m, "DURATION", null);
            r(m, "EDATE", EDate.Instance);
            r(m, "EFFECT", Effect.instance);
            r(m, "EOMONTH", EOMonth.instance);
            r(m, "ERF", Erf.instance);
            r(m, "ERFC", ErfC.instance);
            r(m, "FACTDOUBLE", FactDouble.instance);
            r(m, "FVSCHEDULE", null);
            r(m, "GAMMA", Gamma.instance);
            r(m, "GAMMALN.PRECISE", GammaLnPrecise.instance);
            r(m, "GCD", Gcd.instance);
            r(m, "GESTEP", GeStep.instance);
            r(m, "HEX2BIN", Hex2Bin.instance);
            r(m, "HEX2DEC", Hex2Dec.instance);
            r(m, "HEX2OCT", Hex2Oct.instance);
            r(m, "IFERROR", IfError.Instance);
            r(m, "IFNA", IfNa.instance);
            r(m, "IFS", Ifs.Instance);
            r(m, "IMABS", ImAbs.instance);
            r(m, "IMAGINARY", Imaginary.instance);
            r(m, "IMARGUMENT", ImArgument.instance);
            r(m, "IMCONJUGATE", ImConjugate.instance);
            r(m, "IMCOS", ImCos.instance);
            r(m, "IMCOSH", ImCosh.instance);
            r(m, "IMCOT", ImCot.instance);
            r(m, "IMCSC", ImCsc.instance);
            r(m, "IMCSCH", ImCsch.instance);
            r(m, "IMDIV", ImDiv.instance);
            r(m, "IMEXP", ImExp.instance);
            r(m, "IMLN", ImLn.instance);
            r(m, "IMLOG10", ImLog10.instance);
            r(m, "IMLOG2", ImLog2.instance);
            r(m, "IMPOWER", ImPower.instance);
            r(m, "IMPRODUCT", ImProduct.instance);
            r(m, "IMREAL", ImReal.instance);
            r(m, "IMSEC", ImSec.instance);
            r(m, "IMSECH", ImSech.instance);
            r(m, "IMSIN", ImSin.instance);
            r(m, "IMSINH", ImSinh.instance);
            r(m, "IMSQRT", ImSqrt.instance);
            r(m, "IMSUB", ImSub.instance);
            r(m, "IMSUM", ImSum.instance);
            r(m, "IMTAN", ImTan.instance);
            r(m, "IMTANH", ImTanh.instance);
            r(m, "INTRATE", null);
            r(m, "ISEVEN", ParityFunction.IS_EVEN);
            r(m, "ISFORMULA", IsFormula.instance);
            r(m, "ISODD", ParityFunction.IS_ODD);
            r(m, "ISOWEEKNUM", IsoWeekNum.instance);
            r(m, "JIS", null);
            r(m, "LCM", Lcm.instance);
            r(m, "MAXIFS", Maxifs.instance);
            r(m, "MDURATION", null);
            r(m, "MINIFS", Minifs.instance);
            r(m, "MROUND", MRound.Instance);
            r(m, "MULTINOMIAL", Multinomial.instance);
            r(m, "NETWORKDAYS", NetworkdaysFunction.instance);
            r(m, "NOMINAL", Nominal.instance);
            r(m, "PERMUTATIONA", PermutationA.instance);
            r(m, "NORM.DIST", NormDist.instance);
            r(m, "NORM.S.DIST", NormSDist.instance);
            r(m, "NORM.INV", NormInv.instance);
            r(m, "NORM.S.INV", NormSInv.instance);
            r(m, "NUMBERVALUE", NumberValueFunction.instance);
            r(m, "OCT2BIN", Oct2Bin.instance);
            r(m, "OCT2DEC", Oct2Dec.instance);
            r(m, "OCT2HEX", Oct2Hex.instance);
            r(m, "ODDFPRICE", null);
            r(m, "ODDFYIELD", null);
            r(m, "ODDLPRICE", null);
            r(m, "ODDLYIELD", null);
            r(m, "PRICE", null);
            r(m, "PRICEDISC", null);
            r(m, "PRICEMAT", null);
            r(m, "QUOTIENT", Quotient.instance);
            r(m, "RANDBETWEEN", RandBetween.Instance);
            r(m, "RECEIVED", null);
            r(m, "RTD", null);
            r(m, "SERIESSUM", SeriesSum.instance);
            r(m, "SINGLE", XlfnSingle.instance);
            r(m, "SQRTPI", SqrtPi.instance);
            r(m, "SUMIFS", Sumifs.instance);
            r(m, "SWITCH", Switch.instance);
            r(m, "TBILLEQ", null);
            r(m, "TBILLPRICE", null);
            r(m, "TBILLYIELD", null);
            r(m, "TEXTAFTER", TextAfter.instance);
            r(m, "TEXTBEFORE", TextBefore.instance);
            r(m, "TEXTJOIN", TextJoinFunction.instance);
            r(m, "T.INV", TInv.instance);
            r(m, "UNICHAR", UniChar.instance);
            r(m, "UNICODE", Unicode.instance);
            r(m, "WEEKNUM", WeekNum.instance);
            r(m, "WORKDAY", WorkdayFunction.instance);
            r(m, "WORKDAY.INTL", WorkdayIntlFunction.instance);
            r(m, "XIRR", null);
            r(m, "XOR", Xor.instance);
            r(m, "XLOOKUP", XLookupFunction.instance);
            r(m, "XMATCH", XMatchFunction.instance);
            r(m, "XNPV", null);
            r(m, "YEARFRAC", YearFrac.instance);
            r(m, "YIELD", null);
            r(m, "YIELDDISC", null);
            r(m, "YIELDMAT", null);

            r(m, "CEILING.MATH", CeilingMath.Instance);
            r(m, "FLOOR.MATH", FloorMath.Instance);

            r(m, "STDEV.S", StdevS.Instance);
            r(m, "STDEV.P", StdevP.Instance);
            r(m, "VAR.S", VarS.Instance);
            r(m, "VAR.P", VarP.Instance);

            return m;
        }

        private static void r(Dictionary<String, FreeRefFunction> m, String functionName, FreeRefFunction pFunc)
        {
            FreeRefFunction func = pFunc == null ? new NotImplemented(functionName) : pFunc;
            m[functionName]= func;
        }

        public static bool IsATPFunction(String name)
        {
            //AnalysisToolPak inst = (AnalysisToolPak)instance;
            return AnalysisToolPak._functionsByName.ContainsKey(name);
        }

        /**
         * Returns a collection of ATP function names implemented by POI.
         *
         * @return an array of supported functions
         * @since 3.8 beta6
         */
        public static ReadOnlyCollection<String> GetSupportedFunctionNames()
        {
            AnalysisToolPak inst = (AnalysisToolPak)instance;
            List<String> lst = new List<String>();
            foreach (KeyValuePair<String, FreeRefFunction> me in AnalysisToolPak._functionsByName)
            {
                FreeRefFunction func = me.Value;
                if (func != null && !(func is NotImplemented))
                {
                    lst.Add(me.Key);
                }
            }
            return lst.AsReadOnly(); //Collections.unmodifiableCollection(lst);
        }

        /**
         * Returns a collection of ATP function names NOT implemented by POI.
         *
         * @return an array of not supported functions
         * @since 3.8 beta6
         */
        public static ReadOnlyCollection<String> GetNotSupportedFunctionNames()
        {
            AnalysisToolPak inst = (AnalysisToolPak)instance;
            List<String> lst = new List<String>();
            foreach (KeyValuePair<String, FreeRefFunction> me in AnalysisToolPak._functionsByName)
            {
                FreeRefFunction func = me.Value;
                if (func != null && (func is NotImplemented))
                {
                    lst.Add(me.Key);
                }
            }
            return lst.AsReadOnly(); //Collections.unmodifiableCollection(lst);
        }

        /**
         * Register a ATP function in runtime.
         *
         * @param name  the function name
         * @param func  the functoin to register
         * @throws ArgumentException if the function is unknown or already  registered.
         * @since 3.8 beta6
         */
        public static void RegisterFunction(String name, FreeRefFunction func)
        {
            AnalysisToolPak inst = (AnalysisToolPak)instance;
            if (!IsATPFunction(name))
            {
                FunctionMetadata metaData = FunctionMetadataRegistry.GetFunctionByName(name);
                if (metaData != null)
                {
                    throw new ArgumentException(name + " is a built-in Excel function. " +
                            "Use FunctoinEval.RegisterFunction(String name, Function func) instead.");
                }
                else
                {
                    throw new ArgumentException(name + " is not a function from the Excel Analysis Toolpack.");
                }
            }
            FreeRefFunction f = inst.FindFunction(name);
            if (f != null && !(f is NotImplemented))
            {
                throw new ArgumentException("POI already implememts " + name +
                        ". You cannot override POI's implementations of Excel functions");
            }
            if (_functionsByName.ContainsKey(name))
                _functionsByName[name] = func;
            else
                _functionsByName.Add(name, func);
        }
    }
}
