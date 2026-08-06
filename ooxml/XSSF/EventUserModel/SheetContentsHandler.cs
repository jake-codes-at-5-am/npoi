namespace NPOI.XSSF.EventUserModel
{
    /// <summary>Classifies a streamed cell's resolved value.</summary>
    public enum StreamValueKind
    {
        Blank, Number, String, Boolean, Date, Formula, Error
    }

    /// <summary>
    /// Callback surface for a single-sheet streaming parse. Mirrors Apache POI's
    /// XSSFSheetXMLHandler.SheetContentsHandler, extended to carry the raw typed value.
    /// </summary>
    public interface SheetContentsHandler
    {
        void StartRow(int rowNum);
        void EndRow(int rowNum);
        void Cell(string cellReference, string formattedValue, object rawValue, StreamValueKind kind);
        void HeaderFooter(string text, bool isHeader, string tagName);
    }
}
