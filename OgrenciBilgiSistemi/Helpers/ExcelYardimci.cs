using ClosedXML.Excel;

namespace OgrenciBilgiSistemi.Helpers
{
    /// <summary>
    /// Rapor Excel çıktılarında ortak başlık ve sayfa sonu biçimlendirmesi.
    /// </summary>
    public static class ExcelYardimci
    {
        public static void BasligiBicimle(IXLRange headerRange)
        {
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        public static void SayfayiSonlandir(IXLWorksheet ws, int colCount, int nextRow, string ozet)
        {
            ws.SheetView.FreezeRows(1);
            ws.Range(1, 1, Math.Max(1, nextRow - 1), colCount).SetAutoFilter();

            int summaryRow = nextRow + 1;
            if (colCount > 1)
                ws.Range(summaryRow, 1, summaryRow, colCount - 1).Merge();
            var totalCell = ws.Cell(summaryRow, colCount);
            totalCell.Value = ozet;
            totalCell.Style.Font.Bold = true;
            totalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            totalCell.Style.Border.TopBorder = XLBorderStyleValues.Thin;

            ws.Columns(1, colCount).AdjustToContents(1, summaryRow);
            foreach (var col in ws.Columns(1, colCount))
                if (col.Width < 12) col.Width = 12;
        }
    }
}
