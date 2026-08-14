using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NPOI.OpenXml4Net.OPC;
using NPOI.SS.UserModel;
using NPOI.XSSF.EventUserModel;
using NPOI.XSSF.Model;
using NPOI.XSSF.UserModel;
using NUnit.Framework;

namespace NPOI.XSSF.EventUserModel
{
    [TestFixture]
    public class TestXSSFSheetXMLHandler
    {
        private sealed class Collector : SheetContentsHandler
        {
            public readonly List<string> Events = new List<string>();
            public void StartRow(int r) => Events.Add($"SR:{r}");
            public void EndRow(int r) => Events.Add($"ER:{r}");
            public void Cell(string reference, string text, object raw, StreamValueKind kind, bool isFormula)
            {
                // Format raw explicitly with InvariantCulture: boxed as `object`, raw's default
                // ToString() (via plain string interpolation) dispatches to e.g. double.ToString()
                // with no provider, which resolves to the ambient thread culture and would render
                // "12.5" as "12,5" under de-DE/fr-FR - masking the exact culture-invariance this
                // test exists to check on the handler's own parsing, not on this test helper.
                string rawText = raw is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : raw?.ToString() ?? "";
                // ":F" suffix records formula-ness, now reported separately from the value kind.
                Events.Add($"C:{reference}:{kind}:{rawText}:{text}" + (isFormula ? ":F" : ""));
            }
            public void HeaderFooter(string t, bool h, string n) { }
        }

        [Test]
        public void ParsesNumbersStringsBoolsDatesFormulas()
        {
            // Author a workbook with known cells, then stream-read its first sheet.
            string path = Path.Combine(Path.GetTempPath(), "xl263_h_" + Guid.NewGuid().ToString("N") + ".xlsx");
            IWorkbook wb = new XSSFWorkbook();
            ISheet sh = wb.CreateSheet("S");
            IRow row = sh.CreateRow(0);
            row.CreateCell(0).SetCellValue("text");
            row.CreateCell(1).SetCellValue(12.5);
            row.CreateCell(2).SetCellValue(true);
            ICell date = row.CreateCell(3);
            ICellStyle ds = wb.CreateCellStyle();
            ds.DataFormat = wb.CreateDataFormat().GetFormat("yyyy-mm-dd");
            date.CellStyle = ds;
            date.SetCellValue(new DateTime(2026, 8, 6));
            ICell formula = row.CreateCell(4);
            formula.CellFormula = "B1*2";
            using (FileStream fs = File.Create(path)) { wb.Write(fs); }
            wb.Close();

            var events = new List<string>();
            OPCPackage pkg = OPCPackage.Open(path);
            try
            {
                XSSFReader reader = new XSSFReader(pkg);
                var handler = new Collector();
                using (Stream s = reader.GetSheetStream(0))
                {
                    var h = new XSSFSheetXMLHandler(s, reader.GetStylesTable(), reader.GetSharedStringsTable(), handler);
                    while (h.ParseNextRow()) { }
                }
                events = handler.Events;
            }
            finally
            {
                pkg.Close();
            }
            File.Delete(path);

            Assert.Contains("SR:0", events);
            Assert.Contains("ER:0", events);
            CollectionAssert.Contains(events, "C:A1:String:text:text");
            Assert.IsTrue(events.Exists(e => e.StartsWith("C:B1:Number:12.5")));
            Assert.IsTrue(events.Exists(e => e.StartsWith("C:C1:Boolean:")));
            Assert.IsTrue(events.Exists(e => e.StartsWith("C:D1:Date:")));
            // NPOI writes this freshly-set formula unevaluated (a bare "<f>B1*2</f>" with no cached
            // <v>), so its VALUE kind is Blank while the ":F" flag still marks it a formula - the
            // formula-ness and the value type are now reported independently. A formula carrying a
            // cached typed value is covered separately (F1 in the inline-string fragment test).
            Assert.IsTrue(events.Exists(e => e.StartsWith("C:E1:Blank:") && e.EndsWith(":F")));
        }

