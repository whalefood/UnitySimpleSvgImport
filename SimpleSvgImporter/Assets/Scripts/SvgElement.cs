using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSvgImport
{
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
}