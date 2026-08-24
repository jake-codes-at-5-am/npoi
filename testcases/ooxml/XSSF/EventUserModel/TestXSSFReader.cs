using System.IO;
using System.IO.Compression;
using System.Text;
using NPOI.OpenXml4Net.OPC;
using NPOI.SS.UserModel;
using NPOI.XSSF.EventUserModel;
using NPOI.XSSF.UserModel;
using NUnit.Framework;

namespace NPOI.XSSF.EventUserModel
{
    [TestFixture]
    public class TestXSSFReader
    {
        private static string WriteTwoSheetBook()
        {
            string path = Path.Combine(Path.GetTempPath(), "xl263_reader_" + System.Guid.NewGuid().ToString("N") + ".xlsx");
            IWorkbook wb = new XSSFWorkbook();
            wb.CreateSheet("First").CreateRow(0).CreateCell(0).SetCellValue("hello");
            wb.CreateSheet("Second").CreateRow(0).CreateCell(0).SetCellValue(42.0);
            using (FileStream fs = File.Create(path)) { wb.Write(fs); }
            wb.Close();
            return path;
        }

        [Test]
        public void EnumeratesSheetsInOrder()
        {
            string path = WriteTwoSheetBook();
            OPCPackage pkg = OPCPackage.Open(path);
            try
            {
                XSSFReader reader = new XSSFReader(pkg);
                var sheets = reader.GetSheets();
                Assert.AreEqual(2, sheets.Count);
                Assert.AreEqual("First", sheets[0].Name);
                Assert.AreEqual("Second", sheets[1].Name);
                Assert.IsNotNull(reader.GetStylesTable());
                Assert.IsNotNull(reader.GetSharedStringsTable());
            }
            finally
            {
                pkg.Close();
                File.Delete(path);
            }
        }

        [Test]
        public void GetSheetStreamByNameMatchesIndex()
        {
            string path = WriteTwoSheetBook();
            OPCPackage pkg = OPCPackage.Open(path);
            try
            {
                XSSFReader reader = new XSSFReader(pkg);
                using (Stream byName = reader.GetSheetStream("Second"))
                using (Stream byIndex = reader.GetSheetStream(1))
                {
                    Assert.IsNotNull(byName);
                    Assert.IsNotNull(byIndex);
                }
                Assert.Throws<System.ArgumentException>(() => reader.GetSheetStream("Missing"));
                Assert.Throws<System.ArgumentException>(() => reader.GetSheetStream(9));
            }
            finally
            {
                pkg.Close();
                File.Delete(path);
            }
        }

        [Test]
        public void GetSheetStreamByNameIsCaseInsensitive()
        {
            // XSSFWorkbook.GetSheet(String) resolves names case-insensitively; the streaming
            // lookup must match, so "data"/"DATA" both resolve a sheet authored as "Data".
            string path = Path.Combine(Path.GetTempPath(), "xl263_ci_" + System.Guid.NewGuid().ToString("N") + ".xlsx");
            IWorkbook wb = new XSSFWorkbook();
            wb.CreateSheet("Data").CreateRow(0).CreateCell(0).SetCellValue("v");
            using (FileStream fs = File.Create(path)) { wb.Write(fs); }
            wb.Close();

            OPCPackage pkg = OPCPackage.Open(path);
            try
            {
                XSSFReader reader = new XSSFReader(pkg);
                using (Stream lower = reader.GetSheetStream("data"))
                using (Stream upper = reader.GetSheetStream("DATA"))
                {
                    Assert.IsNotNull(lower);
                    Assert.IsNotNull(upper);
                }
            }
            finally
            {
                pkg.Close();
                File.Delete(path);
            }
        }

        [Test]
        public void ResolvesTemplateContentTypeWorkbook()
        {
            // A .xltx template stores its main part under the template content type, not the plain
            // workbook one. The reader must try every workbook content type, not just WORKBOOK and
            // MACROS_WORKBOOK, or a template throws "does not contain a workbook part".
            string xlsx = WriteTwoSheetBook();
            string xltx = Path.Combine(Path.GetTempPath(), "xl263_tmpl_" + System.Guid.NewGuid().ToString("N") + ".xlsx");
            RetypeWorkbookPartAsTemplate(xlsx, xltx);

            OPCPackage pkg = OPCPackage.Open(xltx);
            try
            {
                XSSFReader reader = new XSSFReader(pkg);
                var sheets = reader.GetSheets();
                Assert.AreEqual(2, sheets.Count);
                Assert.AreEqual("First", sheets[0].Name);
                Assert.AreEqual("Second", sheets[1].Name);
            }
            finally
            {
                pkg.Close();
                File.Delete(xlsx);
                File.Delete(xltx);
            }
        }

        // Rewrites the workbook part's content type in [Content_Types].xml from the plain
        // (sheet.main+xml) type to the template (template.main+xml) type, producing a package that
        // is structurally a .xltx. Operates on the raw zip so it does not depend on NPOI internals.
        private static void RetypeWorkbookPartAsTemplate(string src, string dst)
        {
            const string plain = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml";
            const string template = "application/vnd.openxmlformats-officedocument.spreadsheetml.template.main+xml";
            byte[] bytes = File.ReadAllBytes(src);
            using (var ms = new MemoryStream())
            {
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
                {
                    ZipArchiveEntry entry = zip.GetEntry("[Content_Types].xml");
                    Assert.IsNotNull(entry, "package is missing [Content_Types].xml");
                    string xml;
                    using (var r = new StreamReader(entry.Open())) { xml = r.ReadToEnd(); }
                    Assert.IsTrue(xml.Contains(plain), "expected the plain workbook content type in the source package");
                    xml = xml.Replace(plain, template);
                    entry.Delete();
                    ZipArchiveEntry recreated = zip.CreateEntry("[Content_Types].xml");
                    using (var w = new StreamWriter(recreated.Open(), new UTF8Encoding(false))) { w.Write(xml); }
                }
                File.WriteAllBytes(dst, ms.ToArray());
            }
        }
    }
}
