// Distributed as part of TiledSharp, Copyright 2012 Marshall Ward
// Licensed under the Apache License, Version 2.0
// http://www.apache.org/licenses/LICENSE-2.0
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.IO;
using System.Linq;
namespace TiledSharp
{
	public class TmxLayer : ITmxLayer
	{
		private static readonly char[] CSV_DELIMETERS = new[] {',', '\n'};
		
		public string Name { get; private set; }
		public double Opacity { get; private set; }
		public bool Visible { get; private set; }
		public double OffsetX { get; private set; }
		public double OffsetY { get; private set; }
		public TmxColor Tint { get; private set; }
		public IReadOnlyCollection<TmxLayerTile> Tiles { get; private set; }
		public PropertyDict Properties { get; private set; }
		public TmxLayer(XElement xLayer, int width, int height)
		{
			Name = (string) xLayer.Attribute("name");
			
			Opacity = xLayer.Attribute("opacity") != null ? (double)xLayer.Attribute("opacity") : 1;
			Visible = xLayer.Attribute("visible") == null || (bool) xLayer.Attribute("visible");
			OffsetX = xLayer.Attribute("offsetx") != null ? (double)xLayer.Attribute("offsetx") : 0;
			OffsetY = xLayer.Attribute("offsety") != null ? (double)xLayer.Attribute("offsety") : 0;
			
			Tint = new TmxColor(xLayer.Attribute("tint"));
			var xData = xLayer.Element("data");
			var encoding = (string)xData.Attribute("encoding");
			IEnumerable<XElement> xChunks = xData.Elements("chunk").ToList();
			if(xChunks.Any())
			{
				foreach(XElement xChunk in xChunks)
				{
					var chunkWidth = (int)xChunk.Attribute("width");
					var chunkHeight = (int)xChunk.Attribute("height");
					var chunkX = (int)xChunk.Attribute("x");
					var chunkY = (int)xChunk.Attribute("y");
					ReadChunk(chunkWidth, chunkHeight, chunkX, chunkY, encoding, xChunk);
				}
			}
			else
			{
				ReadChunk(width, height, 0, 0, encoding, xData);
			}
			Properties = new(xLayer.Element("properties"));
		}
	    private void ReadChunk(int width, int height, int startX, int startY, string encoding, XElement xData)
        {
            var tiles = new Collection<TmxLayerTile>();
            Tiles = tiles;
            
            switch(encoding)
            {
                case("base64"):
                {
                    var       decodedStream = new TmxBase64Data(xData);
                    var       stream        = decodedStream.Data;
                    using var br            = new BinaryReader(stream);
                    for(var j = 0; j < height; ++j)
                    {
                        for(var i = 0; i < width; ++i)
                        {
                            tiles.Add(new(br.ReadUInt32(), i + startX, j + startY));
                        }
                    }

                    break;
                }
                case("csv"):
                {
                    var csvData = xData.Value;
                    var k = 0;
                    var data = csvData.Split(
                        CSV_DELIMETERS,
                        StringSplitOptions.RemoveEmptyEntries
                    );
                    foreach(var cell in data)
                    {
                        var gid = uint.Parse(cell.Trim());
                        var x   = k % width;
                        var y   = k / width;
                        tiles.Add(new(gid, x + startX, y + startY));
                        ++k;
                    }

                    break;
                }
                case(null):
                {
                    var k = 0;
                    foreach(var e in xData.Elements("tile"))
                    {
                        var gid = (uint?) e.Attribute("gid") ?? 0;
                        var x   = k % width;
                        var y   = k / width;
                        tiles.Add(new(gid, x + startX, y + startY));
                        ++k;
                    }

                    break;
                }
                default:
                {
                    throw new("TmxLayer: Unknown encoding.");
                }
            }
        }
    }
    
    public class TmxLayerTile
    {
        // Tile flip bit flags
        const uint FLIPPED_HORIZONTALLY_FLAG = 0x80000000;
        const uint FLIPPED_VERTICALLY_FLAG   = 0x40000000;
        const uint FLIPPED_DIAGONALLY_FLAG   = 0x20000000;
        
        public int Gid { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public bool HorizontalFlip { get; private set; }
        public bool VerticalFlip { get; private set; }
        public bool DiagonalFlip { get; private set; }
        
        public TmxLayerTile(uint id, int x, int y)
        {
            var rawGid = id;
            X = x;
            Y = y;
            // Scan for tile flip bit flags
            bool flip;
            flip = (rawGid & FLIPPED_HORIZONTALLY_FLAG) != 0;
            HorizontalFlip = flip ? true : false;
            flip = (rawGid & FLIPPED_VERTICALLY_FLAG) != 0;
            VerticalFlip = flip ? true : false;
            flip = (rawGid & FLIPPED_DIAGONALLY_FLAG) != 0;
            DiagonalFlip = flip ? true : false;
            // Zero the bit flags
            rawGid &= ~(FLIPPED_HORIZONTALLY_FLAG |
                        FLIPPED_VERTICALLY_FLAG |
                        FLIPPED_DIAGONALLY_FLAG);
            // Save GID remainder to int
            Gid = (int)rawGid;
        }
    }
}
