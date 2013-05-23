#region Using Directives

using System;
using Lucid.Configuration.Tests.Classes;
using NUnit;

#endregion

namespace Lucid.Configuration.Tests
{
    [TestFixture]
    public class ConfigurationSourceBaseTests
    {
        private const string Key = "key";
        private const string SectionName = "Default";
        private const string Value = "value";

        protected static ConfigurationSourceBaseImpl GetConfigurationSource()
        {
            var section = new ConfigurationSection( SectionName );
            var source = new ConfigurationSourceBaseImpl {section};
            section.Set( Key, Value );
            return source;
        }

        [Test]
        public void MergingWithNullCollectionThrows()
        {
            ConfigurationSourceBaseImpl source = GetConfigurationSource();
            Assert.Throws<ArgumentNullException>( () => source.Merge( null ) );
        }

        [Test]
        public void MergingWithCollectionAddsConfigurationSection()
        {
            ConfigurationSourceBaseImpl source = GetConfigurationSource();
            const string sectionName = SectionName + Key;
            var section = new ConfigurationSection( sectionName );
            section.Set( Key, Key );
            var source2 = new ConfigurationSourceBaseImpl {section};
            source.Merge( new[] {source2} );
            Assert.Contains( section, source );
        }

        [Test]
        public void MergingWithCollectionOverwritesExistingKeys()
        {
            ConfigurationSourceBaseImpl source = GetConfigurationSource();
            var section = new ConfigurationSection( SectionName );
            section.Set( Key, Key );
            var source2 = new ConfigurationSourceBaseImpl {section};
            Assert.Equal( Value, source.Sections[SectionName].Get<string>( Key ) );
            source.Merge( new[] {source2} );
            Assert.Equal( Key, source.Sections[SectionName].Get<string>( Key ) );
        }

        [Test]
        public void MergingWithAleadyMergedSourceDoesNothing()
        {
            ConfigurationSourceBaseImpl source = GetConfigurationSource();
            ConfigurationSourceBaseImpl newSource = GetConfigurationSource();

            Assert.Equal( 0, source.ConfigurationSources.Count );

            source.Merge( new[] {newSource} );
            Assert.Equal( 1, source.ConfigurationSources.Count );
            Assert.Equal( newSource, source.ConfigurationSources[0] );

            source.Merge( new[] {newSource} );
            Assert.Equal( 1, source.ConfigurationSources.Count );
            Assert.Equal( newSource, source.ConfigurationSources[0] );
        }

        [Test]
        public void AddingNewSectionOverridesKeys()
        {
            ConfigurationSourceBaseImpl source = GetConfigurationSource();
            var section = new ConfigurationSection( SectionName );
            section.Set( Key, Key );

            Assert.Equal( Value, source.Sections[SectionName].Get<string>( Key ) );
            source.Add( section );
            Assert.Equal( Key, source.Sections[SectionName].Get<string>( Key ) );
        }

        [Test]
        public void AddingNullSectionThrows()
        {
            ConfigurationSourceBaseImpl source = GetConfigurationSource();
            Assert.Throws<ArgumentNullException>( () => source.Add( null ) );
        }

        [Test]
        public void ExpandSimpleWorks()
        {
            ConfigurationSourceBaseImpl source = GetConfigurationSource();
            source.Sections[SectionName].Set( Key, "${value}" );
            source.Sections[SectionName].Set( Value, Key );

            Assert.Equal( "${value}", source.Sections[SectionName].Get<string>( Key ) );
            source.ExpandKeyValues();
            Assert.Equal( Key, source.Sections[SectionName].Get<string>( Key ) );
        }

        [Test]
        public void ExpandBackToBackWorks()
        {
            ConfigurationSourceBaseImpl source = GetConfigurationSource();
            source.Sections[SectionName].Set( Key, "${value}${value}" );
            source.Sections[SectionName].Set( Value, Key );

            Assert.Equal( "${value}${value}", source.Sections[SectionName].Get<string>( Key ) );
            source.ExpandKeyValues();
            Assert.Equal( Key + Key, source.Sections[SectionName].Get<string>( Key ) );
        }

