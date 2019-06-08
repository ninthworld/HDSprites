using StardewModdingAPI.Utilities;
using System.Collections.Generic;

namespace HDSprites
{
    public class GlobalTokenManager
    {
        public TokenDictionary GlobalTokens { get; set; }
        private Dictionary<string, List<ContentPackAsset>> RegisteredAssets { get; set; }

        public GlobalTokenManager()
        {
            this.GlobalTokens = new TokenDictionary()
            {
                { "day", "1" },
                { "dayOfWeek", "monday" },
                { "language", "en" },
                { "season", "spring" },
                { "weather", "sun" }
            };

            this.RegisteredAssets = new Dictionary<string, List<ContentPackAsset>>();
            foreach (string token in this.GlobalTokens.Keys)
            {
                this.RegisteredAssets.Add(token, new List<ContentPackAsset>());
            }
        }

        public void RegisterAsset(string token, ContentPackAsset asset)
        {
            if (this.RegisteredAssets.TryGetValue(token, out var list) && !list.Contains(asset))
            {
                list.Add(asset);
            }
        }

        public void OnDayStarted()
        {
            SDate date = SDate.Now();

            TokenDictionary oldTokens = new TokenDictionary(this.GlobalTokens);

            this.GlobalTokens["day"] = date.Day.ToString();
            this.GlobalTokens["dayOfWeek"] = date.DayOfWeek.ToString().ToLower();
            this.GlobalTokens["season"] = date.Season.ToLower();

            List<ContentPackAsset> updateAsset = new List<ContentPackAsset>();
            foreach (var entry in this.GlobalTokens)
            {
                if (!oldTokens[entry.Key].Equals(entry.Value))
                {
                    foreach (var asset in this.RegisteredAssets[entry.Key])
                    {
                        if (!updateAsset.Contains(asset))
                        {
                            updateAsset.Add(asset);
                        }
                    }
                }
            }

            foreach (var asset in updateAsset)
            {
                asset.Update();
            }
        }
    }
}
