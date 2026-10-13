/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
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
using NPOI.OpenXmlFormats.Vml;
using NPOI.OpenXmlFormats.Vml.Office;
using NPOI.OpenXmlFormats.Vml.Spreadsheet;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;

namespace TestCases.XSSF.UserModel
{

    /**
     * @author Yegor Kozlov
     */
    [TestFixture]
    public class TestXSSFVMLDrawing
    {
        [Test]
        public void TestNew()
        {
            XSSFVMLDrawing vml = new XSSFVMLDrawing();
            ArrayList items = vml.GetItems();
            Assert.AreEqual(2, items.Count);
            Assert.IsTrue(items[0] is CT_ShapeLayout);
            CT_ShapeLayout layout = (CT_ShapeLayout)items[0];
            Assert.AreEqual(ST_Ext.edit, layout.ext);
            Assert.AreEqual(ST_Ext.edit, layout.idmap.ext);
            Assert.AreEqual("1", layout.idmap.data);

            Assert.IsTrue(items[1] is CT_Shapetype);
            CT_Shapetype type = (CT_Shapetype)items[1];
            Assert.AreEqual("21600,21600", type.coordsize);
            Assert.AreEqual(202.0f, type.spt);
            Assert.AreEqual("m,l,21600r21600,l21600,xe", type.path2);
            Assert.AreEqual("_x0000_t202", type.id);
            Assert.AreEqual(NPOI.OpenXmlFormats.Vml.ST_TrueFalse.t, type.path.gradientshapeok);
            Assert.AreEqual(ST_ConnectType.rect, type.path.connecttype);

            CT_Shape shape = vml.newCommentShape();
            Assert.AreEqual(3, items.Count);
            Assert.AreSame(items[2], shape);
            Assert.AreEqual("#_x0000_t202", shape.type);
            Assert.AreEqual("position:absolute; visibility:hidden", shape.style);
            Assert.AreEqual("#ffffe1", shape.fillcolor);
            Assert.AreEqual(ST_InsetMode.auto, shape.insetmode);
            Assert.AreEqual("#ffffe1", shape.fill.color);
            CT_Shadow shadow = shape.shadow;
            Assert.AreEqual(NPOI.OpenXmlFormats.Vml.ST_TrueFalse.t, shadow.on);
            Assert.AreEqual("black", shadow.color);
            Assert.AreEqual(NPOI.OpenXmlFormats.Vml.ST_TrueFalse.t, shadow.obscured);
            Assert.AreEqual(ST_ConnectType.none, shape.path.connecttype);
            Assert.AreEqual("mso-direction-alt:auto", shape.textbox.style);
            CT_ClientData cldata = shape.GetClientDataArray(0);
            Assert.AreEqual(ST_ObjectType.Note, cldata.ObjectType);
            Assert.AreEqual(1, cldata.SizeOfMoveWithCellsArray());
            Assert.AreEqual(1, cldata.SizeOfSizeWithCellsArray());
            Assert.AreEqual("1, 15, 0, 2, 3, 15, 3, 16", cldata.anchor);
            Assert.AreEqual(ST_TrueFalseBlank.@false, cldata.autoFill);
            Assert.AreEqual(0, cldata.GetRowArray(0));
            Assert.AreEqual(0, cldata.GetColumnArray(0));

            //each of the properties of CT_ClientData should occurs 0 or 1 times, and CT_ClientData has multiple properties.
            //Assert.AreEqual("[]", cldata.GetVisibleList().ToString());
            Assert.AreEqual(ST_TrueFalseBlank.NONE, cldata.visible);
            cldata.visible = (ST_TrueFalseBlank)Enum.Parse(typeof(ST_TrueFalseBlank), "true");
            Assert.AreEqual(ST_TrueFalseBlank.@true, cldata.visible);
            //serialize and read again
            MemoryStream out1 = new MemoryStream();
            vml.Write(out1);

            XSSFVMLDrawing vml2 = new XSSFVMLDrawing();
            vml2.Read(new MemoryStream(out1.ToArray()));
            ArrayList items2 = vml2.GetItems();
            Assert.AreEqual(3, items2.Count);
            Assert.IsTrue(items2[0] is CT_ShapeLayout);
            Assert.IsTrue(items2[1] is CT_Shapetype);
            Assert.IsTrue(items2[2] is CT_Shape);
        }
        [Test]
        public void TestFindCommentShape()
        {

            XSSFVMLDrawing vml = new XSSFVMLDrawing();
            vml.Read(POIDataSamples.GetSpreadSheetInstance().OpenResourceAsStream("vmlDrawing1.vml"));

            CT_Shape sh_a1 = vml.FindCommentShape(0, 0);
            Assert.IsNotNull(sh_a1);
            Assert.AreEqual("_x0000_s1025", sh_a1.id);

            CT_Shape sh_b1 = vml.FindCommentShape(0, 1);
            Assert.IsNotNull(sh_b1);
            Assert.AreEqual("_x0000_s1026", sh_b1.id);

            CT_Shape sh_c1 = vml.FindCommentShape(0, 2);
            Assert.IsNull(sh_c1);

            CT_Shape sh_d1 = vml.newCommentShape();
            Assert.AreEqual("_x0000_s1027", sh_d1.id);
            sh_d1.GetClientDataArray(0).SetRowArray(0, 0);
            sh_d1.GetClientDataArray(0).SetColumnArray(0, 3);
            Assert.AreSame(sh_d1, vml.FindCommentShape(0, 3));

            //newly created drawing
            XSSFVMLDrawing newVml = new XSSFVMLDrawing();
            Assert.IsNull(newVml.FindCommentShape(0, 0));

            sh_a1 = newVml.newCommentShape();
            Assert.AreEqual("_x0000_s1025", sh_a1.id);
            sh_a1.GetClientDataArray(0).SetRowArray(0, 0);
            sh_a1.GetClientDataArray(0).SetColumnArray(0, 1);
            Assert.AreSame(sh_a1, newVml.FindCommentShape(0, 1));
        }
        [Test]
        public void TestRead()
        {
            XSSFVMLDrawing vml = new XSSFVMLDrawing();

            // Act
            TestDelegate testDelegate = () => vml.Read(POIDataSamples.GetSpreadSheetInstance().OpenResourceAsStream("vmlDrawing1.vml"));

            // Assert
            Assert.DoesNotThrow(testDelegate);
        }
        [Test]
        public void TestRemoveCommentShape()
        {
            XSSFVMLDrawing vml = new XSSFVMLDrawing();
            vml.Read(POIDataSamples.GetSpreadSheetInstance().OpenResourceAsStream("vmlDrawing1.vml"));

            CT_Shape sh_a1 = vml.FindCommentShape(0, 0);
            Assert.IsNotNull(sh_a1);

            Assert.IsTrue(vml.RemoveCommentShape(0, 0));
            Assert.IsNull(vml.FindCommentShape(0, 0));

        }
        [Test]
        public void TestCommentShowsBox()
        {
            XSSFWorkbook wb = new XSSFWorkbook();
            wb.CreateSheet();
            XSSFSheet sheet = (XSSFSheet)wb.GetSheetAt(0);
            XSSFCell cell = (XSSFCell)sheet.CreateRow(0).CreateCell(0);
            XSSFDrawing drawing = (XSSFDrawing)sheet.CreateDrawingPatriarch();
            XSSFCreationHelper factory = (XSSFCreationHelper)wb.GetCreationHelper();
            XSSFClientAnchor anchor = (XSSFClientAnchor)factory.CreateClientAnchor();
            anchor.Col1 = cell.ColumnIndex;
            anchor.Col2 = cell.ColumnIndex + 3;
            anchor.Row1 = cell.RowIndex;
            anchor.Row2 = cell.RowIndex + 5;
            XSSFComment comment = (XSSFComment)drawing.CreateCellComment(anchor);
            XSSFRichTextString str = (XSSFRichTextString)factory.CreateRichTextString("this is a comment");
            comment.String = str;
            cell.CellComment = comment;

            XSSFVMLDrawing vml = sheet.GetVMLDrawing(false);
            CT_Shapetype shapetype = null;
            ArrayList items = vml.GetItems();
            foreach (object o in items)
                if (o is CT_Shapetype)
                    shapetype = (CT_Shapetype)o;
            Assert.AreEqual(NPOI.OpenXmlFormats.Vml.ST_TrueFalse.t, shapetype.stroked);
            Assert.AreEqual(NPOI.OpenXmlFormats.Vml.ST_TrueFalse.t, shapetype.filled);

            using (MemoryStream ws = new MemoryStream())
            {
                wb.Write(ws);

                using (MemoryStream rs = new MemoryStream(ws.GetBuffer()))
                {
                    wb = new XSSFWorkbook(rs);
                    sheet = (XSSFSheet)wb.GetSheetAt(0);

                    vml = sheet.GetVMLDrawing(false);
                    shapetype = null;
                    items = vml.GetItems();
                    foreach (object o in items)
                        if (o is CT_Shapetype)
                            shapetype = (CT_Shapetype)o;

                    //wb.Write(new FileStream("comments.xlsx", FileMode.Create));
                    //using (MemoryStream ws2 = new MemoryStream())
                    //{
                    //    vml.Write(ws2);
                    //    throw new System.Exception(System.Text.Encoding.UTF8.GetString(ws2.GetBuffer()));
                    //}

                    Assert.AreEqual(NPOI.OpenXmlFormats.Vml.ST_TrueFalse.t, shapetype.stroked);
                    Assert.AreEqual(NPOI.OpenXmlFormats.Vml.ST_TrueFalse.t, shapetype.filled);
                }
            }
        }

