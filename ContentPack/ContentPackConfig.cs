using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace HDSprites
{
    public class ChangesConfig
    {
        public string Action { get; set; }
        public string Target { get; set; }
        public string FromFile { get; set; }
        public string Patchmode { get; set; } = "";
        public Dictionary<string, string> When { get; set; } = new Dictionary<string, string>();
        public string Enabled { get; set; } = "True";
        public Rectangle FromArea { get; set; }
        public Rectangle ToArea { get; set; }
    }

    public class ContentConfig
    {
        public ChangesConfig[] Changes { get; set; }
    }
}
