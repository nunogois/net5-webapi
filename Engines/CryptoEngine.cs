using CryptoHelper;

namespace net5_webapi.Engines
{
    public class CryptoEngine : ICryptoEngine
    {
        public string Hash(string text)
        {
            return Crypto.HashPassword(text);
        }

        public bool HashCheck(string hash, string text)
        {
            return Crypto.VerifyHashedPassword(hash, text);
        }
    }
}