        private const string VmlHeader =
            "<xml xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\">"
            + "<o:shapelayout v:ext=\"edit\"><o:idmap v:ext=\"edit\" data=\"1\"/></o:shapelayout>";

        private const string CommentShapetype =
            "<v:shapetype id=\"_x0000_t202\" coordsize=\"21600,21600\" o:spt=\"202\" path=\"m,l,21600r21600,l21600,xe\">"
            + "<v:stroke joinstyle=\"miter\"/><v:path gradientshapeok=\"t\" o:connecttype=\"rect\"/></v:shapetype>";

        private const string ButtonShapetype =
            "<v:shapetype id=\"_x0000_t201\" coordsize=\"21600,21600\" o:spt=\"201\" path=\"m,l,21600r21600,l21600,xe\">"
            + "<v:stroke joinstyle=\"miter\"/>"
            + "<v:path shadowok=\"f\" o:extrusionok=\"f\" strokeok=\"f\" fillok=\"f\" o:connecttype=\"rect\"/>"
            + "<o:lock v:ext=\"edit\" shapetype=\"t\"/></v:shapetype>";

        private static XmlDocument RoundTrip(string vmlXml, out XSSFVMLDrawing vml, out XmlNamespaceManager nsmgr)
        {
            vml = new XSSFVMLDrawing();
            vml.Read(new MemoryStream(Encoding.UTF8.GetBytes(vmlXml)));
            return Write(vml, out nsmgr);
        }

