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
using StardewModdingAPI.Events;
using StardewValley;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using StardewModdingAPI.Utilities;
using System.Text.RegularExpressions;

namespace HDSprites
{
    public class ContentPackAsset
    {
        public static Dictionary<string, string> GlobalTokens { get; set; } = new Dictionary<string, string>()
        {
            { "day", "0" },
            { "dayOfWeek", "sunday" },
            { "season", "spring" },
            { "weather", "sun" },
            { "language", "en" }
        };

        public IContentPack ContentPack { get; set; }
        private string Target { get; set; }
        private string File { get; set; }
        public Rectangle FromArea { get; set; }
        public Rectangle ToArea { get; set; }
        public bool Overlay { get; set; }
        public List<string> Tokens { get; set; }
        public Dictionary<string, List<string>> When { get; set; }
        
        public ContentPackAsset(IContentPack contentPack, string target, string file)
        {
            this.ContentPack = contentPack;
            this.Target = target;
            this.File = file;

            this.Tokens = new List<string>();
            Regex rx = new Regex(@"{{(.+)}}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            foreach (Match match in rx.Matches(this.Target))
            {
                if (!this.Tokens.Contains(match.Value)) this.Tokens.Add(match.Value);
            }
            foreach (Match match in rx.Matches(this.File))
            {
                if (!this.Tokens.Contains(match.Value)) this.Tokens.Add(match.Value);
            }

            this.When = new Dictionary<string, List<string>>();
        }

        public string getTarget()
        {
            return ReplaceTokens(this.Target, GlobalTokens);
        }

        public string getFile()
        {
            return ReplaceTokens(this.File, GlobalTokens);
        }

        public bool isPartial()
        {
            return !this.FromArea.IsEmpty || this.Overlay;
        }

        public bool isEnabled()
        {
            foreach (var token in When)
            {
                if (GlobalTokens.TryGetValue(token.Key, out string value))
                {
                    if (!token.Value.Contains(value)) return false;
                }
            }
            return true;
        }

        public void update()
        {
            string assetName = getTarget();
            
            if (HDSpritesMod.AssetTextures.TryGetValue(assetName, out var assetTexture))
            {
                if (isEnabled())
                {
                    List<ContentPackAsset> cpOverlayAssets = new List<ContentPackAsset>();
                    foreach (var cp in HDSpritesMod.ContentPackAssets)
                    {
                        if (cp.getTarget().Equals(assetName) && cp.ContentPack.Equals(ContentPack) && cp.Overlay) cpOverlayAssets.Add(cp);
                    }

                    Texture2D cpTexture = LoadTexture(getFile(), ContentPack);

                    if (isPartial())
                    {
                        if (HDSpritesMod.AssetFiles.TryGetValue(assetName, out string hdFile))
                        {
                            assetTexture.HDTexture = HDSpritesMod.LoadHDTexture(hdFile);
                            assetTexture.setSubTexture(cpTexture, FromArea, ToArea, Overlay);
                        }
                    }
                    else
                    {
                        assetTexture.HDTexture = cpTexture;
                    }

                    foreach (ContentPackAsset cp in cpOverlayAssets)
                    {
                        Texture2D overlayTexture = LoadTexture(cp.getFile(), cp.ContentPack);
                        assetTexture.setSubTexture(overlayTexture, cp.FromArea, cp.ToArea, cp.Overlay);
                    }
                }
                else
                {
                    if (HDSpritesMod.AssetFiles.TryGetValue(assetName, out string hdFile))
                    {
                        assetTexture.HDTexture = HDSpritesMod.LoadHDTexture(hdFile);
                    }
                }
            }
        }

        public static string ReplaceTokens(string input, Dictionary<string, string> tokens)
        {
            foreach (var token in tokens) input = input.Replace("{{" + token.Key + "}}", token.Value);
            return input;
        }

        private static Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        public static Texture2D LoadTexture(string file, IContentPack contentPack)
        {
            if (!TextureCache.TryGetValue(file, out Texture2D texture))
            {
                texture = contentPack.LoadAsset<Texture2D>(file);
                TextureCache.Add(file, texture);
            }
            return texture;
        }
    }

    public class HDSpritesMod : Mod, IAssetEditor
    {
        public static List<ContentPackAsset> ContentPackAssets = new List<ContentPackAsset>();
        public static Dictionary<string, AssetTexture> AssetTextures = new Dictionary<string, AssetTexture>();
        public static Dictionary<string, string> AssetFiles = new Dictionary<string, string>();
        public static bool EnableMod = true;
        public static IModHelper ModHelper;

