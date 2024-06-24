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

            // Create temporary ZIP files using SharpZipLib
            string tempOriginalZipPath = CreateTempZipFile(originalBytes);
            string tempModifiedZipPath = CreateTempZipFile(modifiedBytes);

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

    static string CreateTempZipFile(byte[] documentBytes)
    {
        string tempZipPath = Path.GetTempFileName();
        using (ZipOutputStream zipOutputStream = new ZipOutputStream(File.Create(tempZipPath)))
        {
            zipOutputStream.SetLevel(9); // 9 = Best Compression

            // Create the necessary entries for a valid Word document package
            ZipEntry relsEntry = new ZipEntry("_rels/.rels");
            zipOutputStream.PutNextEntry(relsEntry);
            zipOutputStream.Write(new byte[0], 0, 0);
            zipOutputStream.CloseEntry();

            ZipEntry documentEntry = new ZipEntry("word/document.xml");
            zipOutputStream.PutNextEntry(documentEntry);
            zipOutputStream.Write(documentBytes, 0, documentBytes.Length);
            zipOutputStream.CloseEntry();

            zipOutputStream.Close();
        }
        return tempZipPath;
    }
}
