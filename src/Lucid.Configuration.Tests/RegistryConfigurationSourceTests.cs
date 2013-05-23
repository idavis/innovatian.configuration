#region Using Directives

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;
using NUnit;

#endregion

namespace Lucid.Configuration.Tests
{
    [TestFixture]
    public class RegistryConfigurationSourceTests
    {
        private const int dWordValue = 42;
        private const string expandedStringValue = "The path is %PATH%";
        private const string KeyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Management";
        private const string LocalMachineRoot = @"HKEY_LOCAL_MACHINE";
        private const long quadWordValue = 42;
        private const string stringValue = "The path is %PATH%";
        private static readonly string TestKey = @"SOFTWARE\Lucid.Configuration.Tests";
        private readonly byte[] binaryValue = new byte[] {10, 43, 44, 45, 14, 255};
        private readonly string[] multipleStringValue = new[] {"One", "Two", "Three"};

        private static string SectionName
        {
            get
            {
                var stackTrace = new StackTrace( 1 );
                var currentMethod = stackTrace.GetFrame( 0 ).GetMethod().Name;
                return currentMethod;
            }
        }

        private static string TestKeyName
        {
            get
            {
                var stackTrace = new StackTrace( 1 );
                var currentMethod = stackTrace.GetFrame( 0 ).GetMethod().Name;
                var key = string.Format( "{0}\\{1}", TestKey, currentMethod );
                return key;
            }
        }

        [Test]
        public void CanReadRegistryGivenKey()
        {
            using ( RegistryKey key =
                Registry.LocalMachine.OpenSubKey( KeyName ) )
            {
                using ( var source = new RegistryConfigurationSource( key ) )
                {
                }
            }
        }

        [Test]
        public void CanReadRegistryGivenKeyName()
        {
            using (
                var source = new RegistryConfigurationSource( string.Format( "{0}\\{1}", LocalMachineRoot, KeyName ) ) )
            {
            }
        }

        [Test]
        public void CanAddNewKeys()
        {
            try
            {
                using ( var source = new RegistryConfigurationSource( FullTestKeyName( TestKeyName ) ) )
                {
                    source.Sections[SectionName].Set( "key", "value" );
                    source.Add( new ConfigurationSection( "NewSettings" ) );
                    source.Sections["NewSettings"].Set( "count", 5 );
                    source.Save();
                }

                var root = TestKeyName;
                Assert.True( KeyExists( root ) );
                Assert.False( KeyExists( string.Format( "{0}\\{1}", root, SectionName ) ) );
                Assert.True( KeyExists( root + "\\NewSettings" ) );
            }
            finally
            {
                DeleteKey( TestKeyName );
            }
        }

        [Test]
        public void CanLoadKeys()
        {
            try
            {
                using ( var source = new RegistryConfigurationSource( FullTestKeyName( TestKeyName ) ) )
                {
                    source.Sections[SectionName].Set( "key", "value" );
                    source.Add( new ConfigurationSection( "NewSettings" ) );
                    source.Sections["NewSettings"].Set( "count", 5 );
                    source.Save();
                }
                using ( var source = new RegistryConfigurationSource( FullTestKeyName( TestKeyName ) ) )
                {
                    Assert.Equal( 2, source.Sections.Count );

                    Assert.Equal( "NewSettings", source.Sections.ToList()[0].Value.Name );
                    Assert.Equal( 5, source.Sections["NewSettings"].Get<int>( "count" ) );

                    Assert.Equal( SectionName, source.Sections.ToList()[1].Value.Name );
                    Assert.Equal( "value", source.Sections[SectionName].Get<string>( "key" ) );
                }
            }
            finally
            {
                DeleteKey( TestKeyName );
            }
        }

        [Test]
        public void CanLoadMultiLevelKeys()
        {
            try
            {
                using ( var source = new RegistryConfigurationSource( FullTestKeyName( TestKeyName ) ) )
                {
                    source.Sections[SectionName].Set( "key", "value" );
                    source.Add( new ConfigurationSection( "NewSettings" ) );
                    source.Sections["NewSettings"].Set( "count", 5 );
                    source.Add( new ConfigurationSection( "NewSettings\\Legacy" ) );
                    source.Sections["NewSettings\\Legacy"].Set( "count", 15 );
                    source.Save();
                }
                using ( var source = new RegistryConfigurationSource( FullTestKeyName( TestKeyName ) ) )
                {
                    Assert.Equal( 3, source.Sections.Count );

                    Assert.Equal( "NewSettings\\Legacy", source.Sections.ToList()[0].Value.Name );
                    Assert.Equal( 15, source.Sections["NewSettings\\Legacy"].Get<int>( "count" ) );

                    Assert.Equal( "NewSettings", source.Sections.ToList()[1].Value.Name );
                    Assert.Equal( 5, source.Sections["NewSettings"].Get<int>( "count" ) );

                    Assert.Equal( SectionName, source.Sections.ToList()[2].Value.Name );
                    Assert.Equal( "value", source.Sections[SectionName].Get<string>( "key" ) );
                }
            }
            finally
            {
                DeleteKey( TestKeyName );
            }
        }

        [Test]
        public void CanAddMultiLevelKeys()
        {
            try
            {
                using ( var source = new RegistryConfigurationSource( FullTestKeyName( TestKeyName ) ) )
                {
                    source.Sections[SectionName].Set( "key", "value" );
                    source.Add( new ConfigurationSection( "NewSettings" ) );
                    source.Sections["NewSettings"].Set( "count", 5 );
                    source.Add( new ConfigurationSection( "NewSettings\\Legacy" ) );
                    source.Sections["NewSettings\\Legacy"].Set( "count", 5 );
                    source.Save();
                }

                var root = TestKeyName;
                Assert.True( KeyExists( root ) );
                Assert.False( KeyExists( string.Format( "{0}\\{1}", root, SectionName ) ) );
                Assert.True( KeyExists( root + "\\NewSettings" ) );
                Assert.True( KeyExists( root + "\\NewSettings\\Legacy" ) );
            }
            finally
            {
                DeleteKey( TestKeyName );
            }
        }

