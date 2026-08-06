using System.IO;
using System.Text;
using NPOI.XSSF.Model;
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
    }
}
