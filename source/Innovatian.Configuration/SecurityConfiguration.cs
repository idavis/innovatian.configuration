#region Using Directives

using System.Security.Cryptography;
using System.Text;

#endregion

namespace Innovatian.Configuration
{
    internal class SecurityConfiguration
    {
        public SecurityConfiguration()
        {
            EncryptionAlgorithm = new AesCryptoServiceProvider {Mode = CipherMode.ECB};
            HashAlgorithm = new MD5CryptoServiceProvider();
        }

        public SymmetricAlgorithm EncryptionAlgorithm { get; set; }
        public HashAlgorithm HashAlgorithm { get; set; }
        public string Key { get; set; }
        public Encoding Encoding { get; set; }
    }
}