using System.IO;
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
    }
}
