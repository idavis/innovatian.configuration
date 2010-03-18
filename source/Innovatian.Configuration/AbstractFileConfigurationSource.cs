#region License

//
// Copyright © 2009 Ian Davis <ian.f.davis@gmail.com>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

#endregion

#region Using Directives

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Permissions;
using System.Text;
using Innovatian.Configuration.Properties;

#endregion

namespace Innovatian.Configuration
{
    /// <summary>
    /// AbstractFileConfigurationSource add helper functions to load configuration sources from file
    /// that are not needed in general implementations of <see cref="IConfigurationSource"/>.
    /// </summary>
    public abstract class AbstractFileConfigurationSource : AbstractConfigurationSource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractFileConfigurationSource"/> class.
        /// </summary>
        protected AbstractFileConfigurationSource()
        {
            DefaultEncoding = Encoding.Default;
        }

        /// <summary>
        /// Gets or sets the default file encoding.
        /// </summary>
        /// <value>The default encoding.</value>
        public Encoding DefaultEncoding { get; set; }

        /// <summary>
        /// The file used to store the configuration on disk.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Loads the current instance from the given file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to load into this instance.
        /// </param>
        [FileIOPermission( SecurityAction.Demand, AllFiles = FileIOPermissionAccess.Read,
            AllLocalFiles = FileIOPermissionAccess.Read, Unrestricted = true )]
        protected abstract void Load( string fileName );

        /// <summary>
        /// Loads an <see cref="IConfigurationSource"/> from the given file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to load into a new instance.
        /// </param>
        /// <returns></returns>
        protected static IConfigurationSource FromFile<T>( string fileName )
            where T : AbstractFileConfigurationSource
        {
            var typeToLoad = typeof (T);
            var instance =
                Activator.CreateInstance( typeToLoad, true ) as
                AbstractFileConfigurationSource;
            Debug.Assert( instance != null );
            instance.Load( fileName );
            return instance;
        }

        /// <summary>
        /// Saves all sections. All data merged from other merged sources will
        /// be included.
        /// </summary>
        [FileIOPermission( SecurityAction.Demand, AllFiles = FileIOPermissionAccess.Write,
            AllLocalFiles = FileIOPermissionAccess.Write, Unrestricted = true )]
        public override void Save()
        {
            if ( string.IsNullOrEmpty( FileName ) )
            {
                throw new InvalidOperationException( Text.DestinationFileNameNotSet );
            }

            if ( !File.Exists( FileName ) )
            {
                using ( File.Create( FileName ) )
                {
                    // just making sure it exists.
                }
            }

            using ( var fileStream = File.Open( FileName, FileMode.Truncate, FileAccess.Write, FileShare.Read ) )
            {
                using ( var textFile = new StreamWriter( fileStream, DefaultEncoding ) )
                {
                    string text = ToString();
                    if ( Encrypt )
                    {
                        text = EncryptString( text );
                    }

                    textFile.Write( text );
                    textFile.Flush();
                }
            }
        }

        protected string EncryptString( string text )
        {
            if ( string.Equals( EncryptionKey, DefaultEncryptionKey, StringComparison.OrdinalIgnoreCase ) )
            {
                throw new InvalidOperationException();
            }
            SecurityConfiguration configuration = GetSecurityConfiguration();
            text = Security.EncryptString( text, configuration );
            return text;
        }

        protected string DecryptString( string text )
        {
            if ( string.Equals( EncryptionKey, DefaultEncryptionKey, StringComparison.OrdinalIgnoreCase ) )
            {
                throw new InvalidOperationException();
            }
            SecurityConfiguration configuration = GetSecurityConfiguration();
            text = Security.DecryptString( text, configuration );
            return text;
        }

        private SecurityConfiguration GetSecurityConfiguration()
        {
            var configuration = new SecurityConfiguration {Encoding = DefaultEncoding, Key = EncryptionKey};
            return configuration;
        }

        /// <summary>
        /// If this instance has a file associated with it 
        /// then the settings are cleared and reloaded from file.
        /// If, however, there is no <see cref="FileName"/> set, then
        /// nothing is done.
        /// </summary>
        public override void Reload()
        {
            if ( string.IsNullOrEmpty( FileName ) )
            {
                return;
            }

            Clear();
            Load( FileName );
        }
    }
}