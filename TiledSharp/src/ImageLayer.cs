// Distributed as part of TiledSharp, Copyright 2012 Marshall Ward
// Licensed under the Apache License, Version 2.0
// http://www.apache.org/licenses/LICENSE-2.0
using System;
using System.Xml.Linq;

namespace TiledSharp
{
	public class TmxImageLayer : ITmxLayer
	{
		public string Name { get; private set; }
		
		public int Width { get; private set; }
		public int Height { get; private set; }

		public bool Visible { get; private set; }
		public double Opacity { get; private set; }
		public double OffsetX { get; private set; }
		public double OffsetY { get; private set; }

		public TmxImage Image { get; private set; }

		public PropertyDict Properties { get; private set; }

		double ITmxLayer.OffsetX => OffsetX;
		double ITmxLayer.OffsetY => OffsetY;

		public TmxImageLayer(XElement xImageLayer, string tmxDir = "")
		{
			Name = (string) xImageLayer.Attribute("name");

			Width = xImageLayer.Attribute("width") != null? (int)xImageLayer.Attribute("width") : 0;
			Height = xImageLayer.Attribute("height") != null? (int)xImageLayer.Attribute("height") : 0;
			
			Opacity = xImageLayer.Attribute("opacity") != null ? (double)xImageLayer.Attribute("opacity") : 1;
			Visible = xImageLayer.Attribute("visible") == null || (bool)xImageLayer.Attribute("visible");
			OffsetX = xImageLayer.Attribute("offsetx") != null ? (double)xImageLayer.Attribute("offsetx") : 0;
			OffsetY = xImageLayer.Attribute("offsety") != null ? (double)xImageLayer.Attribute("offsety") : 0;
			
			Image = new(xImageLayer.Element("image"), tmxDir);

			Properties = new(xImageLayer.Element("properties"));
		}
	}
}