        [TestCase("de-DE")]
        [TestCase("fr-FR")]
        public void RawNumbersAreCultureInvariant(string culture)
        {
            var prev = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            try
            {
                string path = Path.Combine(Path.GetTempPath(), "xl263_c_" + Guid.NewGuid().ToString("N") + ".xlsx");
                IWorkbook wb = new XSSFWorkbook();
                wb.CreateSheet("S").CreateRow(0).CreateCell(0).SetCellValue(12.5);
                using (FileStream fs = File.Create(path)) { wb.Write(fs); }
                wb.Close();

                OPCPackage pkg = OPCPackage.Open(path);
                try
                {
                    XSSFReader reader = new XSSFReader(pkg);
                    var handler = new Collector();
                    using (Stream s = reader.GetSheetStream(0))
                    {
                        var h = new XSSFSheetXMLHandler(s, reader.GetStylesTable(), reader.GetSharedStringsTable(), handler);
                        while (h.ParseNextRow()) { }
                    }
                    Assert.IsTrue(handler.Events.Exists(e => e.StartsWith("C:A1:Number:12.5")));
                }
                finally
                {
                    pkg.Close();
                }
                File.Delete(path);
            }
            finally { System.Threading.Thread.CurrentThread.CurrentCulture = prev; }
        }