        private static XmlDocument Write(XSSFVMLDrawing vml, out XmlNamespaceManager nsmgr)
        {
            MemoryStream out1 = new MemoryStream();
            vml.Write(out1);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(Encoding.UTF8.GetString(out1.ToArray()));
            nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("v", "urn:schemas-microsoft-com:vml");
            nsmgr.AddNamespace("o", "urn:schemas-microsoft-com:office:office");
            nsmgr.AddNamespace("x", "urn:schemas-microsoft-com:office:excel");
            return doc;
        }

        private static void AssertEveryShapeTypeResolves(XmlDocument doc, XmlNamespaceManager nsmgr)
        {
            foreach (XmlElement shape in doc.SelectNodes("//v:shape[@type]", nsmgr))
            {
                string typeId = shape.GetAttribute("type").TrimStart('#');
                Assert.IsNotNull(doc.SelectSingleNode("//v:shapetype[@id='" + typeId + "']", nsmgr),
                    "shape " + shape.GetAttribute("id") + " references undefined shapetype " + typeId);
            }
        }

        [Test]
        public void TestShapetypeIdAndSptSurviveRoundTrip()
        {
            // XL-264: the shapetype id was replaced by the o:spt value and o:spt itself was dropped,
            // leaving every comment's type="#_x0000_t202" reference dangling
            string xml = VmlHeader + CommentShapetype
                + "<v:shape id=\"_x0000_s1025\" type=\"#_x0000_t202\" style=\"visibility:hidden\" fillcolor=\"#ffffe1\" o:insetmode=\"auto\">"
                + "<v:textbox style=\"mso-direction-alt:auto\"><div style=\"text-align:left\"></div></v:textbox>"
                + "<x:ClientData ObjectType=\"Note\"><x:Anchor>1, 15, 0, 2, 3, 15, 4, 4</x:Anchor><x:Row>0</x:Row><x:Column>0</x:Column></x:ClientData>"
                + "</v:shape></xml>";

            XSSFVMLDrawing vml;
            XmlNamespaceManager nsmgr;
            XmlDocument doc = RoundTrip(xml, out vml, out nsmgr);

            CT_Shapetype shapetype = (CT_Shapetype)vml.GetItems()[1];
            Assert.AreEqual("_x0000_t202", shapetype.id);
            Assert.AreEqual(202.0f, shapetype.spt);

            XmlElement written = (XmlElement)doc.SelectSingleNode("//v:shapetype", nsmgr);
            Assert.AreEqual("_x0000_t202", written.GetAttribute("id"));
            Assert.AreEqual("202", written.GetAttribute("spt", "urn:schemas-microsoft-com:office:office"));
            AssertEveryShapeTypeResolves(doc, nsmgr);
        }

