using System;
using System.IO;
using OpenXmlPowerTools;
using DocumentFormat.OpenXml.Packaging;
using ICSharpCode.SharpZipLib.Zip;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: redlines <author_tag> <original_path.docx> <modified_path.docx> <redline_path.docx>");
            return;
        }

        string authorTag = args[0];
        string originalFilePath = args[1];
        string modifiedFilePath = args[2];
        string outputFilePath = args[3];

        if (!File.Exists(originalFilePath) || !File.Exists(modifiedFilePath))
        {
            Console.WriteLine("Error: One or both files do not exist.");
            return;
        }

        try
        {
            var originalBytes = File.ReadAllBytes(originalFilePath);
            var modifiedBytes = File.ReadAllBytes(modifiedFilePath);

            // Use SharpZipLib to create a temporary ZIP file for the original document
            string tempOriginalZipPath = Path.GetTempFileName();
            using (ZipOutputStream zipOutputStream = new ZipOutputStream(File.Create(tempOriginalZipPath)))
            {
                zipOutputStream.SetLevel(9); // 9 = Best Compression
                ZipEntry zipEntry = new ZipEntry("document.xml");
                zipOutputStream.PutNextEntry(zipEntry);
                zipOutputStream.Write(originalBytes, 0, originalBytes.Length);
                zipOutputStream.CloseEntry();
                zipOutputStream.Close();
            }

            // Use SharpZipLib to create a temporary ZIP file for the modified document
            string tempModifiedZipPath = Path.GetTempFileName();
            using (ZipOutputStream zipOutputStream = new ZipOutputStream(File.Create(tempModifiedZipPath)))
            {
                zipOutputStream.SetLevel(9); // 9 = Best Compression
                ZipEntry zipEntry = new ZipEntry("document.xml");
                zipOutputStream.PutNextEntry(zipEntry);
                zipOutputStream.Write(modifiedBytes, 0, modifiedBytes.Length);
                zipOutputStream.CloseEntry();
                zipOutputStream.Close();
            }

            var originalDocument = new WmlDocument(tempOriginalZipPath);
            var modifiedDocument = new WmlDocument(tempModifiedZipPath);

            var comparisonSettings = new WmlComparerSettings
            {
                AuthorForRevisions = authorTag,
                DetailThreshold = 0
            };

            var comparisonResults = WmlComparer.Compare(originalDocument, modifiedDocument, comparisonSettings);
            var revisions = WmlComparer.GetRevisions(comparisonResults, comparisonSettings);

            // Output results
            Console.WriteLine($"Revisions found: {revisions.Count}");

            File.WriteAllBytes(outputFilePath, comparisonResults.DocumentByteArray);

            // Clean up temporary files
            File.Delete(tempOriginalZipPath);
            File.Delete(tempModifiedZipPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("Detailed Stack Trace:");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
