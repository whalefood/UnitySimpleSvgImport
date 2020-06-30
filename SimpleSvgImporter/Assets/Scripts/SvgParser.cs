using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;

public class SvgParser
{
    static XmlNode SvgStringToXmlRoot(string dataStr)
    {
        XmlDocument svgFile = new XmlDocument();
        svgFile.LoadXml(dataStr);
        return svgFile.GetElementsByTagName("svg")[0];
    }
    
    public static void LoadScriptableFromString(SvgScriptable svgObj, string svgFileContents)
    {
        var root = SvgStringToXmlRoot(svgFileContents);
        LoadSvgDataIntoScriptable(svgObj, root);
    }

    public static SvgScriptable Parse(string svgData)
    {
        SvgScriptable rtnval = new SvgScriptable();
        LoadScriptableFromString(rtnval, svgData);
        return rtnval;
    }


    static void LoadFillAndStrode(SvgElement svgElement, XmlElement xmlElement)
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
        else
        {
            svgElement.FillColor = Color.black;
            svgElement.HasFill = true;
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

    static IEnumerable<Vector2> ExtractLinePoints(XmlElement xmlElement)
    {
        float x = float.Parse(xmlElement.GetAttribute("x1"));
        float y = float.Parse(xmlElement.GetAttribute("y1"));
        yield return new Vector2(x, y);

        x = float.Parse(xmlElement.GetAttribute("x2"));
        y = float.Parse(xmlElement.GetAttribute("y2"));
        yield return new Vector2(x, y);
    }

    static readonly Regex numberSplitter = new Regex("[^\\d.]+", RegexOptions.Compiled);

    static IEnumerable<Vector2> ExtractPolyPoints(XmlElement xmlElement)
    {
        var points = numberSplitter.Split(xmlElement.GetAttribute("points")).Where(n => !string.IsNullOrEmpty(n)).ToArray();
        for (int cntr = 0; cntr < points.Length; cntr += 2)
        {
            float x = float.Parse(points[cntr]);
            float y = float.Parse(points[cntr + 1]);
            yield return new Vector2(x, y);
        }
    }

    static IEnumerable<Vector2> ExtractPathPoints(XmlElement xmlElement)
    {
        var pointStr = xmlElement.GetAttribute("d");
        List<float> numList = new List<float>();
        StringBuilder currentNumber = new StringBuilder();

        bool isRelativeLineTo = false;
        bool horizontalMove = false;
        bool verticalMove = false;

        foreach (char c in pointStr + " ")
        {
            if (char.IsDigit(c) || c == '.')
            {
                currentNumber.Append(c);
                continue;
            }

            if (currentNumber.Length > 0)
            {
                if (verticalMove)
                {
                    // add previous X
                    numList.Add(numList[numList.Count - 2]);
                }

                var newNum = float.Parse(currentNumber.ToString());
                // Debug.Log(newNum);
                if (isRelativeLineTo)
                {
                    newNum += numList[numList.Count - 2];
                }

                numList.Add(newNum);
                currentNumber.Clear();

                if (horizontalMove)
                {
                    // add previous Y
                    numList.Add(numList[numList.Count - 2]);
                }

            }

            if (c == '-')
            {
                currentNumber.Append(c);
            }
            if (c == 'l')
            {
                isRelativeLineTo = true;
                horizontalMove = false;
                verticalMove = false;
            }
            if (c == 'L')
            {
                isRelativeLineTo = false;
                horizontalMove = false;
                verticalMove = false;
            }
            else if (c == 'V')
            {
                isRelativeLineTo = false;
                horizontalMove = false;
                verticalMove = true;
            }
            else if (c == 'v')
            {
                isRelativeLineTo = true;
                horizontalMove = false;
                verticalMove = true;
            }
            else if (c == 'H')
            {
                isRelativeLineTo = false;
                horizontalMove = true;
                verticalMove = false;
            }
            else if (c == 'h')
            {
                isRelativeLineTo = true;
                horizontalMove = true;
                verticalMove = false;
            }
        }

        for (var cntr = 0; cntr < numList.Count; cntr += 2)
        {
            var point = new Vector2(numList[cntr], numList[cntr + 1]);
            yield return point;
        }
    }

    static IEnumerable<Vector2> ExtractRectPoints(XmlElement xmlElement)
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

    static void RemoveRepeatedPoints(SvgElement svgElement)
    {
        var points = svgElement.Points;
        if (points.Count < 2)
        {
            return;
        }

        for (var cntr = 1; cntr < points.Count; cntr++)
        {
            if (points[cntr] == points[cntr - 1])
            {
                points.RemoveAt(cntr);
                RemoveRepeatedPoints(svgElement);
                return;
            }
        }
    }

    static bool CheckForClosedElement(SvgElement svgElement)
    {
        if (svgElement.Points.First() == svgElement.Points.Last())
        {
            svgElement.Points.RemoveAt(0);
            svgElement.IsClosed = true;
            return true;
        }
        return false;
    }

    static void LoadSvgDataIntoScriptable(SvgScriptable svgObj, XmlNode node)
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
                case "path":
                    {
                        svgElement.Points = ExtractPathPoints(xmlElement).ToList();
                        if (xmlElement.GetAttribute("d").ToLower().Trim().Last() == 'z')
                        {
                            svgElement.IsClosed = true;
                        }
                        break;
                    }
                case "rect":
                    {
                        svgElement.Points = ExtractRectPoints(xmlElement).ToList();
                        svgElement.IsClosed = true;
                        break;
                    }
            }

            RemoveRepeatedPoints(svgElement);

            if (svgElement.Points.Count <= 1)
            {
                continue;
            }
            CheckForClosedElement(svgElement);

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
