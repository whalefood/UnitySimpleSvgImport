using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace SimpleSvgImport
{

    [ScriptedImporter(1, "svg")]
    public class SvgImporter : ScriptedImporter
    {
        [Range(0.0001f, 0.5f)]
        public float Scale = 1;

        public bool CenterElements = true;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var dataFile = ScriptableObject.CreateInstance<SvgScriptable>();
            XmlDocument svgFile = new XmlDocument();
            svgFile.LoadXml(File.ReadAllText(ctx.assetPath));
            var root = svgFile.GetElementsByTagName("svg")[0];
            LoadSvgDataIntoScriptable(dataFile, root);
            dataFile.Scale(new Vector2(1, -1) * this.Scale);

            if (dataFile.SvgData.Count > 0 && CenterElements)
            {
                Vector2 maxCorner = dataFile.SvgData[0].Points[0];
                Vector2 minCorner = maxCorner;
                foreach (var pt in dataFile.AllPoints)
                {
                    maxCorner.x = Mathf.Max(maxCorner.x, pt.x);
                    maxCorner.y = Mathf.Max(maxCorner.y, pt.y);

                    minCorner.x = Mathf.Min(minCorner.x, pt.x);
                    minCorner.y = Mathf.Min(minCorner.y, pt.y);
                }
                dataFile.Move((minCorner + maxCorner) / -2);
            }


            ctx.AddObjectToAsset("main obj", dataFile);
            ctx.SetMainObject(dataFile);

        }


        public void LoadFillAndStrode(SvgElement svgElement, XmlElement xmlElement)
        {
            if (xmlElement.HasAttribute("fill"))
            {
                var colorCode = xmlElement.GetAttribute("fill");
                if (colorCode != "none" && ColorUtility.TryParseHtmlString(colorCode, out Color color))
                {
                    svgElement.FillColor = color;
                    svgElement.HasFill = true;
                }
            }

            if (xmlElement.HasAttribute("stroke"))
            {
                var colorCode = xmlElement.GetAttribute("stroke");
                if (colorCode != "none" && ColorUtility.TryParseHtmlString(colorCode, out Color color))
                {
                    svgElement.StrokeColor = color;
                    svgElement.HasStroke = true;
                }
            }
        }

        public IEnumerable<Vector2> ExtractLinePoints(XmlElement xmlElement)
        {
            float x = float.Parse(xmlElement.GetAttribute("x1"));
            float y = float.Parse(xmlElement.GetAttribute("y1"));
            yield return new Vector2(x, y);

            x = float.Parse(xmlElement.GetAttribute("x2"));
            y = float.Parse(xmlElement.GetAttribute("y2"));
            yield return new Vector2(x, y);
        }

        public IEnumerable<Vector2> ExtractPolyPoints(XmlElement xmlElement)
        {
            foreach (var pntStr in xmlElement.GetAttribute("points").Split())
            {
                if (string.IsNullOrEmpty(pntStr))
                {
                    continue;
                }
                float x = float.Parse(pntStr.Split(',')[0]);
                float y = float.Parse(pntStr.Split(',')[1]);
                yield return new Vector2(x, y);
            }
        }

        public IEnumerable<Vector2> ExtractRectPoints(XmlElement xmlElement)
        {
            float startX = float.Parse(xmlElement.GetAttribute("x"));
            float startY = float.Parse(xmlElement.GetAttribute("y"));

            float width = float.Parse(xmlElement.GetAttribute("width"));
            float height = float.Parse(xmlElement.GetAttribute("height"));

            yield return new Vector2(startX, startY);
            yield return new Vector2(startX + width, startY);
            yield return new Vector2(startX + width, startY + height);
            yield return new Vector2(startX, startY + height);
        }



        private void LoadSvgDataIntoScriptable(SvgScriptable svgObj, XmlNode node)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (!(childNode is XmlElement))
                {
                    continue;
                }

                var xmlElement = childNode as XmlElement;

                // if group, flatten it
                if (xmlElement.Name == "g")
                {
                    LoadSvgDataIntoScriptable(svgObj, childNode);
                    continue;
                }

                SvgElement svgElement = new SvgElement();
                LoadFillAndStrode(svgElement, xmlElement);
                switch (xmlElement.Name)
                {
                    case "line":
                        {
                            svgElement.Points = ExtractLinePoints(xmlElement).ToList();
                            break;
                        }
                    case "polyline":
                        {
                            svgElement.Points = ExtractPolyPoints(xmlElement).ToList();
                            break;
                        }
                    case "polygon":
                        {
                            svgElement.Points = ExtractPolyPoints(xmlElement).ToList();
                            svgElement.IsClosed = true;
                            break;
                        }
                    case "rect":
                        {
                            svgElement.Points = ExtractRectPoints(xmlElement).ToList();
                            svgElement.IsClosed = true;
                            break;
                        }
                }

                if (svgElement.Points.Count == 0)
                {
                    continue;
                }

                //matrix mult
                if (xmlElement.HasAttribute("transform"))
                {
                    var mtxStr = xmlElement.GetAttribute("transform");
                    if (mtxStr.StartsWith("matrix"))
                    {
                        mtxStr = mtxStr.Substring(7, mtxStr.Length - 8);
                        var coords = mtxStr.Split().Select(p => float.Parse(p)).ToArray();
                        for (int ptInx = 0; ptInx < svgElement.Points.Count; ptInx++)
                        {
                            var oldPt = svgElement.Points[ptInx];
                            var newX = coords[0] * oldPt.x + coords[2] * oldPt.y + coords[4];
                            var newY = coords[1] * oldPt.x + coords[3] * oldPt.y + coords[5];
                            svgElement.Points[ptInx] = new Vector2(newX, newY);
                        }
                    }
                }

                svgObj.SvgData.Add(svgElement);
            }
        }

    }
}