        [Test]
        public void TestPictureShapetypeSurvivesRoundTrip()
        {
            string xml = VmlHeader
                + "<v:shapetype id=\"_x0000_t75\" coordsize=\"21600,21600\" o:spt=\"75\" o:preferrelative=\"t\" path=\"m@4@5l@4@11@9@11@9@5xe\" filled=\"f\" stroked=\"f\">"
                + "<v:stroke joinstyle=\"miter\"/><v:formulas><v:f eqn=\"if lineDrawn pixelLineWidth 0\"/></v:formulas>"
                + "<v:path o:extrusionok=\"f\" gradientshapeok=\"t\" o:connecttype=\"rect\"/><o:lock v:ext=\"edit\" aspectratio=\"t\"/></v:shapetype>"
                + "<v:shape id=\"LH\" o:spid=\"_x0000_s1025\" type=\"#_x0000_t75\" style=\"position:absolute;width:100pt;height:50pt\">"
                + "<v:imagedata o:relid=\"rId1\" o:title=\"logo\"/><o:lock v:ext=\"edit\" rotation=\"t\"/></v:shape></xml>";

            XSSFVMLDrawing vml;
            XmlNamespaceManager nsmgr;
            XmlDocument doc = RoundTrip(xml, out vml, out nsmgr);

            XmlElement shapetype = (XmlElement)doc.SelectSingleNode("//v:shapetype", nsmgr);
            Assert.AreEqual("_x0000_t75", shapetype.GetAttribute("id"));
            Assert.AreEqual("75", shapetype.GetAttribute("spt", "urn:schemas-microsoft-com:office:office"));
            Assert.AreEqual("f", shapetype.GetAttribute("filled"));
            Assert.AreEqual("f", shapetype.GetAttribute("stroked"));
            Assert.AreEqual("f", ((XmlElement)shapetype.SelectSingleNode("v:path", nsmgr)).GetAttribute("extrusionok", "urn:schemas-microsoft-com:office:office"));
            // Excel writes formulas before path
            Assert.AreEqual("formulas", shapetype.ChildNodes[1].LocalName);
            Assert.AreEqual("path", shapetype.ChildNodes[2].LocalName);

            XmlElement shape = (XmlElement)doc.SelectSingleNode("//v:shape", nsmgr);
            Assert.IsFalse(shape.HasAttribute("insetmode", "urn:schemas-microsoft-com:office:office"), "o:insetmode must not be invented");
            AssertEveryShapeTypeResolves(doc, nsmgr);
        }

