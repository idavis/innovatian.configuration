#region Using Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Innovatian.Configuration;

#endregion

namespace HierarchyOfConfigurations
{
    internal class Program
    {
        // Change this to .config, .xml, or .ini to load the different file types.
        // You can also mix and match configuration types if you want.
        private const string Extension = ".ini";
        private static readonly Action WriteLines = () => "     ".ToList().ForEach( Console.WriteLine );

        private static void Main( string[] args )
        {
            // pick any one of these three options to load the settings.
            //IConfigurationSource source = LoadConfigurationSources1();
            //IConfigurationSource source = LoadConfigurationSources2();
            //IConfigurationSource source = LoadConfigurationSources3();
            IConfigurationSource source = LoadConfigurationSources4(); // mix different source types

            WriteSettingsToConsole( source );

            WriteLines();

            // Convert to xml
            ConvertTo( source, new DotNetConfigurationSource(), "xml", ".xml" );

            WriteLines();

            // Convert to .net xml
            ConvertTo( source, new DotNetConfigurationSource(), ".NET xml", ".config" );

            WriteLines();

            // Convert to ini file.
            ConvertTo( source, new IniConfigurationSource(), "ini", ".ini" );

            WriteLines();

            // Convert to encrypted ini file.
            var protectedSource = new IniConfigurationSource();
            protectedSource.Encrypt = true;
            protectedSource.EncryptionKey = "{42C418A0-5612-45a8-9A64-A63E761E7ACF}";
            ConvertTo( source, protectedSource, "encrypted ini", ".ini" );

            WriteLines();

            var encrypted = LoadProtectedIni( "{42C418A0-5612-45a8-9A64-A63E761E7ACF}", "encrypted ini", ".ini" );
            encrypted.Encrypt = false; // so we can see it.
            ConvertTo( encrypted, new IniConfigurationSource(), "unencrypted", ".ini" );

            Console.WriteLine( "Press enter to exit." );
            Console.ReadLine();
        }

        private static IConfigurationSource LoadProtectedIni( string key, string message, string extension )
        {
            Console.WriteLine( "Loading {0}:", message );
            var source = new IniConfigurationSource
                         {
                             Encrypt = true,
                             EncryptionKey = key,
                             FileName = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "composite" + extension )
                         };
            source.Reload();
            return source;
        }

        private static void WriteSettingsToConsole( IConfigurationSource source )
        {
            var networkSection = source.Sections["Network"];
            var url = networkSection.Get<string>( "Url" );
            Console.WriteLine( "Url: {0}", url );
            var uri = networkSection.Get<Uri>( "Url" );
            Console.WriteLine( "Uri: {0}", uri );
            var lastUpdateTime = networkSection.Get<DateTime>( "LastUpdate" );
            Console.WriteLine( "LastUpdate: {0}", lastUpdateTime );
            var lastUpdateDate = networkSection.Get<string>( "LastUpdate" );
            Console.WriteLine( "LastUpdate: {0}", lastUpdateDate );

            var dataSection = source.Sections["Data"];
            var logFile = dataSection.Get<string>( "LogFile" );
            Console.WriteLine( "logFile: {0}", logFile );
        }

        private static void ConvertTo( IConfigurationSource source, AbstractFileConfigurationSource destination,
                                       string name, string extension )
        {
            Console.WriteLine( "Convert to {0}:", name );
            destination.Merge( source );
            destination.FileName = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "composite" + extension );
            if ( File.Exists( destination.FileName ) )
            {
                File.Delete( destination.FileName );
            }
            destination.Save();
            using ( var file = File.OpenText( destination.FileName ) )
            {
                Console.WriteLine( file.ReadToEnd() );
            }
        }

        private static IEnumerable<string> GetFiles()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string defaultSettingsFile = Path.Combine( baseDirectory, "default" + Extension );
            string customSettingsFile = Path.Combine( baseDirectory, "custom" + Extension );
            string devSettingsFile = Path.Combine( baseDirectory, "dev" + Extension );
            yield return defaultSettingsFile;
            yield return customSettingsFile;
            yield return devSettingsFile;
        }

        private static IConfigurationSource LoadConfigurationSources1()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string defaultSettingsFile = Path.Combine( baseDirectory, "default" + Extension );
            string customSettingsFile = Path.Combine( baseDirectory, "custom" + Extension );
            string devSettingsFile = Path.Combine( baseDirectory, "dev" + Extension );

            IConfigurationSource defaultSettings = GetSource( defaultSettingsFile );
            IConfigurationSource customSettings = GetSource( customSettingsFile );
            IConfigurationSource devSettings = GetSource( devSettingsFile );

            defaultSettings.Merge( customSettings );
            defaultSettings.Merge( devSettings );
            defaultSettings.ExpandKeyValues();
            return defaultSettings;
        }

        private static IConfigurationSource LoadConfigurationSources2()
        {
            var source = GetSource();
            foreach ( string file in GetFiles() )
            {
                var settings = GetSource( file );
                source.Merge( settings );
            }
            source.ExpandKeyValues();
            return source;
        }

        private static IConfigurationSource LoadConfigurationSources3()
        {
            IConfigurationSource source = GetSource();
            GetFiles().ToList().ForEach( file => source.Merge( GetSource( file ) ) );
            source.ExpandKeyValues();
            return source;
        }

        // mix it up.
        private static IConfigurationSource LoadConfigurationSources4()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string defaultSettingsFile = Path.Combine( baseDirectory, "default.xml" );
            string customSettingsFile = Path.Combine( baseDirectory, "custom.ini" );
            string devSettingsFile = Path.Combine( baseDirectory, "dev.config" );

            IConfigurationSource defaultSettings = XmlConfigurationSource.FromFile( defaultSettingsFile );
            IConfigurationSource customSettings = IniConfigurationSource.FromFile( customSettingsFile );
            IConfigurationSource devSettings = DotNetConfigurationSource.FromFile( devSettingsFile );

            defaultSettings.Merge( customSettings );
            defaultSettings.Merge( devSettings );
            defaultSettings.ExpandKeyValues();
            return defaultSettings;
        }

        private static IConfigurationSource GetSource()
        {
            IConfigurationSource source;
            switch ( Extension )
            {
                case ".ini":
                    source = new IniConfigurationSource();
                    break;
                case ".xml":
                    source = new XmlConfigurationSource();
                    break;
                case ".config":
                    source = new DotNetConfigurationSource();
                    break;
                default:
                    source = new IniConfigurationSource();
                    break;
            }
            return source;
        }

        private static IConfigurationSource GetSource( string fileName )
        {
            IConfigurationSource source;
            switch ( Extension )
            {
                case ".ini":
                    source = IniConfigurationSource.FromFile( fileName );
                    break;
                case ".xml":
                    source = XmlConfigurationSource.FromFile( fileName );
                    break;
                case ".config":
                    source = DotNetConfigurationSource.FromFile( fileName );
                    break;
                default:
                    source = IniConfigurationSource.FromFile( fileName );
                    break;
            }
            return source;
        }
    }
}