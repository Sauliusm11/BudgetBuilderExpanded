using BudgetBuilder.Domain.Data.Entities;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Pdf;
using PdfSharp.Quality;

namespace BudgetBuilder.Domain.Reports
{
    public class ReportGenerator
    {
        public static string GenerateSimplePurchasesPdf(List<Purchase> purchases)
        {
            // Create a new MigraDoc document.
            var document = new Document();

            /// Add a section to the document.
            var section = document.AddSection();

            // Add table.
            var table = section.AddTable();
            table.Borders.Visible = true;

            // Add first column.
            var columnA = table.AddColumn(Unit.FromCentimeter(4));

            // Add second column.
            var columnB = table.AddColumn(Unit.FromCentimeter(3));

            var columnC = table.AddColumn(Unit.FromCentimeter(3));

            // Add first row as header.
            var row1 = table.AddRow();
            row1.HeadingFormat = true;

            // Add paragraph to first cell of row1.
            row1[0].AddParagraph("Purchase");

            // Add paragraph to second cell of row1.
            row1[1].AddParagraph("Amount");

            // Add paragraph to second cell of row1.
            row1[2].AddParagraph("Cost per unit");



            for (int i = 0; i < purchases.Count; i++)
            {
                // Add second row.
                var newRow = table.AddRow();


                Purchase purchase = purchases[i];
                if (purchase != null)
                {
                    // Add paragraph to first cell of the row.
                    var cellA1 = newRow[0];
                    cellA1.AddParagraph(purchase.Name);

                    // Add paragraph to second cell of the row.
                    var cellB1 = newRow[1];
                    cellB1.AddParagraph(purchase.Amount.ToString());

                    // Add paragraph to third cell of the row.
                    var cellC1 = newRow[2];
                    cellC1.AddParagraph(purchase.Cost.ToString());
                }
            }

            var style = document.Styles[StyleNames.Normal]!;
            style.Font.Name = "Arial";

            // Create a renderer for the MigraDoc document.
            var pdfRenderer = new PdfDocumentRenderer
            {
                // Associate the MigraDoc document with a renderer.
                Document = document,
                PdfDocument =
                {
                    // Change some settings before rendering the MigraDoc document.
                    PageLayout = PdfPageLayout.SinglePage,
                    ViewerPreferences =
                    {
                        FitWindow = true
                    }
                }
            };

            // Layout and render document to PDF.
            pdfRenderer.RenderDocument();

            // Save the document...
            string filename = PdfFileUtility.GetTempPdfFullFileName("HelloWorld");
            pdfRenderer.PdfDocument.Save(filename);

            Byte[] fileBytes = File.ReadAllBytes(filename);
            File.Delete(filename);
            string content = Convert.ToBase64String(fileBytes);
            return content;
        }
    }
}