        [Test]
        public void TestFormControlsSurviveRoundTrip()
        {
            string xml = VmlHeader + ButtonShapetype
                + "<v:shape id=\"_x0000_s1026\" type=\"#_x0000_t201\" style=\"position:absolute\" o:button=\"t\" fillcolor=\"buttonFace [67]\" strokecolor=\"windowText [64]\" o:insetmode=\"auto\">"
                + "<v:fill color2=\"buttonFace [67]\" o:detectmouseclick=\"t\"/>"
                + "<o:lock v:ext=\"edit\" rotation=\"t\"/>"
                + "<v:textbox style=\"mso-direction-alt:auto\" o:singleclick=\"f\"><div style=\"text-align:center\">Run</div></v:textbox>"
                + "<x:ClientData ObjectType=\"Button\"><x:Anchor>5, 0, 1, 0, 6, 32, 2, 12</x:Anchor><x:PrintObject>False</x:PrintObject>"
                + "<x:AutoFill>False</x:AutoFill><x:FmlaMacro>[0]!RunMe</x:FmlaMacro><x:TextHAlign>Center</x:TextHAlign></x:ClientData>"
                + "</v:shape>"
                + "<v:shape id=\"_x0000_s1027\" type=\"#_x0000_t201\" style=\"position:absolute\" filled=\"f\" fillcolor=\"window [65]\" stroked=\"f\" strokecolor=\"windowText [64]\" o:insetmode=\"auto\">"
                + "<x:ClientData ObjectType=\"Checkbox\"><x:Anchor>5, 0, 3, 0, 6, 32, 4, 4</x:Anchor><x:FmlaLink>$H$1</x:FmlaLink><x:Checked>1</x:Checked><x:NoThreeD/></x:ClientData>"
                + "</v:shape>"
                + "<v:shape id=\"_x0000_s1028\" type=\"#_x0000_t201\" style=\"position:absolute\" stroked=\"f\" strokecolor=\"windowText [64]\" o:insetmode=\"auto\">"
                + "<x:ClientData ObjectType=\"Drop\"><x:Anchor>5, 0, 5, 0, 6, 32, 6, 4</x:Anchor><x:FmlaRange>$J$1:$J$5</x:FmlaRange><x:Sel>2</x:Sel><x:DropStyle>Combo</x:DropStyle></x:ClientData>"
                + "</v:shape></xml>";

            XSSFVMLDrawing vml;
            XmlNamespaceManager nsmgr;
            XmlDocument doc = RoundTrip(xml, out vml, out nsmgr);
            AssertEveryShapeTypeResolves(doc, nsmgr);

            XmlElement path = (XmlElement)doc.SelectSingleNode("//v:shapetype/v:path", nsmgr);
            Assert.AreEqual("f", path.GetAttribute("shadowok"));
            Assert.AreEqual("f", path.GetAttribute("strokeok"));
            Assert.AreEqual("f", path.GetAttribute("fillok"));
            Assert.AreEqual("f", path.GetAttribute("extrusionok", "urn:schemas-microsoft-com:office:office"));

            XmlElement button = (XmlElement)doc.SelectSingleNode("//v:shape[@id='_x0000_s1026']", nsmgr);
            Assert.AreEqual("t", button.GetAttribute("button", "urn:schemas-microsoft-com:office:office"));
            Assert.AreEqual("windowText [64]", button.GetAttribute("strokecolor"));
            Assert.AreEqual("t", ((XmlElement)button.SelectSingleNode("v:fill", nsmgr)).GetAttribute("detectmouseclick", "urn:schemas-microsoft-com:office:office"));
            Assert.AreEqual("f", ((XmlElement)button.SelectSingleNode("v:textbox", nsmgr)).GetAttribute("singleclick", "urn:schemas-microsoft-com:office:office"));
            Assert.AreEqual("[0]!RunMe", button.SelectSingleNode("x:ClientData/x:FmlaMacro", nsmgr).InnerText);
            Assert.AreEqual("False", button.SelectSingleNode("x:ClientData/x:PrintObject", nsmgr).InnerText);
            Assert.AreEqual("Center", button.SelectSingleNode("x:ClientData/x:TextHAlign", nsmgr).InnerText);

            XmlElement checkbox = (XmlElement)doc.SelectSingleNode("//v:shape[@id='_x0000_s1027']", nsmgr);
            Assert.AreEqual("f", checkbox.GetAttribute("filled"));
            Assert.AreEqual("f", checkbox.GetAttribute("stroked"));
            Assert.AreEqual("$H$1", checkbox.SelectSingleNode("x:ClientData/x:FmlaLink", nsmgr).InnerText);
            Assert.AreEqual("1", checkbox.SelectSingleNode("x:ClientData/x:Checked", nsmgr).InnerText);
            Assert.IsNotNull(checkbox.SelectSingleNode("x:ClientData/x:NoThreeD", nsmgr));

            XmlElement drop = (XmlElement)doc.SelectSingleNode("//v:shape[@id='_x0000_s1028']", nsmgr);
            Assert.AreEqual("$J$1:$J$5", drop.SelectSingleNode("x:ClientData/x:FmlaRange", nsmgr).InnerText);
            Assert.AreEqual("2", drop.SelectSingleNode("x:ClientData/x:Sel", nsmgr).InnerText);
            Assert.AreEqual("Combo", drop.SelectSingleNode("x:ClientData/x:DropStyle", nsmgr).InnerText);

            // a second round-trip is stable
            XmlNamespaceManager nsmgr2;
            XSSFVMLDrawing vml2;
            XmlDocument doc2 = RoundTrip(doc.OuterXml, out vml2, out nsmgr2);
            Assert.AreEqual(doc.OuterXml, doc2.OuterXml);
        }