        public static List<string> ScaleFixAssets = new List<string>()
        {
            "TileSheets\\Craftables",
            "Maps\\MenuTiles",
            "LooseSprites\\LanguageButtons",
            "LooseSprites\\chatBox",
            "LooseSprites\\textBox",
            "LooseSprites\\yellowLettersLogo",
            "LooseSprites\\JunimoNote"
        };
        public static List<string> WhiteBoxFixAssets = new List<string>()
        {
            "TileSheets\\tools"
        };

        private ModConfig Config;

        public override void Entry(IModHelper help)
        {
            this.Config = this.Helper.ReadConfig<ModConfig>();            
            this.Helper.Events.Input.ButtonPressed += OnButtonPressed;
            this.Helper.Events.GameLoop.DayStarted += OnDayStarted;

            EnableMod = this.Config.EnableMod;
            ModHelper = this.Helper;

            // Base Assets
            foreach (var asset in this.Config.LoadAssets)
            {
                if (asset.Value)
                {
                    string loadSection = asset.Key.Substring(0, asset.Key.LastIndexOf("/"));
                    if (this.Config.LoadSections.GetValueSafe(loadSection))
                    {
                        string assetFileRel = Path.Combine(this.Config.AssetsPath, asset.Key) + ".png";
                        string assetFileAbs = Path.Combine(help.DirectoryPath, assetFileRel);
                        if (File.Exists(assetFileAbs)) AssetFiles.Add(asset.Key.Replace("/", $"\\"), assetFileRel);
                    }
                }
            }

            // Content Pack Assets
            if (this.Config.EnableContentPacks)
            {
                foreach (IContentPack contentPack in this.Helper.ContentPacks.GetOwned())
                {
                    this.Monitor.Log($"Reading content pack: {contentPack.Manifest.Name} {contentPack.Manifest.Version} from {contentPack.DirectoryPath}");

                    ContentConfig contentConfig = contentPack.ReadJsonFile<ContentConfig>("content.json");                    
                    Dictionary<string, string> configChoices = contentPack.ReadJsonFile<Dictionary<string, string>>("config.json");                    
                    foreach (var change in contentConfig.Changes)
                    {
                        if ((!change.Action.Equals("Load") && !change.Action.Equals("EditImage")) || change.Enabled.ToLower().Equals("false")) continue;

                        Dictionary<string, List<string>> cpWhen = new Dictionary<string, List<string>>();

                        bool shouldUse = true;
                        foreach (var when in change.When)
                        {
                            if (configChoices.ContainsKey(when.Key))
                            {
                                List<string> values = new List<string>(when.Value.ToLower().Replace(" ", "").Split(','));
                                shouldUse &= values.Contains(configChoices.GetValueSafe(when.Key).ToLower());
                            }
                            else
                            {
                                string key = when.Key.Substring(0, 1).ToLower() + when.Key.Substring(1);
                                if (!cpWhen.ContainsKey(key))
                                {
                                    string value = when.Value.ToLower().Replace(" ", "");
                                    List<string> whenList = new List<string>(value.Split(','));
                                    cpWhen.Add(key, whenList);
                                }
                            }
                        }
                        if (!shouldUse) continue;

                        string target = change.Target.Replace("/", $"\\");
                        string file = change.FromFile;
                        if (configChoices != null)
                        {
                            target = ContentPackAsset.ReplaceTokens(target, configChoices);
                            file = ContentPackAsset.ReplaceTokens(file, configChoices);
                        }

                        ContentPackAsset asset = new ContentPackAsset(contentPack, target, file);
                        asset.FromArea = change.FromArea;
                        asset.ToArea = change.ToArea;
                        asset.Overlay = change.Patchmode.ToLower().Equals("overlay");

                        asset.When = cpWhen;
                        foreach (var when in cpWhen)
                        {
                            if (!asset.Tokens.Contains(when.Key)) asset.Tokens.Add(when.Key);
                        }

                        ContentPackAssets.Add(asset);
                    }
                }
            }

            HarmonyInstance instance = HarmonyInstance.Create("NinthWorld.HDSprites");
            instance.PatchAll(Assembly.GetExecutingAssembly());
        }

