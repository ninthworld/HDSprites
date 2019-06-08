using System.Collections.Generic;

namespace HDSprites
{
    public class TokenDictionary : Dictionary<string, string>
    {
        public TokenDictionary() : base()
        {
        }

        public TokenDictionary(TokenDictionary d) : base(d)
        {
        }
    }
}
