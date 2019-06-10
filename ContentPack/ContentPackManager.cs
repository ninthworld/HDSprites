using HDSprites.Token;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace HDSprites.ContentPack
{
    public class ContentPackManager
    {
        private List<ContentPackAsset> ContentPackAssets { get; set; }
        private Dictionary<string, Texture2D> ContentPackTextures { get; set; }
        private HDAssetManager HDAssetManager { get; set; }
        public DynamicTokenManager DynamicTokenManager { get; set; }

        public ContentPackManager(HDAssetManager hdAssetManager)
        {
            this.ContentPackAssets = new List<ContentPackAsset>();
            this.ContentPackTextures = new Dictionary<string, Texture2D>();
            this.HDAssetManager = hdAssetManager;
            this.DynamicTokenManager = new DynamicTokenManager();
        }

        public void AddContentPack(IContentPack contentPack)
        {
            WhenDictionary configChoices = contentPack.ReadJsonFile<WhenDictionary>("config.json");
            if (configChoices == null) configChoices = new WhenDictionary();

            Dictionary<string, DynamicToken> configTokens = new Dictionary<string, DynamicToken>();
            foreach (var entry in configChoices)
            {
                DynamicToken token = new DynamicToken(entry.Key);
                token.AddValue(new TokenValue(entry.Value, true));
                configTokens.Add(entry.Key, token);
            }

            ContentConfig contentConfig = contentPack.ReadJsonFile<ContentConfig>("content.json");
            if (configChoices.Count < 1)
            {
                foreach (var token in contentConfig.ConfigSchema)
                {
                    if (token.Value.Default != null)
                    {
                        configChoices.Add(token.Key, token.Value.Default);
                    }
                    else if (token.Value.AllowValues != null)
                    {
                        configChoices.Add(token.Key, token.Value.AllowValues.Split(',')[0]);                        
                    }
                }
                contentPack.WriteJsonFile<WhenDictionary>("config.json", configChoices);
            }

            foreach (var dynamicToken in contentConfig.DynamicTokens)
            {
                if (!this.DynamicTokenManager.DynamicTokens.TryGetValue(dynamicToken.Name, out var token)) {
                    token = new DynamicToken(dynamicToken.Name);
                    this.DynamicTokenManager.AddToken(token);
                }

                List<TokenEntry> parsedWhen = new List<TokenEntry>();
                foreach (var entry in dynamicToken.When)
                {
                    parsedWhen.Add(new TokenEntry(entry.Key, entry.Value));
                }

                List<TokenEntry> when = new List<TokenEntry>();

                bool enabled = true;
                foreach (var entry in parsedWhen)
                {
                    if (configChoices.TryGetValue(entry.Name, out var value))
                    {
                        if ((entry.IsConditional && entry.Condition.RawString.Equals(value) != value.ToLower().Equals("true"))
                            || !entry.Values.Contains(value))
                        {
                            enabled = false;
                            break;
                        }
                    }
                    else
                    {
                        when.Add(entry);
                    }
                }

                token.AddValue(new DynamicTokenValue(dynamicToken.Value, enabled, this.DynamicTokenManager, when));
            }

            foreach (var change in contentConfig.Changes)
            {
                if (!(change.Action.Equals("Load") || change.Action.Equals("EditImage")) 
                    || change.Enabled.ToLower().Equals("false")) continue;

                List<TokenEntry> parsedWhen = new List<TokenEntry>();
                foreach (var entry in change.When)
                {
                    parsedWhen.Add(new TokenEntry(entry.Key, entry.Value));
                }

                List<TokenEntry> when = new List<TokenEntry>();

                bool enabled = true;
                foreach (var entry in parsedWhen)
                {
                    if (configChoices.TryGetValue(entry.Name, out var value))
                    {
                        if ((entry.IsConditional && entry.Condition.Parse(this.DynamicTokenManager.DynamicTokens).Equals(value) != value.ToLower().Equals("true")) 
                            || !entry.Values.Contains(value))
                        {
                            enabled = false;
                            break;
                        }
                    }
                    else
                    {
                        when.Add(entry);
                    }
                }
                if (!enabled) continue;

                string[] targetSplit = change.Target.Split(',');
                foreach (string targetStr in targetSplit)
                {
                    string targetStrFix = targetStr;
                    if (targetStr.StartsWith(" ")) targetStrFix = targetStr.Substring(1);

                    StringWithTokens target = new StringWithTokens(targetStrFix.Replace("/", $"\\")).Parse(configTokens);
                    StringWithTokens file = new StringWithTokens(change.FromFile).Parse(configTokens);
                    bool overlay = change.Patchmode.ToLower().Equals("overlay");

                    ContentPackAsset asset = new ContentPackAsset(this, contentPack, target, file, when, change.FromArea, change.ToArea, overlay);

                    this.ContentPackAssets.Add(asset);
                }
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

            if (assetName.Equals("Characters\\Haley"))
            {

            }

            if (HDSpritesMod.AssetTextures.TryGetValue(assetName, out var assetTexture))
            {
                bool enabled = asset.IsEnabled();

                /*
                if (!enabled && !asset.IsPartial())
                {
                    assetTexture.HDTexture = this.HDAssetManager.LoadAsset(assetName);
                }
                */

                if (enabled)
                {
                    Texture2D texture = this.LoadTexture(asset.GetFile(), asset.ContentPack);

                    if (!asset.IsPartial() && texture != null)
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
                            && contentPack.IsPartial()
                            && contentPack.IsEnabled())
                        {
                            Texture2D partialTexture = this.LoadTexture(contentPack.GetFile(), contentPack.ContentPack);
                            if (partialTexture != null)
                            {
                                assetTexture.setSubTexture(partialTexture, contentPack.FromArea, contentPack.ToArea, contentPack.Overlay);
                            }
                        }
                    }
                }
            }
        }

        public Texture2D LoadTexture(string file, IContentPack contentPack)
        {
            if (!this.ContentPackTextures.TryGetValue(file, out Texture2D texture))
            {
                try
                {
                    texture = contentPack.LoadAsset<Texture2D>(file);
                }
                catch (Exception e)
                {
                    return null;
                }

                this.ContentPackTextures.Add(file, texture);
            }
            return texture;
        }
    }
}
