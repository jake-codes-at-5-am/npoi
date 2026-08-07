using System.IO;
using System.Text;
using NPOI.OpenXml4Net.OPC;
using NPOI.Util;
using NPOI.XSSF.UserModel;
using NUnit.Framework;

namespace NPOI.XSSF.Model
{
    [TestFixture]
    public class TestReadOnlySharedStringsTable
    {
        private static Stream Xml(string body) =>
            new MemoryStream(Encoding.UTF8.GetBytes(
                "<?xml version=\"1.0\"?><sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"3\" uniqueCount=\"3\">"
                + body + "</sst>"));

        [Test]
        public void ReadsPlainAndRichStrings()
        {
            var sst = new ReadOnlySharedStringsTable(Xml(
                "<si><t>Alpha</t></si>" +
                "<si><t xml:space=\"preserve\"> Beta </t></si>" +
                "<si><r><t>Ga</t></r><r><t>mma</t></r></si>"));

            Assert.AreEqual(3, sst.Count);
            Assert.AreEqual("Alpha", sst.GetEntryAt(0));
            Assert.AreEqual(" Beta ", sst.GetEntryAt(1));
            Assert.AreEqual("Gamma", sst.GetEntryAt(2));
        }

        [Test]
        public void EmptyTableHasZeroCount()
        {
            var sst = new ReadOnlySharedStringsTable(Xml(string.Empty));
            Assert.AreEqual(0, sst.Count);
        }

        [Test]
        public void SelfClosingSiYieldsEmptyString()
        {
            var sst = new ReadOnlySharedStringsTable(Xml(
                "<si><t>First</t></si><si/><si><t>Third</t></si>"));

            Assert.AreEqual(3, sst.Count);
            Assert.AreEqual("First", sst.GetEntryAt(0));
            Assert.AreEqual(string.Empty, sst.GetEntryAt(1));
            Assert.AreEqual("Third", sst.GetEntryAt(2));
        }

        [Test]
        public void CDataInsideTIsPreserved()
        {
            var sst = new ReadOnlySharedStringsTable(Xml("<si><t><![CDATA[Hello]]></t></si>"));

            Assert.AreEqual(1, sst.Count);
            Assert.AreEqual("Hello", sst.GetEntryAt(0));
        }

        [Test]
        public void SelfClosingSiAtFirstAndLastPositionKeepIndices()
        {
            var sst = new ReadOnlySharedStringsTable(Xml(
                "<si/><si><t>Mid</t></si><si/>"));

            Assert.AreEqual(3, sst.Count);
            Assert.AreEqual(string.Empty, sst.GetEntryAt(0));
            Assert.AreEqual("Mid", sst.GetEntryAt(1));
            Assert.AreEqual(string.Empty, sst.GetEntryAt(2));
        }

        [Test]
        public void ConsecutiveSelfClosingSiEachYieldEmptyString()
        {
            var sst = new ReadOnlySharedStringsTable(Xml(
                "<si><t>Before</t></si><si/><si/><si><t>After</t></si>"));

            Assert.AreEqual(4, sst.Count);
            Assert.AreEqual("Before", sst.GetEntryAt(0));
            Assert.AreEqual(string.Empty, sst.GetEntryAt(1));
            Assert.AreEqual(string.Empty, sst.GetEntryAt(2));
            Assert.AreEqual("After", sst.GetEntryAt(3));
        }

        [Test]
        public void CDataInsideRichRunIsPreserved()
        {
            var sst = new ReadOnlySharedStringsTable(Xml(
                "<si><r><t>Ga</t></r><r><t><![CDATA[mma]]></t></r></si>"));

            Assert.AreEqual(1, sst.Count);
            Assert.AreEqual("Gamma", sst.GetEntryAt(0));
        }

        [Test]
        public void ReadsSharedStringsFromOpcPackage()
        {
            FileInfo file = TempFile.CreateTempFile("TestReadOnlySharedStringsTable-", ".xlsx");
            try
            {
                using (var wb = new XSSFWorkbook())
                {
                    var sheet = wb.CreateSheet();
                    var row = sheet.CreateRow(0);
                    row.CreateCell(0).SetCellValue("Delta");
                    row.CreateCell(1).SetCellValue("Epsilon");

                    using (var fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write))
                    {
                        wb.Write(fs);
                    }
                }

                OPCPackage pkg = OPCPackage.Open(file.FullName, PackageAccess.READ);
                try
                {
                    var sst = new ReadOnlySharedStringsTable(pkg);

                    Assert.AreEqual(2, sst.Count);
                    Assert.AreEqual("Delta", sst.GetEntryAt(0));
                    Assert.AreEqual("Epsilon", sst.GetEntryAt(1));
                }
                finally
                {
                    pkg.Close();
                }
            }
            finally
            {
                if (file.Exists)
                {
                    file.Delete();
                }
            }
        }
    }
}
