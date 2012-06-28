#region Using Directives

using System;

#endregion

namespace Innovatian.Configuration.Samples.CustomSettings
{
    /// <summary>
    ///   You would create this class once for your application and all of your settings files will derive from it.
    ///   You don't have to do this, but we are adding a very helpful method that is unique to each consumer.
    /// </summary>
    public class SettingsFileBase : SettingsBase
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="SettingsFileBase" /> class.
        /// </summary>
        /// <remarks>
        ///   This ctor will initialize itself with the global settings system for the environment that is 
        ///   currently configured.
        /// </remarks>
        protected SettingsFileBase()
                : base( SettingsManager.ConfigurationSource, SettingsManager.Environment )
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SettingsFileBase" /> class.
        /// </summary>
        /// <param name="environment"> The environment in which to pull settings from. </param>
        /// <remarks>
        ///   This ctor will initialize itself with the global settings system.
        /// </remarks>
        protected SettingsFileBase( string environment )
                : base( SettingsManager.ConfigurationSource, environment )
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SettingsBase" /> class.
        /// </summary>
        /// <param name="configurationSource"> The configuration source which contains the section in which this class will pull its settings. </param>
        /// <param name="environment"> The environment in which to pull settings from. </param>
        /// <exception cref="ArgumentNullException">if
        ///   <paramref name="configurationSource" />
        ///   is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">if
        ///   <paramref name="environment" />
        ///   is not one of the defined values.</exception>
        protected SettingsFileBase( IConfigurationSource configurationSource, string environment )
                : base( configurationSource, environment )
        {
        }

        /// <summary>
        ///   Implement a custom method. We are going to have three different environments in our sample:
        ///   Dev, Stage, Prod
        /// </summary>
        /// <typeparam name="T"> </typeparam>
        /// <returns> </returns>
        protected virtual T GetDefault<T>( T devValue, T stageValue, T prodValue )
        {
            switch ( Environment.ToUpperInvariant() )
            {
                case "DEV":
                    return devValue;
                case "STAGE":
                    return stageValue;
                case "PROD":
                    return prodValue;
                default:
                    return devValue;
            }
        }
    }
}