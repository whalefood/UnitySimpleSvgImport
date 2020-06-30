using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

[ScriptedImporter(1, "svg")]
public class SvgImporter : ScriptedImporter
{
    [Range(0.0001f, 1f)]
    public float Scale = 1;

    public bool CenterElements = true;
    
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var dataFile = ScriptableObject.CreateInstance<SvgScriptable>();
        var dataFileText = File.ReadAllText(ctx.assetPath);
        SvgParser.LoadScriptableFromString(dataFile, dataFileText);
        dataFile.Scale(new Vector2(1, -1) * this.Scale);

        if (dataFile.SvgData.Count > 0 && CenterElements)
        {
            Vector2 maxCorner = dataFile.SvgData[0].Points[0];
            Vector2 minCorner = maxCorner;
            foreach(var pt in dataFile.AllPoints)
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
    

}
