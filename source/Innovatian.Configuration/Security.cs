#region Using Directives

using System;
using System.Security.Cryptography;

#endregion

namespace Innovatian.Configuration
{
    internal class Security
    {
        private static byte[] EncryptBytes( byte[] input, SecurityConfiguration configuration )
        {
            configuration.EncryptionAlgorithm.Key =
                configuration.HashAlgorithm.ComputeHash( configuration.Encoding.GetBytes( configuration.Key ) );
            configuration.EncryptionAlgorithm.Mode = CipherMode.ECB;

            ICryptoTransform transform = configuration.EncryptionAlgorithm.CreateEncryptor();
            return transform.TransformFinalBlock( input, 0, input.Length );
        }

        private static byte[] EncryptBytes( string input, SecurityConfiguration configuration )
        {
            var buffer = configuration.Encoding.GetBytes( input );
            var encryptedBuffer = EncryptBytes( buffer, configuration );
            return encryptedBuffer;
        }

        public static string EncryptString( string input, SecurityConfiguration configuration )
        {
            var encryptedBuffer = EncryptBytes( input, configuration );
            var encryptedString = Convert.ToBase64String( encryptedBuffer );
            return encryptedString;
        }

        private static byte[] DecryptBytes( byte[] input, SecurityConfiguration configuration )
        {
            if ( input == null || input.Length == 0 )
            {
                return null;
            }

            configuration.EncryptionAlgorithm.Key =
                configuration.HashAlgorithm.ComputeHash( configuration.Encoding.GetBytes( configuration.Key ) );
            configuration.EncryptionAlgorithm.Mode = CipherMode.ECB;

            ICryptoTransform transform = configuration.EncryptionAlgorithm.CreateDecryptor();

            return transform.TransformFinalBlock( input, 0, input.Length );
        }

        private static byte[] DecryptBytes( string input, SecurityConfiguration configuration )
        {
            return DecryptBytes( Convert.FromBase64String( input ), configuration );
        }

        public static string DecryptString( string input, SecurityConfiguration configuration )
        {
            try
            {
                return configuration.Encoding.GetString( DecryptBytes( input, configuration ) );
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}