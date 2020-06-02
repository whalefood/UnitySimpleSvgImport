# UnitySimpleSvgImport
A simple importer for svg files to Unity

This is a utility that converts svg files into scriptable objects for use by Unity.  
It uses a "ScriptedImporter" which is technically part of Unity's experimental library, but seems to stable.

This doesn't include any rendering functionality, but I recommend using something like Freya Holmér's Shapes library.

The importer is pretty rudementary at the moment and only supports the svg elements that I needed.  These include:

* line
* polyline
* polygon
* rect

But it should be pretty easy to expand if needed.

Feel free to reach out if you have any questions: Jonah@WFGames.com
