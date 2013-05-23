#region Using Directives

using System;

#endregion

namespace Lucid.Configuration.Samples.CustomSettings
{
    /// <summary>
    /// Loads settings from the default.settings.ini (see the sample.cs) for the Network Section.
    /// </summary>
    public class NetworkSettings : SettingsFileBase
    {
        public NetworkSettings()
                : base( SettingsManager.ConfigurationSource, SettingsManager.Environment )
        {
        }

        public NetworkSettings( string environment )
                : base( SettingsManager.ConfigurationSource, environment )
        {
        }

        protected override string SectionName
        {
            get
            {
                // this would return the DLL name so that you can isolate your settings by dll
                //return base.SectionName;

                // but we just want a simple section name
                return "Network";
            }
        }

        public string Scheme
        {
            get { return Get( () => Scheme, "http" ); }
        }

        public string Domain
        {
            get { return Get( () => Domain, "www.time.gov" ); }
        }

        public string Query
        {
            get { return Get( () => Query, @"Eastern/d/-5" ); }
        }

        public string Action
        {
            get { return Get( () => Action, "timezone.cgi" ); }
        }

        public Uri Url
        {
            // this will use the expanded values from the config file.
            get { return Get( () => Url, null ); }
        }

        public Uri UrlLazy
        {
            get { return new Uri( string.Format( "{0}://{1}/{2}?{3}", Scheme, Domain, Action, Query ) ); }
        }

        public string LogFile
        {
            get { return Get( () => LogFile, null ); }
        }
        
        public string LogFileLazy
        {
            get { return string.Format( "{0}.xml", Scheme ); }
        }

        public Uri Server
        {
            // Get the server variable. If it doesn't exist, use the supplied default.
            get { return Get( () => Server, new Uri( "http://contoso.com" ) ); }
        }

        public string ServiceToHit
        {
            // get the service to hit variable, but the default depends on the environment. So 
            // we use our helper method to supply a different default for the various environments.
            get { return Get( () => ServiceToHit, GetDefault( "/api/dev", "/api/prod", "/api/prod" ) ); }
        }
    }
}