        [Test]
        public void CanReadRegistryValueKinds()
        {
            try
            {
                CreateRegistryValueKindSamples( TestKey, SectionName );

                using ( var source = new RegistryConfigurationSource( FullTestKeyName( TestKeyName ) ) )
                {
                    var section = source.Sections[SectionName];

                    var quadWord = section.Get<long>( "QuadWordValue" );
                    Assert.Equal( quadWordValue, quadWord );

                    var dWord = section.Get<int>( "DWordValue" );
                    Assert.Equal( quadWordValue, dWord );

                    var strings = section.Get<string[]>( "MultipleStringValue" );
                    Assert.Equal( multipleStringValue, strings );

                    var newStringValue = section.Get<string>( "StringValue" );
                    Assert.Equal( stringValue, newStringValue );

                    var newExpandedStringValue = section.Get<string>( "ExpandedStringValue" );
                    Assert.NotEqual( expandedStringValue, newExpandedStringValue );
                    var realExpandedValue = expandedStringValue.Replace( "%PATH%",
                                                                         Environment.GetEnvironmentVariable( "PATH" ) );
                    Assert.Equal( realExpandedValue, newExpandedStringValue );

                    var data = section.Get<byte[]>( "BinaryValue" );
                    Assert.Equal( binaryValue, data );
                }
            }
            finally
            {
                DeleteKey( TestKeyName );
            }
        }

        [Test]
        public void CanWriteRegistryValueKinds()
        {
            try
            {
                CreateRegistryValueKindSamples( TestKey, SectionName );

                using ( var source = new RegistryConfigurationSource( FullTestKeyName( TestKeyName ) ) )
                {
                    var section = source.Sections[SectionName];
                    section.Set( "QuadWordValue", 13 );
                    section.Set( "DWordValue", 13 );
                    section.Set( "StringValue", 13.ToString() );
                    section.Set( "ExpandedStringValue", "13 %PATH%" );
                    section.Set( "MultipleStringValue", new[] {13.ToString(), 13.ToString()} );
                    section.Set( "BinaryValue", new byte[] {13, 13} );
                    source.Save();
                }
                using ( var source = new RegistryConfigurationSource( FullTestKeyName( TestKeyName ) ) )
                {
                    var section = source.Sections[SectionName];
                    var quadWord = section.Get<long>( "QuadWordValue" );
                    Assert.Equal( 13, quadWord );

                    var dWord = section.Get<int>( "DWordValue" );
                    Assert.Equal( 13, dWord );

                    var strings = section.Get<string[]>( "MultipleStringValue" );
                    Assert.Equal( new[] {13.ToString(), 13.ToString()}, strings );

                    var newStringValue = section.Get<string>( "StringValue" );
                    Assert.Equal( 13.ToString(), newStringValue );

                    var newExpandedStringValue = section.Get<string>( "ExpandedStringValue" );
                    Assert.NotEqual( expandedStringValue, newExpandedStringValue );
                    var realExpandedValue = "13 %PATH%".Replace( "%PATH%",
                                                                 Environment.GetEnvironmentVariable( "PATH" ) );
                    Assert.Equal( realExpandedValue, newExpandedStringValue );

                    var data = section.Get<byte[]>( "BinaryValue" );
                    Assert.Equal( new byte[] {13, 13}, data );
                }
            }
            finally
            {
                DeleteKey( TestKeyName );
            }
        }

        private void CreateRegistryValueKindSamples( string testKey, string sectionName )
        {
            using ( var key = Registry.LocalMachine.OpenSubKey( testKey, true ) )
            {
                using (
                    var sectionKey = key.CreateSubKey( sectionName, RegistryKeyPermissionCheck.ReadWriteSubTree ) )
                {
                    // This overload supports QWord (long) values. 
                    sectionKey.SetValue( "QuadWordValue", quadWordValue, RegistryValueKind.QWord );

                    // The following SetValue calls have the same effect as using the
                    // SetValue overload that does not specify RegistryValueKind.
                    //
                    sectionKey.SetValue( "DWordValue", dWordValue, RegistryValueKind.DWord );
                    sectionKey.SetValue( "MultipleStringValue", multipleStringValue, RegistryValueKind.MultiString );
                    sectionKey.SetValue( "BinaryValue", binaryValue, RegistryValueKind.Binary );
                    sectionKey.SetValue( "StringValue", stringValue, RegistryValueKind.String );

                    // This overload supports setting expandable string values. Compare
                    // the output from this value with the previous string value.
                    sectionKey.SetValue( "ExpandedStringValue", expandedStringValue, RegistryValueKind.ExpandString );
                }
            }
        }

        private static string FullTestKeyName( string testKeyName )
        {
            var key = string.Format( "{0}\\{1}", LocalMachineRoot, testKeyName );
            return key;
        }

        private static void DeleteKey( string keyName )
        {
            if ( !KeyExists( keyName ) )
            {
                return;
            }

            using ( RegistryKey key = RegistryConfigurationSource.OpenRoot( LocalMachineRoot ) )
            {
                key.DeleteSubKeyTree( keyName );
                Assert.Null( key.OpenSubKey( keyName ) );
            }
        }

        private static bool KeyExists( string keyName )
        {
            using ( RegistryKey key = RegistryConfigurationSource.OpenRoot( LocalMachineRoot ) )
            {
                using ( var target = key.OpenSubKey( keyName ) )
                {
                    return target != null;
                }
            }
        }
    }
}