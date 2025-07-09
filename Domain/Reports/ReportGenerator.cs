using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Quality;

namespace BudgetBuilder.Domain.Reports
{
    public class ReportGenerator
    {
        public static string GenerateSimplePurchasePdf()
        {
            var document = new PdfDocument();

            // Show a single page.
            document.PageLayout = PdfPageLayout.SinglePage;

            // Let the viewer application fit the page in the windows.
            document.ViewerPreferences.FitWindow = true;

            // Set the document title.
            document.Info.Title = "Created with PDFsharp";

            // Create an empty page.
            var page = document.AddPage();

            // Get an XGraphics object for drawing.
            var gfx = XGraphics.FromPdfPage(page);

            // Create a font.
            var font = new XFont("Arial", 20, XFontStyleEx.BoldItalic);

            // Draw the text.
            gfx.DrawString("Hello, World!", font, XBrushes.Black,
                new XRect(0, 0, page.Width.Point, page.Height.Point),
                XStringFormats.Center);

            // Save the document...
            string filename = PdfFileUtility.GetTempPdfFullFileName("HelloWorld");
            document.Save(filename);

            Byte[] fileBytes = File.ReadAllBytes(filename);
            File.Delete(filename);
            string content = Convert.ToBase64String(fileBytes);
            return content;
        }
    }
}
