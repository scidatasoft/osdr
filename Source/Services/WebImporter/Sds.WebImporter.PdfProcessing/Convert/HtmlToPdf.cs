using Serilog;
using System;
using System.IO;

namespace Sds.WebImporter.PdfProcessing.Convert
{
    public static class HtmlToPdf
    {
        public static byte[] GetPdfAsByteArray(string url)
        {
            var pdfHtmlToPdfExePath = Path.Combine(Directory.GetCurrentDirectory(), "wkhtmltopdf", "wkhtmltopdf.exe");

            string outputFilename = Path.GetTempFileName();

            var p = new System.Diagnostics.Process()
            {
                StartInfo =
                    {
                        FileName = pdfHtmlToPdfExePath,
                        Arguments = url + " " + outputFilename,
                    }
            };

            Log.Information($"Working with exe from {pdfHtmlToPdfExePath}...");
            p.Start();

            while (!p.HasExited)
            {
                p.WaitForExit(2000);
            }

            if (p.ExitCode != 0)
            {
                Log.Information("Exited with error.");
                throw new Exception($"Pdf can not be generated from url : {url}. Exit code: {p.ExitCode}, queried path: {pdfHtmlToPdfExePath}");
            }

            var pdfBytes = File.ReadAllBytes(outputFilename);

            File.Delete(outputFilename);

            return pdfBytes;
        }
    }
}
