#region Using Directives

using System.Collections.Generic;
using Innovatian.Configuration.Tests.Classes;
using Xunit;

#endregion

namespace Innovatian.Configuration.Tests
{
    public class XmlConfigurationSourceTests
    {
        [Fact]
        public void CanParseSingleSection()
        {
            IConfigurationSection section = SectionGenerator.GetSingleSection();

            string xml = XmlConfigurationSource.ToXml( new[] {section} );

            var source = new XmlConfigurationSource( xml );
            Assert.Equal( 1, source.Sections.Count );
            Assert.NotNull( source.Sections["Default"] );
            int count = 0;
            foreach ( IConfigurationSection configurationSection in source )
            {
                foreach ( KeyValuePair<string, string> pair in configurationSection )
                {
                    Assert.Equal( pair.Value, section.Get<string>( pair.Key ) );
                    count++;
                }

                foreach ( KeyValuePair<string, string> pair in section )
                {
                    Assert.Equal( pair.Value, configurationSection.Get<string>( pair.Key ) );
                    count++;
                }
            }
            Assert.Equal( 10, count );
        }

        [Fact]
        public void CanParseMultipleSections()
        {
            string xml = XmlConfigurationSource.ToXml( SectionGenerator.GetThreeSections() );

            var source = new XmlConfigurationSource( xml );
            Assert.Equal( 3, source.Sections.Count );
            Assert.NotNull( source.Sections["Default"] );
            Assert.NotNull( source.Sections["Default2"] );
            Assert.NotNull( source.Sections["Default3"] );

            int count = 0;
            foreach ( IConfigurationSection configurationSection in source )
            {
                foreach ( KeyValuePair<string, string> pair in configurationSection )
                {
                    Assert.Equal( pair.Value,
                                  source.Sections[configurationSection.Name].Get<string>( pair.Key ) );
                    count++;
                }

                foreach ( KeyValuePair<string, string> pair in source.Sections[configurationSection.Name] )
                {
                    Assert.Equal( pair.Value, configurationSection.Get<string>( pair.Key ) );
                    count++;
                }
            }
            Assert.Equal( 10, count );
        }

        [Fact]
        public void CanLoadFromFile()
        {
            string xml = XmlConfigurationSource.ToXml( SectionGenerator.GetThreeSections() );

            var source = new XmlConfigurationSource( xml ) {FileName = "CanLoadFromFile.xml"};
            source.Save();

            var sourceFromFile = XmlConfigurationSource.FromFile( "CanLoadFromFile.xml" );
            string sourceString = source.ToString();
            string sourceFromFileString = sourceFromFile.ToString();
            Assert.Equal( sourceString, sourceFromFileString );
        }
    }
}