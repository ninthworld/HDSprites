using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace HDSprites
{
    public class ContentPackAsset
    {
        private ContentPackManager Manager { get; set; }
        public IContentPack ContentPack { get; set; }
        public TokenString Target { get; set; }
        public TokenString File { get; set; }
        public MultiTokenDictionary When { get; set; }
        public Rectangle FromArea { get; set; }
        public Rectangle ToArea { get; set; }
        public bool Overlay { get; set; }

        public ContentPackAsset(ContentPackManager manager, IContentPack contentPack, TokenString target, TokenString file, MultiTokenDictionary when, Rectangle fromArea, Rectangle toArea, bool overlay)
        {
            this.Manager = manager;
            this.ContentPack = contentPack;
            this.Target = target;
            this.File = file;
            this.When = when;
            this.Overlay = overlay;
            this.FromArea = fromArea;
            this.ToArea = toArea;

            foreach (string token in this.Target.GetTokens())
            {
                this.Manager.GlobalTokenManager.RegisterAsset(token, this);
            }

            foreach (string token in this.File.GetTokens())
            {
                this.Manager.GlobalTokenManager.RegisterAsset(token, this);
            }

            foreach (string token in this.When.Keys)
            {
                this.Manager.GlobalTokenManager.RegisterAsset(token, this);
            }
        }

        public string GetTarget()
        {
            return this.Target.GetInterpreted(this.Manager.GlobalTokenManager.GlobalTokens).ToString();
        }

        public string GetFile()
        {
            return this.File.GetInterpreted(this.Manager.GlobalTokenManager.GlobalTokens).ToString();
        }

        public bool IsPartial()
        {
            return !this.FromArea.IsEmpty || !this.ToArea.IsEmpty || this.Overlay;
        }

        public bool IsEnabled()
        {
            return this.When.ContainsAll(this.Manager.GlobalTokenManager.GlobalTokens);
        }

        public void Update()
        {
            this.Manager.UpdateAsset(this);
        }
    }
}
