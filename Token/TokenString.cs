using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HDSprites
{
    public class TokenString
    {
        private static Regex RX = new Regex(@"{{(?<Key>.+)}}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string RawString { get; set; }

        public TokenString(string rawString)
        {
            this.RawString = rawString;
        }

        public List<string> GetTokens()
        {
            List<string> tokens = new List<string>();
            foreach (Match match in RX.Matches(this.RawString))
            {
                tokens.Add(match.Groups["Key"].Value);
            }
            return tokens;
        }

        public TokenString GetInterpreted(TokenDictionary tokens)
        {
            TokenString str = new TokenString(this.RawString);
            foreach (var entry in tokens)
            {
                str.RawString = str.RawString.Replace("{{" + entry.Key + "}}", entry.Value);
            }
            return str;
        }

        public override string ToString()
        {
            return this.RawString;
        }
    }
}