        [Test]
        public void ParsesInlineStringFormulaStringErrorAndBlankCells()
        {
            // Fed as a literal worksheet-XML fragment (bypassing XSSFWorkbook, which always
            // prefers shared strings) so the t="inlineStr" / t="str" / t="e" / self-closing-cell
            // paths in ReadInlineString/EmitCell get real coverage.
            const string xml =
                "<?xml version=\"1.0\"?>" +
                "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<sheetData>" +
                "<row r=\"1\">" +
                "<c r=\"A1\" t=\"inlineStr\"><is><t>Hello</t></is></c>" +
                "<c r=\"B1\" t=\"inlineStr\"><is><r><t>Wor</t></r><r><t>ld</t></r></is></c>" +
                "<c r=\"C1\" t=\"str\"><f>A1</f><v>calc</v></c>" +
                "<c r=\"D1\" t=\"e\"><v>#DIV/0!</v></c>" +
                "<c r=\"E1\"/>" +
                "<c r=\"F1\"><f>B1*2</f><v>25</v></c>" +
                "</row>" +
                "</sheetData>" +
                "</worksheet>";

            var styles = new StylesTable();
            var strings = new ReadOnlySharedStringsTable(new MemoryStream(Encoding.UTF8.GetBytes(
                "<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"/>")));
            var handler = new Collector();
            using (Stream s = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                var h = new XSSFSheetXMLHandler(s, styles, strings, handler);
                while (h.ParseNextRow()) { }
            }

            CollectionAssert.Contains(handler.Events, "C:A1:String:Hello:Hello");
            CollectionAssert.Contains(handler.Events, "C:B1:String:World:World");
            // t="str" is a cached formula-string result: value kind String, flagged as a formula.
            Assert.IsTrue(handler.Events.Exists(e => e.StartsWith("C:C1:String:calc") && e.EndsWith(":F")));
            Assert.IsTrue(handler.Events.Exists(e => e.StartsWith("C:D1:Error:#DIV/0!")));
            Assert.IsTrue(handler.Events.Exists(e => e.StartsWith("C:E1:Blank:")));
            // A formula with a cached numeric result surfaces its VALUE kind (Number) AND the
            // formula flag - the value type is no longer hidden behind an opaque "Formula" kind.
            Assert.IsTrue(handler.Events.Exists(e => e.StartsWith("C:F1:Number:25") && e.EndsWith(":F")));
        }

        [Test]
        public void ParsesUnstyledNumericCellsWithoutThrowing()
        {
            // Spec-valid and common: minimal writers (SXSSF and others) emit numeric cells
            // with no "s" (style) attribute at all, e.g. <c r="A1"><v>123</v></c>. An unstyled
            // numeric cell uses Excel's implicit "General" format; EmitCell must not pass a
            // null format string down to DataFormatter.FormatRawCellContents in that case.
            const string xml =
                "<?xml version=\"1.0\"?>" +
                "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<sheetData>" +
                "<row r=\"1\">" +
                "<c r=\"A1\"><v>123</v></c>" +
                "<c r=\"B1\" t=\"n\"><v>45.6</v></c>" +
                "</row>" +
                "</sheetData>" +
                "</worksheet>";

            var styles = new StylesTable();
            var strings = new ReadOnlySharedStringsTable(new MemoryStream(Encoding.UTF8.GetBytes(
                "<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"/>")));
            var handler = new Collector();
            using (Stream s = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                var h = new XSSFSheetXMLHandler(s, styles, strings, handler);
                Assert.DoesNotThrow(() => { while (h.ParseNextRow()) { } });
            }

            Assert.IsTrue(handler.Events.Exists(e => e.StartsWith("C:A1:Number:123")));
            Assert.IsTrue(handler.Events.Exists(e => e.StartsWith("C:B1:Number:45.6")));
            // The formatted text must be present and non-empty, not just the raw value.
            string a1 = handler.Events.Find(e => e.StartsWith("C:A1:Number:"));
            Assert.IsFalse(string.IsNullOrEmpty(a1.Split(':')[4]));
        }

        [Test]
        public void SkipsPhoneticRunsAndKeepsCDataInInlineStrings()
        {
            // A1: an inline string carrying a phonetic (ruby) guide - the <rPh> body "かんじ" is
            // pronunciation metadata and must NOT be merged into the base value "漢字".
            // B1: an inline string whose <t> interleaves plain text with a CDATA section - both
            // pieces must survive (exercises the CDATA branch of ReadElementText).
            const string xml =
                "<?xml version=\"1.0\"?>" +
                "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<sheetData>" +
                "<row r=\"1\">" +
                "<c r=\"A1\" t=\"inlineStr\"><is><t>漢字</t><rPh sb=\"0\" eb=\"2\"><t>かんじ</t></rPh></is></c>" +
                "<c r=\"B1\" t=\"inlineStr\"><is><t>ab<![CDATA[cd]]></t></is></c>" +
                "</row>" +
                "</sheetData>" +
                "</worksheet>";

            var styles = new StylesTable();
            var strings = new ReadOnlySharedStringsTable(new MemoryStream(Encoding.UTF8.GetBytes(
                "<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"/>")));
            var handler = new Collector();
            using (Stream s = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                var h = new XSSFSheetXMLHandler(s, styles, strings, handler);
                while (h.ParseNextRow()) { }
            }

            CollectionAssert.Contains(handler.Events, "C:A1:String:漢字:漢字");
            CollectionAssert.Contains(handler.Events, "C:B1:String:abcd:abcd");
        }

        [Test]
        public void ImplicitRowAndColumnPositions()
        {
            // Neither <row> nor <c> carries an "r" in the first two rows: positions are implied by
            // document order. An explicit r ("C2", row "5") must re-sync the running counters so a
            // following bare element continues from the right place.
            const string xml =
                "<?xml version=\"1.0\"?>" +
                "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<sheetData>" +
                "<row>" +
                "<c t=\"inlineStr\"><is><t>a</t></is></c>" +
                "<c t=\"inlineStr\"><is><t>b</t></is></c>" +
                "</row>" +
                "<row>" +
                "<c r=\"C2\" t=\"inlineStr\"><is><t>c</t></is></c>" +
                "<c t=\"inlineStr\"><is><t>d</t></is></c>" +
                "</row>" +
                "<row r=\"5\">" +
                "<c t=\"inlineStr\"><is><t>e</t></is></c>" +
                "</row>" +
                "</sheetData>" +
                "</worksheet>";

            var styles = new StylesTable();
            var strings = new ReadOnlySharedStringsTable(new MemoryStream(Encoding.UTF8.GetBytes(
                "<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"/>")));
            var handler = new Collector();
            using (Stream s = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                var h = new XSSFSheetXMLHandler(s, styles, strings, handler);
                while (h.ParseNextRow()) { }
            }

            // Implicit rows number sequentially from 0; the explicit r="5" row re-syncs to index 4.
            Assert.Contains("SR:0", handler.Events);
            Assert.Contains("SR:1", handler.Events);
            Assert.Contains("SR:4", handler.Events);
            // Implicit columns from 0 (A1, B1); explicit C2 re-syncs so the next bare cell is D2;
            // and the first bare cell of the r="5" row lands at A5 (column 0).
            CollectionAssert.Contains(handler.Events, "C:A1:String:a:a");
            CollectionAssert.Contains(handler.Events, "C:B1:String:b:b");
            CollectionAssert.Contains(handler.Events, "C:C2:String:c:c");
            CollectionAssert.Contains(handler.Events, "C:D2:String:d:d");
            CollectionAssert.Contains(handler.Events, "C:A5:String:e:e");
        }

        private sealed class FirstDateCapture : SheetContentsHandler
        {
            public DateTime? Value;
            public void StartRow(int r) { }
            public void EndRow(int r) { }
            public void Cell(string reference, string text, object raw, StreamValueKind kind, bool isFormula)
            {
                if (Value == null && kind == StreamValueKind.Date && raw is DateTime dt)
                {
                    Value = dt;
                }
            }
            public void HeaderFooter(string t, bool h, string n) { }
        }

        private static DateTime ReadFirstDate(XSSFReader reader, StylesTable styles,
            ReadOnlySharedStringsTable strings, bool use1904Windowing)
        {
            var cap = new FirstDateCapture();
            using (Stream s = reader.GetSheetStream(0))
            {
                var h = new XSSFSheetXMLHandler(s, styles, strings, cap, use1904Windowing);
                while (h.ParseNextRow()) { }
            }
            Assert.IsTrue(cap.Value.HasValue, "expected a date cell");
            return cap.Value.Value;
        }

        [Test]
        public void Use1904WindowingShiftsDates()
        {
            // Author a 1900-based workbook holding a date-formatted numeric serial, then read the
            // SAME serial twice: once with 1900 windowing (the default) and once with 1904. The
            // 1904 epoch sits exactly 1462 days (~4 years and a day) after the 1900 epoch, so the
            // 1904 reading must land that much later - proving the handler honours the parameter.
            string path = Path.Combine(Path.GetTempPath(), "xl263_1904_" + Guid.NewGuid().ToString("N") + ".xlsx");
            IWorkbook wb = new XSSFWorkbook();
            ICell date = wb.CreateSheet("S").CreateRow(0).CreateCell(0);
            ICellStyle ds = wb.CreateCellStyle();
            ds.DataFormat = wb.CreateDataFormat().GetFormat("yyyy-mm-dd");
            date.CellStyle = ds;
            date.SetCellValue(new DateTime(2000, 1, 1));
            using (FileStream fs = File.Create(path)) { wb.Write(fs); }
            wb.Close();

            OPCPackage pkg = OPCPackage.Open(path);
            try
            {
                XSSFReader reader = new XSSFReader(pkg);
                StylesTable styles = reader.GetStylesTable();
                ReadOnlySharedStringsTable strings = reader.GetSharedStringsTable();

                // This workbook does not set date1904, so the reader reports the 1900 system.
                Assert.IsFalse(reader.IsDate1904);

                DateTime d1900 = ReadFirstDate(reader, styles, strings, false);
                DateTime d1904 = ReadFirstDate(reader, styles, strings, true);

                Assert.AreEqual(new DateTime(2000, 1, 1), d1900);
                Assert.That((d1904 - d1900).TotalDays, Is.EqualTo(1462).Within(1));
            }
            finally
            {
                pkg.Close();
            }
            File.Delete(path);
        }
    }
}
