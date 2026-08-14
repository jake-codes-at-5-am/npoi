namespace NPOI.XSSF.EventUserModel
{
    /// <summary>
    /// Classifies a streamed cell's resolved VALUE. Formula-ness is reported
    /// separately (see <see cref="SheetContentsHandler.Cell"/>'s isFormula flag),
    /// so a formula cell still surfaces its underlying value type here.
    /// </summary>
    public enum StreamValueKind
    {
        Blank, Number, String, Boolean, Date, Error
    }

    /// <summary>
    /// Callback surface for a single-sheet streaming parse. Mirrors Apache POI's
    /// XSSFSheetXMLHandler.SheetContentsHandler, extended to carry the raw typed value.
    /// </summary>
    public interface SheetContentsHandler
    {
        void StartRow(int rowNum);
        void EndRow(int rowNum);
        void Cell(string cellReference, string formattedValue, object rawValue, StreamValueKind kind, bool isFormula);
        void HeaderFooter(string text, bool isHeader, string tagName);
    }
}
