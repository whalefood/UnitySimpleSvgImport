using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Xml;

public class SvgScriptable : ScriptableObject
{
    public List<SvgElement> SvgData = new List<SvgElement>();


    private void TransformPoints(Func<Vector2, Vector2> transFunc)
    {
        foreach (var elem in SvgData)
        {
            for (int cntr = 0; cntr < elem.Points.Count; cntr++)
            {
                elem.Points[cntr] = transFunc(elem.Points[cntr]);
            }
        }
    }

    public void Scale(float scaleVal)
    {
        TransformPoints(pt => pt * scaleVal);
    }

    public void Scale(Vector2 scaleVal)
    {
        TransformPoints(pt => pt * scaleVal);
    }

    public void Move(Vector2 posChange)
    {
        TransformPoints(pt => pt + posChange);
    }

    public IEnumerable<Vector2> AllPoints
    {
        get
        {
            foreach (var elem in SvgData)
            {
                foreach (var point in elem.Points)
                {
                    yield return point;
                }
            }
        }
    }
}
