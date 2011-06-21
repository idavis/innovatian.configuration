#region Using Directives

using System;
using System.Collections.Generic;
using Innovatian.Configuration.Tests.Classes;
using Xunit;

#endregion

namespace Innovatian.Configuration.Tests
{
    public class ConfigurationSectionTests
    {
        private const string Key = "key";
        private const string SectionName = "Default";
        private const string Value = "value";

        [Fact]
        public void CanAddValue()
        {
            var section = new ConfigurationSection( SectionName );
            section.Set( Key, Value );
        }

        [Fact]
        public void CanReadAddedValue()
        {
            var section = new ConfigurationSection( SectionName );
            section.Set( Key, Value );
            var value = section.Get<string>( "key" );
            Assert.Equal( Value, value );
        }

        [Fact]
        public void CanReadAddedValueWithTryGet()
        {
            var section = new ConfigurationSection( SectionName );
            section.Set( Key, Value );
            string value;
            bool found = section.TryGet( "key", out value );
            Assert.True( found );
            Assert.Equal( Value, value );
        }

        [Fact]
        public void CanRemoveAddedValue()
        {
            var section = new ConfigurationSection( SectionName );
            section.Set( Key, Value );
            bool success = section.Remove( Key );
            Assert.True( success );
        }

        [Fact]
        public void RemovingUnAddedKeyFails()
        {
            var section = new ConfigurationSection( SectionName );
            bool success = section.Remove( Key );
            Assert.False( success );
        }

        [Fact]
        public void CreatingSectionWithNullNameFails()
        {
            Assert.Throws<ArgumentNullException>( () => new ConfigurationSection( null ) );
            Assert.Throws<ArgumentNullException>( () => new ConfigurationSection( string.Empty ) );
        }

        [Fact]
        public void NamePassedDuringCreationIsSet()
        {
            var section = new ConfigurationSection( SectionName );
            Assert.Equal( SectionName, section.Name );
        }

        [Fact]
        public void EnumeratorGivesKeysAndValues()
        {
            var section = new ConfigurationSection( SectionName );
            section.Set( Key, Value );
            int count = 0;
            foreach ( KeyValuePair<string, string> pair in section )
            {
                Assert.Equal( Key, pair.Key );
                Assert.Equal( Value, pair.Value );
                count++;
            }
            Assert.Equal( 1, count );
        }

        [Fact]
        public void SettingAValueForAnExistingKeyOverrides()
        {
            var section = new ConfigurationSection( SectionName );
            section.Set( Key, Value );
            Assert.Equal( Value, section.Get<string>( Key ) );
            section.Set( Key, Key );
            Assert.Equal( Key, section.Get<string>( Key ) );
        }

        [Fact]
        public void CanGetAndSetEnumValues()
        {
            const OSEnum value = OSEnum.Win2k;
            var section = new ConfigurationSection( SectionName );
            section.Set( Key, value );
            var fromSection = section.Get<OSEnum>( Key );
            Assert.Equal( value, fromSection );
        }

        [Fact]
        public void CanGetAndSetEnumStringValues()
        {
            const OSEnum value = OSEnum.Win2k;
            var section = new ConfigurationSection( SectionName );
            section.Set( Key, value.ToString() );
            var fromSection = section.Get<OSEnum>( Key );
            Assert.Equal( value, fromSection );
        }

        [Fact]
        public void CanGetAndSetEnumInt32Values()
        {
            const OSEnum value = OSEnum.Win2k;
            var section = new ConfigurationSection( SectionName );
            section.Set( Key, (int) value );
            var fromSection = section.Get<OSEnum>( Key );
            Assert.Equal( value, fromSection );
        }

        [Fact]
        public void CanGetAndSetEnumFlagValues()
        {
            const OptionsEnum all = ( OptionsEnum.A | OptionsEnum.B | OptionsEnum.C );
            var section = new ConfigurationSection( SectionName );
            section.Set( Key, all );
            var fromSection = section.Get<OptionsEnum>( Key );
            Assert.Equal( all, fromSection );
        }

