#region Using Directives

using System;
using Lucid.Configuration.Tests.Classes;
using NUnit;

#endregion

namespace Lucid.Configuration.Tests
{
    [TestFixture]
    public class SettingsConverterTests
    {
        [Test]
        public void CanGetEnumValueByName()
        {
            const string none = "None";
            var value = SettingConverter.GetTFromString<OSEnum>( none );
            Assert.Equal( OSEnum.None, value );
        }

        [Test]
        public void CanGetEnumValueByValue()
        {
            const string none = "0";
            var value = SettingConverter.GetTFromString<OSEnum>( none );
            Assert.Equal( OSEnum.None, value );
        }

        [Test]
        public void CanGetEnumValueByFlagValue()
        {
            const OptionsEnum all = ( OptionsEnum.A | OptionsEnum.B | OptionsEnum.C );
            string allString = ( (int) all ).ToString();
            var value = SettingConverter.GetTFromString<OptionsEnum>( allString );
            Assert.Equal( all, value );
        }

        [Test]
        public void CanGetEnumValueByFlagString()
        {
            const OptionsEnum all = ( OptionsEnum.A | OptionsEnum.B | OptionsEnum.C );
            string allString = all.ToString();
            var value = SettingConverter.GetTFromString<OptionsEnum>( allString );
            Assert.Equal( all, value );
        }

        [Test]
        public void CanGetBoolFromTrueString()
        {
            string boolString = bool.TrueString;
            var boolValue = SettingConverter.GetTFromString<bool>( boolString );
            Assert.True( boolValue );
        }

        [Test]
        public void CanGetBoolFromFalseString()
        {
            string boolString = bool.FalseString;
            var boolValue = SettingConverter.GetTFromString<bool>( boolString );
            Assert.False( boolValue );
        }

        [Test]
        public void CanGetBoolFromTrueStringLowerCase()
        {
            string boolString = bool.TrueString.ToLower();
            var boolValue = SettingConverter.GetTFromString<bool>( boolString );
            Assert.True( boolValue );
        }

        [Test]
        public void CanGetBoolFromFalseStringLowerCase()
        {
            string boolString = bool.FalseString.ToLower();
            var boolValue = SettingConverter.GetTFromString<bool>( boolString );
            Assert.False( boolValue );
        }

        [Test]
        public void CanGetBoolFromIntToTrue()
        {
            string boolString = 1.ToString();
            var boolValue = SettingConverter.GetTFromString<bool>( boolString );
            Assert.True( boolValue );
        }

        [Test]
        public void CanGetBoolFromIntToFalse()
        {
            string boolString = 0.ToString();
            var boolValue = SettingConverter.GetTFromString<bool>( boolString );
            Assert.False( boolValue );
        }

        [Test]
        public void CanGetInt()
        {
            string now = 5.ToString();
            var value = SettingConverter.GetTFromString<int>( now );
            Assert.Equal( now, value.ToString() );
        }

        [Test]
        public void CanGetStringInt()
        {
            string now = 5.ToString();
            var value = SettingConverter.GetStringFromT( 5 );
            Assert.Equal( now, value );
        }

        [Test]
        public void CanGetDateTime()
        {
            string now = DateTime.Now.ToString();
            var value = SettingConverter.GetTFromString<DateTime>( now );
            Assert.Equal( now, value.ToString() );
        }

        [Test]
        public void CanGetUri()
        {
            const string url = "http://mydomain.com/";
            var value = SettingConverter.GetTFromString<Uri>( url );
            Assert.Equal( url, value.ToString() );
        }
    }
}