using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System.Collections.Generic;

namespace HDSprites
{
    public class ContentPackManager
    {
        private List<ContentPackAsset> ContentPackAssets { get; set; }
        private Dictionary<string, Texture2D> ContentPackTextures { get; set; }
        private HDAssetManager HDAssetManager { get; set; }
        public GlobalTokenManager GlobalTokenManager { get; set; }

        public ContentPackManager(HDAssetManager hdAssetManager)
        {
            this.ContentPackAssets = new List<ContentPackAsset>();
            this.ContentPackTextures = new Dictionary<string, Texture2D>();
            this.HDAssetManager = hdAssetManager;
            this.GlobalTokenManager = new GlobalTokenManager();
        }

        public void AddContentPack(IContentPack contentPack)
        {
            TokenDictionary configChoices = contentPack.ReadJsonFile<TokenDictionary>("config.json");
            if (configChoices == null) configChoices = new TokenDictionary();

            ContentConfig contentConfig = contentPack.ReadJsonFile<ContentConfig>("content.json");
            foreach (var change in contentConfig.Changes)
            {
                if (!(change.Action.Equals("Load") || change.Action.Equals("EditImage")) 
                    || change.Enabled.ToLower().Equals("false")) continue;

                MultiTokenDictionary parsedWhen = new MultiTokenDictionary(change.When);

                MultiTokenDictionary when = new MultiTokenDictionary();

                bool enabled = true;
                foreach (var entry in parsedWhen)
                {
                    if (configChoices.TryGetValue(entry.Key, out var value))
                    {
                        if (!parsedWhen.ContainsAt(entry.Key, value))
                        {
                            enabled = false;
                            break;
                        }
                    }
                    else
                    {
                        string safeKey = entry.Key.Substring(0, 1).ToLower() + entry.Key.Substring(1);
                        when.Add(safeKey, entry.Value.ConvertAll(s => s.ToLower()));
                    }
                }
                if (!enabled) continue;
                
                TokenString target = new TokenString(change.Target.Replace("/", $"\\")).GetInterpreted(configChoices);
                TokenString file = new TokenString(change.FromFile).GetInterpreted(configChoices);
                bool overlay = change.Patchmode.ToLower().Equals("overlay");

                ContentPackAsset asset = new ContentPackAsset(this, contentPack, target, file, when, change.FromArea, change.ToArea, overlay);
                
                this.ContentPackAssets.Add(asset);
            }
        }

        public void EditAsset(string assetName)
        {
            foreach (ContentPackAsset asset in this.ContentPackAssets)
            {
                if (asset.GetTarget().Equals(assetName))
                {
                    this.UpdateAsset(asset);
                }
            }
        }

        public void UpdateAsset(ContentPackAsset asset)
        {
            string assetName = asset.GetTarget();

            if (HDSpritesMod.AssetTextures.TryGetValue(assetName, out var assetTexture))
            {
                bool enabled = asset.IsEnabled();

                if (!enabled && !asset.IsPartial())
                {
                    assetTexture.HDTexture = this.HDAssetManager.LoadAsset(assetName);
                }

                if (enabled)
                {
                    Texture2D texture = this.LoadTexture(asset.GetFile(), asset.ContentPack);

                    if (!asset.IsPartial())
                    {
                        assetTexture.HDTexture = texture;
                    }
                    else
                    {
                        assetTexture.setSubTexture(texture, asset.FromArea, asset.ToArea, asset.Overlay);
                    }
                }

                if (!asset.IsPartial())
                {
                    foreach (var contentPack in this.ContentPackAssets)
                    {
                        if (contentPack.GetTarget().Equals(assetName)
                            && contentPack.ContentPack.Equals(asset.ContentPack)
                            && contentPack.IsPartial())
                        {
                            Texture2D partialTexture = this.LoadTexture(contentPack.GetFile(), contentPack.ContentPack);
                            assetTexture.setSubTexture(partialTexture, contentPack.FromArea, contentPack.ToArea, contentPack.Overlay);
                        }
                    }
                }
            }
        }

        public Texture2D LoadTexture(string file, IContentPack contentPack)
        {
            if (!this.ContentPackTextures.TryGetValue(file, out Texture2D texture))
            {
                texture = contentPack.LoadAsset<Texture2D>(file);
                this.ContentPackTextures.Add(file, texture);
            }
            return texture;
        }
    }
}
