// Distributed as part of TiledSharp, Copyright 2012 Marshall Ward
// Licensed under the Apache License, Version 2.0
// http://www.apache.org/licenses/LICENSE-2.0
using System;
using System.Linq;
using System.Xml.Linq;

namespace TiledSharp
{
	public class TmxGroup : ITmxLayer
	{
		public string Name { get; private set; }

		public double Opacity { get; private set; }
		public bool Visible { get; private set; }
		public double OffsetX { get; private set; }
		public double OffsetY { get; private set; }

		public TmxList<ITmxLayer> Layers { get; private set; }

		public TmxList<TmxLayer> TileLayers { get; private set; }
		public TmxList<TmxObjectGroup> ObjectGroups { get; private set; }
		public TmxList<TmxImageLayer> ImageLayers { get; private set; }
		public TmxList<TmxGroup> Groups { get; private set; }
		public PropertyDict Properties { get; private set; }

		public TmxGroup(XElement xGroup, int width, int height, string tmxDirectory)
		{
			Name = (string)xGroup.Attribute("name") ?? string.Empty;
			Opacity = xGroup.Attribute("opacity") != null ? (double)xGroup.Attribute("opacity") : 1;
			Visible = xGroup.Attribute("visible") == null || (bool)xGroup.Attribute("visible");
			OffsetX = xGroup.Attribute("offsetx") != null ? (double)xGroup.Attribute("offsetx") : 0;
			OffsetY = xGroup.Attribute("offsety") != null ? (double)xGroup.Attribute("offsety") : 0;
			
			Properties = new(xGroup.Element("properties"));

			Layers = new();
			TileLayers = new();
			ObjectGroups = new();
			ImageLayers = new();
			Groups = new();
			var elements = xGroup.Elements()
							  .Where(x => x.Name == "layer"
									  || x.Name == "objectgroup"
									  || x.Name == "imagelayer"
									  || x.Name == "group"
								  );
			foreach(var e in elements)
			{
                ITmxLayer layer;
                switch (e.Name.LocalName)
                {
                    case ("layer"):
                    {
                        var tileLayer = new TmxLayer(e, width, height);
                        layer = tileLayer;
                        TileLayers.Add(tileLayer);
                        break;    
                    }
                    case ("objectgroup"):
                    {
                        var objectgroup = new TmxObjectGroup(e);
                        layer = objectgroup;
                        ObjectGroups.Add(objectgroup);

                        break;
                    }
                    case ("imagelayer"):
                    {
                        var imagelayer = new TmxImageLayer(e, tmxDirectory);
                        layer = imagelayer;
                        ImageLayers.Add(imagelayer);

                        break;
                    }
                    case("group"):
                    {
                        var group = new TmxGroup(e, width, height, tmxDirectory);
                        layer = group;
                        Groups.Add(group);
                        break;
                    }
                    default:
                    {
                        throw new InvalidOperationException();
                    }
                }
                Layers.Add(layer);
            }
        }
    }
}