        [Test]
        public void ExpandBackToBackWithSpaceWorks()
        {
            ConfigurationSourceBaseImpl source = GetConfigurationSource();
            source.Sections[SectionName].Set( Key, "${value} ${value}" );
            source.Sections[SectionName].Set( Value, Key );

            Assert.Equal( "${value} ${value}", source.Sections[SectionName].Get<string>( Key ) );
            source.ExpandKeyValues();
            Assert.Equal( Key + " " + Key, source.Sections[SectionName].Get<string>( Key ) );
        }

        [Test]
        public void ExpandFromExternalSectionWorks()
        {
            const string newSectionName = SectionName + "New";
            var section = new ConfigurationSection( newSectionName );
            section.Set( Value, Value );

            ConfigurationSourceBaseImpl source = GetConfigurationSource();
            const string varKeyValue = "${" + newSectionName + "|value}";
            source.Sections[SectionName].Set( Key, varKeyValue );

            source.Add( section );

            Assert.Equal( varKeyValue, source.Sections[SectionName].Get<string>( Key ) );
            source.ExpandKeyValues();
            Assert.Equal( Value, source.Sections[SectionName].Get<string>( Key ) );
        }

        [Test]
        public void ExpandFromDoubleExternalSectionWorks()
        {
            // old -> new -> dev -> key : value
            const string devSectionName = SectionName + "Dev";
            const string newSectionName = SectionName + "New";

            var devSection = new ConfigurationSection( devSectionName );
            var newSection = new ConfigurationSection( newSectionName );
            const string varKeyValue = "${" + newSectionName + "|key}";

            devSection.Set( Key, Value );
            newSection.Set( Key, "${" + devSectionName + "|key}" );

            ConfigurationSourceBaseImpl source = GetConfigurationSource();

            source.Sections[SectionName].Set( Key, varKeyValue );

            source.Add( newSection );
            source.Add( devSection );

            Assert.Equal( varKeyValue, source.Sections[SectionName].Get<string>( Key ) );
            source.ExpandKeyValues();
            Assert.Equal( Value, source.Sections[SectionName].Get<string>( Key ) );
        }

        [Test]
        public void SaveIsCalledOnPropertyChangeWithAutoSaveEnabled()
        {
            var source = GetConfigurationSource();
            source.AutoSave = true;
            Assert.Throws<NotSupportedException>( () => source.Sections[SectionName].Set( Key, Key ) );
            Assert.Equal( source.Sections[SectionName].Get<string>( Key ), Key );
        }

        [Test]
        public void SaveIsNotCalledOnPropertyChangeWithAutoSaveDisabled()
        {
            var source = GetConfigurationSource();
            source.AutoSave = false;
            source.Sections[SectionName].Set( Key, Key );
            Assert.Equal( source.Sections[SectionName].Get<string>( Key ), Key );
        }

        [Test]
        public void ClearRemovesAllSectionsAndClearsAllSettingsInSections()
        {
            var source = GetConfigurationSource();
            source.Merge( new[] {GetConfigurationSource()} );
            Assert.Equal( 1, source.ConfigurationSources.Count );
            var configSource = source.ConfigurationSources[0];
            Assert.Equal( 1, configSource.Sections.Count );
            Assert.Equal( 1, source.Sections.Count );
            var configSection = source.Sections[SectionName];
            Assert.Equal( 1, configSection.Count );
            source.Clear();
            Assert.Equal( 0, source.ConfigurationSources.Count );
            Assert.Equal( 0, configSource.Sections.Count );
            Assert.Equal( 0, source.Sections.Count );
            Assert.Equal( 0, configSection.Count );
        }
    }
}