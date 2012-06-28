#region License

// 
// Copyright (c) 2009-2012, Ian Davis <ian.f.davis@gmail.com>
// 
// Dual-licensed under the Apache License, Version 2.0, and the Microsoft Public License (Ms-PL).
// See the file LICENSE.txt for details.
// 

#endregion

#region Using Directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

#endregion

namespace Innovatian.Configuration
{
    /// <summary>
    /// This class should only be used for initial configuration and by inheriting <see cref="SettingsBase"/> implementations.
    /// </summary>
    public static class SettingsManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="defaultConfigFileExtension"></param>
        /// <param name="globalSectionName"></param>
        /// <param name="environmentVariableName"></param>
        /// <param name="defaultEnvironment"></param>
        public static void Initialize(string defaultConfigFileExtension = ".ini", string globalSectionName = "Global", string environmentVariableName = "Environment", string defaultEnvironment = "dev")
        {
            DefaultConfigFileExtension = defaultConfigFileExtension;
            GlobalSectionName = globalSectionName;
            EnvironmentVariableName = environmentVariableName;
            DefaultEnvironment = defaultEnvironment;
            ConfigurationSource = new InMemoryConfigurationSource();
        }

        /// <summary>
        ///   The file extension used to look for the base config file. For example, if .ini is chosen, then the settings
        ///   system will look for default.settings.ini. The same follows for .xml with default.settings.xml, and .config with
        ///   default.settings.config.
        /// </summary>
        public static string DefaultConfigFileExtension { get; private set; }

        /// <summary>
        ///   The section which contains the global configuration including the <see cref="EnvironmentVariableName" /> entry.
        /// </summary>
        public static string GlobalSectionName { get; private set; }

        /// <summary>
        ///   The variable name in the <see cref="GlobalSectionName" /> which contains the environment to use. This
        ///   name is also used to search the environment variables if it is not set in the default config file.
        /// </summary>
        public static string EnvironmentVariableName { get; private set; }

        /// <summary>
        ///   The default environment to load if no environment is specified in the default config file or environment variable.
        /// </summary>
        public static string DefaultEnvironment { get; private set; }

        /// <summary>
        ///   The configured environment as loaded from the configuration files or environment variable.
        /// </summary>
        public static string Environment { get; private set; }

        public static IConfigurationSource ConfigurationSource { get; private set; }

        #region Nested type: InMemoryConfigurationSource

        private sealed class InMemoryConfigurationSource : AbstractConfigurationSource
        {
            private static readonly IDictionary<string, Func<string, IConfigurationSource>> Factories
                    = new Dictionary<string, Func<string, IConfigurationSource>>
                    {
                            { ".ini", IniConfigurationSource.FromFile },
                            { ".config", DotNetConfigurationSource.FromFile },
                            { ".xml", XmlConfigurationSource.FromFile }
                    };

            public InMemoryConfigurationSource()
            {
                Reload();
            }

            #region Overrides of AbstractConfigurationSource

            /// <summary>
            ///   Saves all sections. All data merged from other merged sources will
            ///   be included.
            /// </summary>
            public override void Save()
            {
                // do nothing
            }

            /// <summary>
            ///   Discards all sections and merged sources and reloads a fresh set of
            ///   settings.
            /// </summary>
            public override void Reload()
            {
                IConfigurationSource defaultSettings;
                bool defaultSettingsLoaded = TryLoadConfigurationFile( DefaultConfigurationFile, out defaultSettings );

                if ( defaultSettingsLoaded && defaultSettings.Sections.ContainsKey( GlobalSectionName ) )
                {
                    IConfigurationSection section = defaultSettings.Sections[GlobalSectionName];
                    ConfigureEnvironment( section );
                }
                else
                {
                    ConfigureEnvironmentFromUserVariable();
                }

                if ( defaultSettingsLoaded )
                {
                    Merge( defaultSettings );
                }

                IConfigurationSource environmentSettings;
                if ( TryLoadConfigurationFile( EnvironmentConfigurationFile, out environmentSettings ) )
                {
                    Merge( environmentSettings );
                }

                ExpandKeyValues();
            }

            private static bool TryLoadConfigurationFile( string fileName, out IConfigurationSource configurationSource )
            {
                if ( string.IsNullOrEmpty( fileName ) )
                {
                    throw new ArgumentNullException( "fileName" );
                }

                if ( !File.Exists( fileName ) )
                {
                    configurationSource = null;
                    return false;
                }
                var fileInfo = new FileInfo( fileName );
                if ( !Factories.ContainsKey( fileInfo.Extension ) )
                {
                    configurationSource = null;
                    return false;
                }
                configurationSource = Factories[fileInfo.Extension]( fileName );
                return true;
            }

            private static void ConfigureEnvironment( IConfigurationSection section )
            {
                string environment;
                if ( section.TryGet( EnvironmentVariableName, out environment ) &&
                     !string.IsNullOrEmpty( environment ) )
                {
                    Environment = environment;
                }
                else
                {
                    ConfigureEnvironmentFromUserVariable();
                }
            }

            #endregion

            private string DefaultConfigurationFile
            {
                get
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    var uri = new Uri( assembly.CodeBase );
                    string baseDirectory = Directory.GetParent( uri.LocalPath ).FullName;
                    string defaultSettingsFile = Path.Combine( baseDirectory,
                                                               "default.settings" + DefaultConfigFileExtension );
                    return defaultSettingsFile;
                }
            }

            private string EnvironmentConfigurationFile
            {
                get
                {
                    string baseDirectory = Directory.GetParent( Assembly.GetExecutingAssembly().Location ).FullName;
                    string envSettingsFile = Path.Combine( baseDirectory,
                                                           Environment + ".settings" + DefaultConfigFileExtension );
                    return envSettingsFile;
                }
            }

            private static void ConfigureEnvironmentFromUserVariable()
            {
                IDictionary variables = System.Environment.GetEnvironmentVariables( EnvironmentVariableTarget.Machine );
                Environment = variables.Contains( EnvironmentVariableName )
                                      ? variables[EnvironmentVariableName].ToString().Trim()
                                      : DefaultEnvironment;
            }
        }

        #endregion
    }
}