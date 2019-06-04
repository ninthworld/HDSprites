/// **********
/// HDSpritesMod is a mod for Stardew Valley using SMAPI and Harmony.
/// It loads all *.png/*.xnb from the mod's local assets folder into 
/// ScaledTexture2D objects and replaces their game loaded counterparts.
/// 
/// Harmony is used to patch the XNA drawMethod (which the game uses to render its
/// textures) to check if the texture being drawn is of the replaced type ScaledTexture2D, 
/// and if it is, then draw the larger version using its scale adjusted parameters.
/// 
/// Credit goes to Platonymous for the ScaledTexture2D and SpriteBatchFix Harmony 
/// patch classes from his Portraiture mod that makes this whole mod possible.
/// 
/// Author: NinthWorld
/// Date: 5/31/19
/// **********

using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace HDSprites
{
    public class HDSpritesMod : Mod, IAssetEditor
    {
        public static string assetsFolder = "assets";

        public string assetPath;
        public List<string> assetFiles;
        public static Dictionary<String, Texture2D> assets;

        public static string toolsTextureName = "TileSheets/tools";
        public static int toolsTextureWidth = 336;
        public static int toolsTextureHeight = 384;
        public static Color toolsTextureColor = new Color(13, 37, 42);

        public override void Entry(IModHelper help)
        {
            assetPath = Path.Combine(help.DirectoryPath, assetsFolder);
            Directory.CreateDirectory(assetPath);

            assetFiles = new List<string>();
            assets = new Dictionary<string, Texture2D>();

            string[] allFiles = Directory.GetFiles(assetPath, "*.*", SearchOption.AllDirectories);
            foreach(var file in allFiles)
            {
                if(file.ToLower().EndsWith("png") || file.ToLower().EndsWith("xnb"))
                {
                    assetFiles.Add(file.Substring(assetPath.Length).Replace("\\", "/").TrimStart("/".ToCharArray()));
                }
            }

            Monitor.Log("Loading Assets...");
            foreach (String file in assetFiles)
            {
                string assetName = file.Substring(0, file.Length - 4);
                // Monitor.Log("(" + (assets.Count + 1) + "/" + assetFiles.Count + ") - " + assetName);
                Texture2D texture = this.Helper.Content.Load<Texture2D>(assetsFolder + "/" + file, ContentSource.ModFolder);
                ScaledTexture2D scaled = new ScaledTexture2D(texture, assetName, 2);
                assets.Add(assetName, scaled);
            }

            HarmonyInstance instance = HarmonyInstance.Create("NinthWorld.HDSprites");
            instance.PatchAll(Assembly.GetExecutingAssembly());
        }

        public bool CanEdit<T>(IAssetInfo asset)
        {
            foreach (String name in assets.Keys) if (asset.AssetNameEquals(name)) return true;
            return false;
        }

        public void Edit<T>(IAssetData asset)
        {
            foreach(KeyValuePair<string, Texture2D> entry in assets)
            {
                if (asset.AssetNameEquals(entry.Key)) asset.AsImage().ReplaceWith(entry.Value);
            }
        }
    }
    
    // Modified from PyTK.Types.ScaledTexture2D
    // Origial Source: https://github.com/Platonymous/Stardew-Valley-Mods/blob/master/PyTK/Types/ScaledTexture2D.cs
    // Original Licence: GNU General Public License v3.0
    // Original Author: Platonymous
    public class ScaledTexture2D : Texture2D
    {
        public float Scale { get; set; }
        public virtual Texture2D STexture { get; set; }
        public string AssetName { get; set; }
        
        public ScaledTexture2D(Texture2D tex, string name, float scale = 1)
            : base(tex.GraphicsDevice, (int)(tex.Width / scale), (int)(tex.Height / scale))
        {
            Color[] data = new Color[(int)(tex.Width / scale) * (int)(tex.Height / scale)];
            for (int i = 0; i < data.Length; ++i) data[i] = Color.White;

            // Fixes tools texture
            if (name.Equals(HDSpritesMod.toolsTextureName)) data[0] = HDSpritesMod.toolsTextureColor;

            SetData(data);

            Scale = scale;
            STexture = tex;
            AssetName = name;
        }
    }

    // Modified from PyTK.Overrides.OvSpritebatch
    // Origial Source: https://github.com/Platonymous/Stardew-Valley-Mods/blob/master/PyTK/Overrides/OvSpritebatch.cs
    // Original Licence: GNU General Public License v3.0
    // Original Author: Platonymous
    public class DrawFix {

        internal static MethodInfo drawMethod = AccessTools.Method(Type.GetType("Microsoft.Xna.Framework.Graphics.SpriteBatch, Microsoft.Xna.Framework.Graphics"), "InternalDraw");
        
        [HarmonyPatch]
        internal class SpriteBatchFix
        {
            internal static MethodInfo TargetMethod()
            {
                if (Type.GetType("Microsoft.Xna.Framework.Graphics.SpriteBatch, Microsoft.Xna.Framework.Graphics") != null)
                    return AccessTools.Method(Type.GetType("Microsoft.Xna.Framework.Graphics.SpriteBatch, Microsoft.Xna.Framework.Graphics"), "InternalDraw");
                else
                    return AccessTools.Method(typeof(FakeSpriteBatch), "InternalDraw");
            }

            static bool skip = false;

            static Color[] toolsData = new Color[HDSpritesMod.toolsTextureWidth * HDSpritesMod.toolsTextureHeight];

            internal static bool Prefix(ref SpriteBatch __instance, ref Texture2D texture, ref Vector4 destination, ref bool scaleDestination, ref Rectangle? sourceRectangle, ref Color color, ref float rotation, ref Vector2 origin, ref SpriteEffects effects, ref float depth)
            {
                if (skip || !sourceRectangle.HasValue) return true;

                // Fixes tools texture
                if (texture.Width == HDSpritesMod.toolsTextureWidth && texture.Height == HDSpritesMod.toolsTextureHeight) {
                    try
                    {
                        texture.GetData<Color>(toolsData);
                        if (toolsData[0] == HDSpritesMod.toolsTextureColor) texture = HDSpritesMod.assets.GetValueSafe(HDSpritesMod.toolsTextureName);
                    }
                    catch (Exception e) { }
                }

                if (texture is ScaledTexture2D s && sourceRectangle != null && sourceRectangle.Value is Rectangle r)
                {
                    var newDestination = new Vector4(destination.X, destination.Y, destination.Z / s.Scale, destination.W / s.Scale);
                    var newSR = new Rectangle?(new Rectangle((int)(r.X * s.Scale), (int)(r.Y * s.Scale), (int)(r.Width * s.Scale), (int)(r.Height * s.Scale)));
                    var newOrigin = new Vector2(origin.X * s.Scale, origin.Y * s.Scale);

                    // Fixes scaling issues
                    if (s.AssetName.Equals("TileSheets/Craftables") 
                        || s.AssetName.Equals("Maps/MenuTiles") 
                        || s.AssetName.Equals("LooseSprites/LanguageButtons")
                        || s.AssetName.Equals("LooseSprites/chatBox")
                        || s.AssetName.Equals("LooseSprites/textBox")
                        || s.AssetName.Equals("LooseSprites/yellowLettersLogo"))
                    {
                        if (!scaleDestination) newDestination = destination;
                    }

                    skip = true;
                    drawMethod.Invoke(__instance, new object[] { s.STexture, newDestination, scaleDestination, newSR, color, rotation, newOrigin, effects, depth });
                    skip = false;

                    return false;
                }
                
                return true;
            }

            internal class FakeSpriteBatch
            {
                internal void DrawInternal(Texture2D texture, Vector4 destinationRectangle, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, SpriteEffects effect, float depth, bool autoFlush) { return; }
                internal void InternalDraw(Texture2D texture, ref Vector4 destination, bool scaleDestination, ref Rectangle? sourceRectangle, Color color, float rotation, ref Vector2 origin, SpriteEffects effects, float depth) { return; }
            }
        }
    }
}