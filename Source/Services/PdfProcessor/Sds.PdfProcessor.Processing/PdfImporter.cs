using org.apache.pdfbox.pdmodel;
using org.apache.pdfbox.text;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace Sds.PdfProcessor.Processing
{
    public static class PdfImporter
    {

        public static string GetText(Stream input, string fileName)
        {
            byte[] inputBytes = new byte[input.Length];

            input.Read(inputBytes, 0, inputBytes.Length);

            return parseUsingPDFBox(inputBytes);
        }

        public static Dictionary<string, byte[]> GetImagesAsBytes(Stream input, string fileName)
        {
            byte[] inputBytes = new byte[input.Length];

            input.Read(inputBytes, 0, inputBytes.Length);

            var images = new Dictionary<string, byte[]>();            

            var stream = new MemoryStream(inputBytes);

            PdfDocument document = PdfReader.Open(stream, PdfDocumentOpenMode.ReadOnly);

            int allImages = 0;

            foreach (PdfPage page in document.Pages)
            {
                PdfDictionary resources = page.Elements.GetDictionary("/Resources");

                if (resources != null)
                {
                    PdfDictionary xObjects = resources.Elements.GetDictionary("/XObject");
                    if (xObjects != null)
                    {
                        ICollection<PdfItem> items = xObjects.Elements.Values;

                        foreach (PdfItem item in items)
                        {
                            PdfReference reference = item as PdfReference;

                            if (reference != null)
                            {
                                PdfDictionary xObject = reference.Value as PdfDictionary;

                                if (xObject != null && xObject.Elements.GetString("/Subtype") == "/Image")
                                {
                                    images.Add($"{Guid.NewGuid()}", ExportImage(xObject));

                                    allImages++;
                                }
                            }
                        }
                    }
                }
            }
            return images;
        }

        private static string parseUsingPDFBox(byte[] inputBytes)
        {
            PDDocument doc = null;

            try
            {
                doc = PDDocument.load(inputBytes);
                PDFTextStripper stripper = new PDFTextStripper();
                var res = stripper.getText(doc);
                return res;
            }
            finally
            {
                if (doc != null)
                {
                    doc.close();
                }
            }
        }


        private static byte[] ExportImage(PdfDictionary image)
        {
            string filter = image.Elements.GetName("/Filter");
            byte[] resultBytes = null;
            switch (filter)
            {
                case "/DCTDecode":
                    resultBytes = ExportJpegImage(image);
                    break;

                    //case "/FlateDecode":
                    //    ExportAsPngImage(image, count);
                    //    break;

                    //case "/CCITTFaxDecode":
                    //    ExportAsTifImage(image, count);
                    //    break;
            }
            return resultBytes;
        }

        private static byte[] ExportJpegImage(PdfDictionary image)
        {
            byte[] stream = image.Stream.Value;

            return stream;
            //FileStream fs = new FileStream(String.Format("Image{0}.jpeg", count), FileMode.Create, FileAccess.Write);

            //BinaryWriter bw = new BinaryWriter(fs);

            //bw.Write(stream);

            //bw.Close();
        }


        //static void ExportAsPngImage(PdfSharp.Pdf.PdfDictionary image, int count)
        //{
        //    byte[] bytes = image.Stream.Value;

        //    int width = image.Elements.GetInteger(PdfSharp.Pdf.Advanced.PdfImage.Keys.Width);

        //    int height = image.Elements.GetInteger(PdfSharp.Pdf.Advanced.PdfImage.Keys.Height);

        //    int bitsPerComponent = image.Elements.GetInteger(PdfSharp.Pdf.Advanced.PdfImage.Keys.BitsPerComponent);

        //    bytes = iTextSharp.text.pdf.PdfReader.FlateDecode(bytes, true);


        //    System.Drawing.Imaging.PixelFormat pixelFormat = System.Drawing.Imaging.PixelFormat.Format1bppIndexed;



        //    switch (bitsPerComponent)

        //    {

        //        case 1:

        //            pixelFormat = System.Drawing.Imaging.PixelFormat.Format1bppIndexed;

        //            break;



        //        case 8:

        //            pixelFormat = System.Drawing.Imaging.PixelFormat.Format8bppIndexed;

        //            break;



        //        case 24:

        //            pixelFormat = System.Drawing.Imaging.PixelFormat.Format24bppRgb;

        //            break;



        //        default:

        //            Console.WriteLine("Unknown pixel format " + bitsPerComponent);

        //            break;



        //    }



        //    using (Bitmap bmp = new Bitmap(width, height, pixelFormat))
        //    {
        //        try
        //        {
        //            BitmapData bmd =

        //                bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height),

        //                ImageLockMode.WriteOnly, pixelFormat);

        //            int offset = 0;
        //            //long ptr = bmd.Scan0.ToInt64();
        //            for (int i = 0; i < height; i++)
        //            {
        //                Marshal.Copy(bytes, offset, bmd.Scan0, width * 3);
        //                offset += width * 3;
        //            }

        //            bmp.UnlockBits(bmd);



        //            string sb = $"Image{count}.png";



        //            //bmp.Save(sb, System.Drawing.Imaging.ImageFormat.Png);

        //            FileStream fs = new FileStream(String.Format("Image{0}.png", count), FileMode.Create, FileAccess.Write);

        //            BinaryWriter bw = new BinaryWriter(fs);

        //            bw.Write(bytes);


        //        }

        //        catch (Exception ex)

        //        {

        //            Console.WriteLine(ex.Message);

        //        }

        //    }

        //}

        //static void ExportAsTifImage(PdfSharp.Pdf.PdfDictionary image, int count)
        //{
        //    byte[] bytes = image.Stream.Value;

        //    int width = image.Elements.GetInteger(PdfSharp.Pdf.Advanced.PdfImage.Keys.Width);

        //    int height = image.Elements.GetInteger(PdfSharp.Pdf.Advanced.PdfImage.Keys.Height);

        //    int bitsPerComponent = image.Elements.GetInteger(PdfSharp.Pdf.Advanced.PdfImage.Keys.BitsPerComponent);

        //    bytes = iTextSharp.text.pdf.PdfReader.FlateDecode(bytes, true);

        //    System.Drawing.Imaging.PixelFormat pixelFormat = System.Drawing.Imaging.PixelFormat.Format1bppIndexed;
        //    switch (bitsPerComponent)

        //    {

        //        case 1:

        //            pixelFormat = System.Drawing.Imaging.PixelFormat.Format1bppIndexed;

        //            break;



        //        case 8:

        //            pixelFormat = System.Drawing.Imaging.PixelFormat.Format8bppIndexed;

        //            break;



        //        case 24:

        //            pixelFormat = System.Drawing.Imaging.PixelFormat.Format24bppRgb;

        //            break;



        //        default:

        //            Console.WriteLine("Unknown pixel format " + bitsPerComponent);

        //            break;



        //    }



        //    using (Bitmap bmp = new Bitmap(width, height, pixelFormat))
        //    {
        //        try
        //        {
        //            BitmapData bmd =

        //                bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height),

        //                ImageLockMode.WriteOnly, pixelFormat);

        //            //int offset = 0;
        //            ////long ptr = bmd.Scan0.ToInt64();
        //            //for (int i = 0; i < height; i++)
        //            //{
        //            //    Marshal.Copy(bytes, offset, bmd.Scan0, width * 3);
        //            //    offset += width * 3;
        //            //}

        //            bmd.Scan0 = new IntPtr(BitConverter.ToInt64(bytes, 0));

        //            bmp.UnlockBits(bmd);



        //            string sb = $"Image{count}.tiff";



        //            bmp.Save(sb, System.Drawing.Imaging.ImageFormat.Tiff);

        //            FileStream fs = new FileStream(String.Format("Image{0}.tiff", count), FileMode.Create, FileAccess.Write);

        //            BinaryWriter bw = new BinaryWriter(fs);

        //            bw.Write(bytes);

        //        }

        //        catch (Exception ex)

        //        {

        //            Console.WriteLine(ex.Message);

        //        }

        //    }
        //}
    }
}
