using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace HDSprites
{
    public class ChangesConfig
    {
        public string Action { get; set; }
        public string Target { get; set; }
        public string FromFile { get; set; }
        public Rectangle FromArea { get; set; }
        public Rectangle ToArea { get; set; }
        public string Enabled { get; set; } = "True";
        public string Patchmode { get; set; } = "";
        public TokenDictionary When { get; set; } = new TokenDictionary();
    }

    public class ContentConfig
    {
        public ChangesConfig[] Changes { get; set; }
    }
}
