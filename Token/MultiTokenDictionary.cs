using System.Collections.Generic;

namespace HDSprites
{
    public class MultiTokenDictionary : Dictionary<string, List<string>>
    {
        public MultiTokenDictionary() : base()
        {
        }

        public MultiTokenDictionary(TokenDictionary tokenDictionary) : base()
        {
            foreach (var entry in tokenDictionary)
            {
                this.Add(entry.Key, entry.Value);
            }
        }

        public void Add(string token, string commaList)
        {
            this.Add(token, new List<string>(commaList.Replace(" ", "").Split(',')));
        }

        public bool ContainsAt(string token, string value)
        {
            return this.TryGetValue(token, out var list) && list.Contains(value);
        }

        public bool ContainsAll(TokenDictionary tokenDictionary)
        {
            foreach (var entry in this)
            {
                if (!tokenDictionary.TryGetValue(entry.Key, out string value) || !entry.Value.Contains(value))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