        [Fact]
        public void CanGetAndSetEnumFlagStringValues()
        {
            const OptionsEnum all = ( OptionsEnum.A | OptionsEnum.B | OptionsEnum.C );
            var section = new ConfigurationSection( SectionName );
            section.Set( Key, all.ToString() );
            var fromSection = section.Get<OptionsEnum>( Key );
            Assert.Equal( all, fromSection );
        }

        [Fact]
        public void CanGetAndSetEnumFlagInt32Values()
        {
            const OptionsEnum all = ( OptionsEnum.A | OptionsEnum.B | OptionsEnum.C );
            var section = new ConfigurationSection( SectionName );
            section.Set( Key, (int) all );
            var fromSection = section.Get<OptionsEnum>( Key );
            Assert.Equal( all, fromSection );
        }

        [Fact]
        public void GettingNonExistingItemReturnsDefaultForType()
        {
            var section = new ConfigurationSection( SectionName );
            var optionsEnum = section.Get<OptionsEnum>( Key );
            Assert.Equal( default( OptionsEnum ), optionsEnum );

            var osEnum = section.Get<OSEnum>( Key );
            Assert.Equal( default( OSEnum ), osEnum );

            var stringValue = section.Get<string>( Key );
            Assert.Equal( default( string ), stringValue );

            var boolValue = section.Get<bool>( Key );
            Assert.Equal( default( bool ), boolValue );

            var dummySection = section.Get<ConfigurationSection>( Key );
            Assert.Equal( default( ConfigurationSection ), dummySection );
        }

        [Fact]
        public void UsingTryGetForNonExistingItemReturnsFalseAndSetsTheOutParamToTheDefaultForType()
        {
            var section = new ConfigurationSection( SectionName );

            OptionsEnum optionsEnum;
            bool found = section.TryGet( Key, out optionsEnum );
            Assert.False( found );
            Assert.Equal( default( OptionsEnum ), optionsEnum );

            OSEnum osEnum;
            found = section.TryGet( Key, out osEnum );
            Assert.False( found );
            Assert.Equal( default( OSEnum ), osEnum );

            string stringValue;
            found = section.TryGet( Key, out stringValue );
            Assert.False( found );
            Assert.Equal( default( string ), stringValue );

            bool boolValue;
            found = section.TryGet( Key, out boolValue );
            Assert.False( found );
            Assert.Equal( default( bool ), boolValue );

            ConfigurationSection dummySection;
            found = section.TryGet( Key, out dummySection );
            Assert.False( found );
            Assert.Equal( default( ConfigurationSection ), dummySection );
        }

        [Fact]
        public void DefaultParameterValueIsReturnedForNonExistingKey()
        {
            var section = new ConfigurationSection( SectionName );

            const OptionsEnum optionsEnumDefault = OptionsEnum.None;
            OptionsEnum optionsEnum = section.Get( Key, optionsEnumDefault );
            Assert.Equal( optionsEnumDefault, optionsEnum );

            const OSEnum osEnumDefault = OSEnum.WinXp;
            OSEnum osEnum = section.Get( Key, osEnumDefault );
            Assert.Equal( osEnumDefault, osEnum );

            const string stringDefault = Value;
            string stringValue = section.Get( Key, stringDefault );
            Assert.Equal( stringDefault, stringValue );

            bool boolValue = section.Get( Key, true );
            Assert.Equal( true, boolValue );

            boolValue = section.Get( Key, false );
            Assert.Equal( false, boolValue );

            var defaultSection = new ConfigurationSection( "MyDefault" );
            ConfigurationSection dummySection = section.Get( Key, defaultSection );
            Assert.Equal( defaultSection, dummySection );
        }

        [Fact]
        public void SectionsCanBeComparedForEquality()
        {
            IConfigurationSection section1 = SectionGenerator.GetSingleSection();
            IConfigurationSection section2 = SectionGenerator.GetSingleSection();
            Assert.Equal( section1, section2 );
        }
    }
}