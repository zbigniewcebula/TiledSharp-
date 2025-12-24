/* Distributed as part of TiledSharp, Copyright 2012 Marshall Ward
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;

namespace TiledSharp
{
	// TODO: The design here is all wrong. A Tileset should be a list of tiles,
	//	   it shouldn't force the user to do so much tile ID management

	public class TmxTileset : TmxDocument, ITmxElement
	{
		public int FirstGid { get; private set; }
		public string Name { get; private set; }
		public int TileWidth { get; private set; }
		public int TileHeight { get; private set; }
		public int Spacing { get; private set; }
		public int Margin { get; private set; }
		public int Columns { get; private set; }
		public int TileCount { get; private set; }

		public IReadOnlyDictionary<int, TmxTilesetTile> Tiles { get; private set; }
		public TmxTileOffset TileOffset { get; private set; }
		public PropertyDict Properties { get; private set; }
		public TmxImage Image { get; private set; }
		public TmxList<TmxTerrain> Terrains { get; private set; }

		// TSX file constructor
		public TmxTileset(XContainer xDoc, string tmxDir, ICustomLoader customLoader = null)
			: this(xDoc.Element("tileset"), tmxDir, customLoader)
		{ }

		// TMX tileset element constructor
		public TmxTileset(XElement xTileset, string tmxDir = "", ICustomLoader customLoader = null) : base(customLoader)
		{
			var xFirstGid = xTileset.Attribute("firstgid");
			var source = (string)xTileset.Attribute("source");

			if(source != null)
			{
				// Prepend the parent TMX directory if necessary
				source = Path.Combine(tmxDir, source);

				// source is always preceded by firstgid
				FirstGid = (int)xFirstGid;

				// Everything else is in the TSX file
				var xDocTileset = ReadXml(source);
				var ts = new TmxTileset(xDocTileset, TmxDirectory, CustomLoader);
				Name = ts.Name;
				TileWidth = ts.TileWidth;
				TileHeight = ts.TileHeight;
				Spacing = ts.Spacing;
				Margin = ts.Margin;
				Columns = ts.Columns;
				TileCount = ts.TileCount;
				TileOffset = ts.TileOffset;
				Image = ts.Image;
				Terrains = ts.Terrains;
	            Tiles = ts.Tiles;
                Properties = ts.Properties;
            }
            else
            {
                // firstgid is always in TMX, but not TSX
				if(xFirstGid != null)
				{
					FirstGid = (int)xFirstGid;
				}

				Name = (string)xTileset.Attribute("name");
                TileWidth = (int)xTileset.Attribute("tilewidth");
                TileHeight = (int)xTileset.Attribute("tileheight");

				var spacing = xTileset.Attribute("spacing");
				if(spacing != null)
				{
					Spacing = (int)xTileset.Attribute("spacing");
				}
				
				var margin =  xTileset.Attribute("margin");
				if(margin != null)
				{
					Margin = (int)xTileset.Attribute("margin");
				}
				
                Columns = (int)xTileset.Attribute("columns");
                TileCount = (int)xTileset.Attribute("tilecount");
                TileOffset = new(xTileset.Element("tileoffset"));
                Image = new(xTileset.Element("image"), tmxDir);

                Terrains = new();
                var xTerrainType = xTileset.Element("terraintypes");
                if(xTerrainType != null) {
					foreach(var e in xTerrainType.Elements("terrain"))
					{
                        Terrains.Add(new(e));
					}
                }

                var tiles = new Dictionary<int, TmxTilesetTile>();
                Tiles = tiles;
                foreach(var xTile in xTileset.Elements("tile")) {
                    var tile = new TmxTilesetTile(xTile, Terrains, tmxDir, Image);
					tiles[tile.Id] = tile;
                }

				if(tiles.Count == 0)
				{
					for(int i = 0; i < TileCount; ++i)
					{
						XElement xTile = new XElement("tile");
						xTile.SetAttributeValue("id", i.ToString());
						tiles.Add(FirstGid + i, new TmxTilesetTile(
							xTile,
							Terrains,
							tmxDir,
							Image
						));
					}

					Tiles = tiles;
				}

                Properties = new(xTileset.Element("properties"));
            }
        }
    }

    public class TmxTileOffset
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public TmxTileOffset(XElement xTileOffset)
		{
			if(xTileOffset == null)
			{
				return;
			}

			X = (int)xTileOffset.Attribute("x");
			Y = (int)xTileOffset.Attribute("y");
		}
    }

    public class TmxTerrain : ITmxElement
    {
        public string Name { get; private set; }
        public int Tile { get; private set; }

        public PropertyDict Properties { get; private set; }

        public TmxTerrain(XElement xTerrain)
        {
            Name = (string)xTerrain.Attribute("name");
            Tile = (int)xTerrain.Attribute("tile");
            Properties = new(xTerrain.Element("properties"));
        }
    }

    public class TmxTilesetTile
    {
        public int Id { get; private set; }
        //public Collection<TmxTerrain> TerrainEdges { get; private set; }
        public IReadOnlyList<TmxTerrain> TerrainEdges { get; private set; }
        public double Probability { get; private set; }
        public string Type { get; private set; }

        public PropertyDict Properties { get; private set; }
        public TmxImage Image { get; private set; }
        public TmxList<TmxObjectGroup> ObjectGroups { get; private set; }
        public IReadOnlyCollection<TmxAnimationFrame> AnimationFrames { get; private set; }

        // Human-readable aliases to the Terrain markers
        public TmxTerrain TopLeft => TerrainEdges[0];
		public TmxTerrain TopRight => TerrainEdges[1];
		public TmxTerrain BottomLeft => TerrainEdges[2];
		public TmxTerrain BottomRight => TerrainEdges[3];

        public TmxTilesetTile(
			XElement xTile,
			TmxList<TmxTerrain> terrains,
            string tmxDir, 
			TmxImage tileSetImage
		)
        {
            Id = (int)xTile.Attribute("id");

            var terrainEdges = new Collection<TmxTerrain>();
            TerrainEdges = terrainEdges;

            int result;

            var strTerrain = (string)xTile.Attribute("terrain") ?? ",,,";
			var terrain    = strTerrain.Split(',');
            foreach(var v in terrain) {
                var success = int.TryParse(v, out result);
				
				if(terrainEdges.Count == 4)
				{
					throw new("Too many terrain edges!");
				}
				
                terrainEdges.Add(success ? terrainEdges[result] : null);
            }

            Probability = (double?)xTile.Attribute("probability") ?? 1.0;
			if(xTile.Attribute("class") != null)
			{
                Type = (string)xTile.Attribute("class") ?? string.Empty;
			}
			else
			{
                Type = (string)xTile.Attribute("type") ?? string.Empty;
			}
            
            var xImage = xTile.Element("image");
            if(xImage != null)
            {
                Image = new(xImage, tmxDir);
            }
            else
            {
                Image = tileSetImage;
            }

            ObjectGroups = new();
			foreach(var e in xTile.Elements("objectgroup"))
			{
                ObjectGroups.Add(new(e));
			}

			var animationFrames = new Collection<TmxAnimationFrame>();
			AnimationFrames = animationFrames;
			
			var animation = xTile.Element("animation");
            if(animation != null)
			{
				var frames = animation.Elements("frame");
				foreach(var e in frames)
				{
					animationFrames.Add(new(e));
				}
            }

            Properties = new(xTile.Element("properties"));
        }
    }

    public class TmxAnimationFrame
    {
        public int Id { get; private set; }
        public int Duration { get; private set; }

        public TmxAnimationFrame(XElement xFrame)
        {
            Id = (int)xFrame.Attribute("tileid");
            Duration = (int)xFrame.Attribute("duration");
        }
    }
}
