using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;


[Serializable]
public class SvgElement
{
    public bool HasFill;

    public bool HasStroke;

    public Color FillColor;

    public Color StrokeColor;

    public List<Vector2> Points = new List<Vector2>();

    public bool IsClosed;
    

}