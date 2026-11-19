using System.Collections.Generic;
using System.Xml.Linq;
using POKE.PMDCollab;

namespace POKE.PMDCollab.Editor
{
    public class PMDParsedAnimData
    {
        public int shadowSize = 1;
        public List<PMDAnimSequence> sequences = new List<PMDAnimSequence>();
    }

    public static class PMDAnimDataParser
    {
        public static PMDParsedAnimData ParseXml(string xmlContent)
        {
            var result = new PMDParsedAnimData();
            if (string.IsNullOrWhiteSpace(xmlContent))
                return result;

            XDocument doc = XDocument.Parse(xmlContent);
            XElement root = doc.Element("AnimData");
            if (root == null)
                return result;

            result.shadowSize = ParseInt(root.Element("ShadowSize"), 1);

            XElement animsElement = root.Element("Anims");
            if (animsElement == null)
                return result;

            foreach (XElement animEl in animsElement.Elements("Anim"))
            {
                var seq = new PMDAnimSequence
                {
                    name = animEl.Element("Name")?.Value?.Trim() ?? string.Empty,
                    index = ParseInt(animEl.Element("Index"), -1),
                    copyOf = animEl.Element("CopyOf")?.Value?.Trim() ?? string.Empty,
                    frameWidth = ParseInt(animEl.Element("FrameWidth"), 0),
                    frameHeight = ParseInt(animEl.Element("FrameHeight"), 0),
                    rushFrame = ParseInt(animEl.Element("RushFrame"), -1),
                    hitFrame = ParseInt(animEl.Element("HitFrame"), -1),
                    returnFrame = ParseInt(animEl.Element("ReturnFrame"), -1)
                };

                XElement durationsEl = animEl.Element("Durations");
                if (durationsEl != null)
                {
                    foreach (XElement durEl in durationsEl.Elements("Duration"))
                    {
                        seq.durations.Add(ParseInt(durEl, 4));
                    }
                }

                if (!string.IsNullOrEmpty(seq.name))
                {
                    result.sequences.Add(seq);
                }
            }

            return result;
        }

        private static int ParseInt(XElement el, int fallback)
        {
            if (el == null)
                return fallback;

            return int.TryParse(el.Value.Trim(), out int val) ? val : fallback;
        }
    }
}

