using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.epam.indigo;

namespace Sds.Imaging.Rasterizers
{
    public class IndigoAdapter
    {
        public static string[] SupportedFiles = { ".mol", ".rxn" };

        private Indigo GetIndigo()
        {
            var indigo = new Indigo();
            indigo.setOption("ignore-stereochemistry-errors", true);
            indigo.setOption("ignore-noncritical-query-features", true);
            return indigo;
        }

        private IndigoRenderer GetRenderer(Indigo indigo, string format = "png", int width = -1, int height = -1)
        {
            var renderer = new IndigoRenderer(indigo);
            indigo.setOption("render-output-format", format);
            indigo.setOption("render-stereo-style", "ext");
            indigo.setOption("render-margins", 5, 5);
            indigo.setOption("render-coloring", true);
            indigo.setOption("render-relative-thickness", "1.5");
            indigo.setOption("render-image-size", width, height);
            return renderer;
        }

        public byte[] Mol2Image(string mol, string format = "png", int width = -1, int height = -1)
        {
            //_indigo.setOption("render-image-size", width, height);

            using (var indigo = GetIndigo())
            {
                IndigoObject indogoObject = indigo.loadMolecule(mol);
                var renderer = GetRenderer(indigo, format, width, height);
                return renderer.renderToBuffer(indogoObject);
            }
        }

        public byte[] Rxn2Image(string rxn, string format = "png", int width = -1, int height = -1)
        {
            //_indigo.setOption("render-image-size", width, height);

            using (var indigo = GetIndigo())
            {
                IndigoObject indogoObject = indigo.loadReaction(rxn);
                var renderer = GetRenderer(indigo, format, width, height);
                return renderer.renderToBuffer(indogoObject);
            }
        }
    }
}
