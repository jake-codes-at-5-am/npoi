using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
            public void Cell(string reference, string text, object raw, StreamValueKind kind)
            {
                // Format raw explicitly with InvariantCulture: boxed as `object`, raw's default
                // ToString() (via plain string interpolation) dispatches to e.g. double.ToString()
                // with no provider, which resolves to the ambient thread culture and would render
                // "12.5" as "12,5" under de-DE/fr-FR - masking the exact culture-invariance this
                // test exists to check on the handler's own parsing, not on this test helper.
                string rawText = raw is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : raw?.ToString() ?? "";
                Events.Add($"C:{reference}:{kind}:{rawText}:{text}");
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
            Assert.IsTrue(events.Exists(e => e.StartsWith("C:E1:Formula:")));
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
    }
}
