#region Using Directives

using System.Collections.Generic;

#endregion

namespace Innovatian.Configuration.Tests.Classes
{
    internal static class SectionGenerator
    {
        public static IConfigurationSection GetSingleSection()
        {
            IConfigurationSection section = new ConfigurationSection( "Default" );
            section.Set( "a", "a" );
            section.Set( "b", "b" );
            section.Set( "c", "c" );
            section.Set( "d", "d" );
            section.Set( "e", "e" );
            return section;
        }

        public static IEnumerable<IConfigurationSection> GetTwoSections()
        {
            IConfigurationSection section = new ConfigurationSection( "Default" );
            section.Set( "a", "a" );
            section.Set( "b", "b" );
            IConfigurationSection section2 = new ConfigurationSection( "Default2" );
            section2.Set( "c", "c" );
            section2.Set( "d", "d" );
            section2.Set( "e", "e" );
            return new[] {section, section2};
        }

        public static IEnumerable<IConfigurationSection> GetThreeSections()
        {
            IConfigurationSection section = new ConfigurationSection( "Default" );
            section.Set( "a", "a" );
            section.Set( "b", "b" );
            IConfigurationSection section2 = new ConfigurationSection( "Default2" );
            section2.Set( "c", "c" );
            section2.Set( "d", "d" );
            IConfigurationSection section3 = new ConfigurationSection( "Default3" );
            section3.Set( "e", "e" );
            return new[] {section, section2, section3};
        }
    }
}