        public bool CanEdit<T>(IAssetInfo assetInfo)
        {
            return AssetFiles.ContainsKey(assetInfo.AssetName);
        }

        public void Edit<T>(IAssetData assetData)
        {
            AssetTexture assetTexture;
            if (!AssetTextures.ContainsKey(assetData.AssetName))
            {
                ContentPackAsset cpAsset = null;
                List<ContentPackAsset> cpOverlayAssets = new List<ContentPackAsset>();
                foreach (ContentPackAsset cp in ContentPackAssets)
                {
                    if (cp.getTarget().Equals(assetData.AssetName) && cp.isEnabled())
                    {
                        if (cp.Overlay)
                        {
                            cpOverlayAssets.Add(cp);
                            continue;
                        }
                        if (cpAsset == null) cpAsset = cp;
                    }
                }

                Texture2D cpTexture = null;
                if (cpAsset != null)
                {
                    cpTexture = ContentPackAsset.LoadTexture(cpAsset.getFile(), cpAsset.ContentPack);
                }

                if (cpAsset == null || cpAsset.isPartial()) {
                    Texture2D hdTexture = LoadHDTexture(AssetFiles.GetValueSafe(assetData.AssetName));                    
                    assetTexture = new AssetTexture(assetData.AssetName, assetData.AsImage().Data, hdTexture, this.Config.AssetScale, WhiteBoxFixAssets.Contains(assetData.AssetName));

                    if (cpAsset != null && cpAsset.isPartial()) assetTexture.setSubTexture(cpTexture, cpAsset.FromArea, cpAsset.ToArea, cpAsset.Overlay);                    
                }
                else
                {
                    assetTexture = new AssetTexture(assetData.AssetName, assetData.AsImage().Data, cpTexture, this.Config.AssetScale, WhiteBoxFixAssets.Contains(assetData.AssetName));
                }

                foreach (ContentPackAsset cp in cpOverlayAssets)
                {
                    Texture2D overlayTexture = ContentPackAsset.LoadTexture(cp.getFile(), cp.ContentPack);
                    assetTexture.setSubTexture(overlayTexture, cp.FromArea, cp.ToArea, cp.Overlay);
                }

                AssetTextures.Add(assetData.AssetName, assetTexture);
            }
            else
            {
                assetTexture = AssetTextures.GetValueSafe(assetData.AssetName);
                assetTexture.setOriginalTexture(assetData.AsImage().Data);
            }

            assetData.AsImage().ReplaceWith(assetTexture);
        }

        private static Dictionary<string, Texture2D> HDTextureCache = new Dictionary<string, Texture2D>();
        public static Texture2D LoadHDTexture(string file)
        {
            if (!HDTextureCache.TryGetValue(file, out Texture2D texture))
            {
                texture = ModHelper.Content.Load<Texture2D>(file, ContentSource.ModFolder);
                HDTextureCache.Add(file, texture);
            }
            return texture;
        }

        private void OnButtonPressed(object s, ButtonPressedEventArgs e)
        {
            if (e.Button.Equals(this.Config.ToggleEnableButton))
            {
                EnableMod = !EnableMod;
                this.Config.EnableMod = EnableMod;
                this.Helper.WriteConfig(this.Config);
            }
        }

        private void OnDayStarted(object s, DayStartedEventArgs e)
        {
            SDate date = SDate.Now();
            Dictionary<string, string> newTokens = new Dictionary<string, string>()
            {
                { "day", date.Day.ToString() },
                { "dayOfWeek", date.DayOfWeek.ToString().ToLower() },
                { "season", date.Season.ToLower() },
                { "weather", "sun" },
                { "language", "en" }
            };

            List<ContentPackAsset> updateAssets = new List<ContentPackAsset>();
            foreach (var token in newTokens)
            {
                if (!ContentPackAsset.GlobalTokens.GetValueSafe(token.Key).Equals(token.Value))
                {
                    ContentPackAsset.GlobalTokens[token.Key] = token.Value;                    
                    foreach (var cpAsset in ContentPackAssets)
                    {
                        if (cpAsset.Tokens.Contains(token.Key))
                        {
                            if (!updateAssets.Contains(cpAsset)) updateAssets.Add(cpAsset);
                        }
                    }
                }
            }

            foreach (ContentPackAsset cpAsset in updateAssets)
            {
                cpAsset.update();
            }
        }
    }
}