        [Test]
        public void TestEmptyClientDataFlagsMeanTrue()
        {
            // Excel writes <x:MoveWithCells/>; an empty ST_TrueFalseBlank element means True
            string xml = VmlHeader + CommentShapetype
                + "<v:shape id=\"_x0000_s1025\" type=\"#_x0000_t202\" style=\"visibility:hidden\">"
                + "<x:ClientData ObjectType=\"Note\"><x:MoveWithCells/><x:SizeWithCells/><x:Anchor>1, 15, 0, 2, 3, 15, 4, 4</x:Anchor>"
                + "<x:Row>0</x:Row><x:Column>0</x:Column><x:Visible/></x:ClientData></v:shape></xml>";

            XSSFVMLDrawing vml;
            XmlNamespaceManager nsmgr;
            XmlDocument doc = RoundTrip(xml, out vml, out nsmgr);

            CT_ClientData clientData = vml.FindCommentShape(0, 0).GetClientDataArray(0);
            Assert.AreEqual(ST_TrueFalseBlank.@true, clientData.moveWithCells);
            Assert.AreEqual(ST_TrueFalseBlank.@true, clientData.sizeWithCells);
            Assert.AreEqual(ST_TrueFalseBlank.@true, clientData.visible);
            Assert.IsNotNull(doc.SelectSingleNode("//x:ClientData/x:MoveWithCells", nsmgr));
            Assert.IsNotNull(doc.SelectSingleNode("//x:ClientData/x:SizeWithCells", nsmgr));
            Assert.IsNotNull(doc.SelectSingleNode("//x:ClientData/x:Visible", nsmgr));
        }

        [Test]
        public void TestNewCommentInDrawingWithoutCommentShapetype()
        {
            // a drawing holding only a form button and a header picture: the new comment must not reuse the
            // button's shapetype, must get a comment shapetype of its own, and must not reuse an o:spid
            string xml = VmlHeader + ButtonShapetype
                + "<v:shape id=\"_x0000_s1026\" type=\"#_x0000_t201\" style=\"position:absolute\" o:button=\"t\">"
                + "<x:ClientData ObjectType=\"Button\"><x:Anchor>5, 0, 1, 0, 6, 32, 2, 12</x:Anchor><x:FmlaMacro>[0]!RunMe</x:FmlaMacro></x:ClientData></v:shape>"
                + "<v:shape id=\"LH\" o:spid=\"_x0000_s1030\" type=\"#_x0000_t201\" style=\"position:absolute\"/>"
                + "</xml>";

            XSSFVMLDrawing vml = new XSSFVMLDrawing();
            vml.Read(new MemoryStream(Encoding.UTF8.GetBytes(xml)));

            CT_Shape comment = vml.newCommentShape();
            Assert.AreEqual("#_x0000_t202", comment.type);
            Assert.AreEqual("_x0000_s1031", comment.id);

            XmlNamespaceManager nsmgr;
            XmlDocument doc = Write(vml, out nsmgr);
            XmlElement commentShapetype = (XmlElement)doc.SelectSingleNode("//v:shapetype[@id='_x0000_t202']", nsmgr);
            Assert.IsNotNull(commentShapetype);
            Assert.AreEqual("202", commentShapetype.GetAttribute("spt", "urn:schemas-microsoft-com:office:office"));
            AssertEveryShapeTypeResolves(doc, nsmgr);
        }

        [Test]
        public void TestNewCommentUsesCommentShapetypeWhenButtonShapetypeComesLast()
        {
            string xml = VmlHeader + CommentShapetype + ButtonShapetype + "</xml>";

            XSSFVMLDrawing vml = new XSSFVMLDrawing();
            vml.Read(new MemoryStream(Encoding.UTF8.GetBytes(xml)));

            Assert.AreEqual("#_x0000_t202", vml.newCommentShape().type);
        }
    }
}
