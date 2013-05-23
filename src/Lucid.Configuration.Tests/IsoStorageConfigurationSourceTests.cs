#region Using Directives

using System;
using System.IO;
using System.IO.IsolatedStorage;
using Lucid.Configuration.Tests.Classes;
using NUnit;

#endregion

namespace Lucid.Configuration.Tests
{
    // These tests are all integration tests.
    [TestFixture]
    public class IsoStorageConfigurationSourceTests
    {
        private const IsolatedStorageScope Scope = IsolatedStorageScope.User | IsolatedStorageScope.Assembly;
        private readonly string _fileName = typeof (IsoStorageConfigurationSourceTests).Name + ".xml";

        [Test]
        public void EmptyFileNameThrows()
        {
            Assert.Throws<ArgumentNullException>(
                () => new IsoStorageConfigurationSource( Scope, string.Empty ) );
        }

        [Test]
        public void NullFileNameThrows()
        {
            Assert.Throws<ArgumentNullException>(
                () => new IsoStorageConfigurationSource( Scope, null ) );
        }

        /* 
         * System.IO.IsolatedStorage.IsolatedStorageException : Unable to determine application identity of the caller.
         * Need to find a way to test application scope IsoStorage in unit testing.
         * 
        [Test]
        public void CanLoadAndSaveWithDefaultScope()
        {
            var source = new IsoStorageConfigurationSource( _fileName );
            RunCreationTest( source );
        }
        
        [Test]
        public void CanLoadAndSaveInApplicationMachineScope()
        {
            var source = new IsoStorageConfigurationSource( IsolatedStorageScope.Application | IsolatedStorageScope.Machine, _fileName );
            RunCreationTest( source );
        }
        
        [Test]
        public void CanLoadAndSaveInApplicationUserScope()
        {
            var source = new IsoStorageConfigurationSource(IsolatedStorageScope.Application | IsolatedStorageScope.User, _fileName);
            RunCreationTest(source);
        }
        */

        [Test]
        public void CanLoadAndSaveInMachineAssemblyScope()
        {
            const IsolatedStorageScope scope = IsolatedStorageScope.Machine | IsolatedStorageScope.Assembly;
            var source = new IsoStorageConfigurationSource( scope, _fileName );
            RunCreationTest( source );
        }

        [Test]
        public void CanLoadAndSaveInAssemblyUserScope()
        {
            const IsolatedStorageScope scope = IsolatedStorageScope.User | IsolatedStorageScope.Assembly;
            var source = new IsoStorageConfigurationSource( scope, _fileName );
            RunCreationTest( source );
        }

        [Fact(Skip = "Not sure why this no longer works when on Win7. WinXP worked fine.")]
        // System.IO.IsolatedStorage.IsolatedStorageException: Unable to determine the domain of the caller.
        public void CanLoadAndSaveInMachineStorForDomainScope()
        {
            const IsolatedStorageScope scope =
                IsolatedStorageScope.Machine | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain;
            var source =
                new IsoStorageConfigurationSource( scope, _fileName );
            RunCreationTest( source );
        }

        [Fact(Skip = "Not sure why this no longer works when on Win7. WinXP worked fine.")]
        // System.IO.IsolatedStorage.IsolatedStorageException: Unable to determine the domain of the caller.
        public void CanLoadAndSaveInUserStorForDomainScope()
        {
            const IsolatedStorageScope scope =
                IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain;
            var source =
                new IsoStorageConfigurationSource( scope, _fileName );
            RunCreationTest( source );
        }

        [Test]
        public void CreationWithNoneScopeThrows()
        {
            Assert.Throws<ArgumentException>(
                () => new IsoStorageConfigurationSource( IsolatedStorageScope.None, _fileName ) );
        }

        private void RunCreationTest( IsoStorageConfigurationSource source )
        {
            string sourceFile = string.Empty;
            try
            {
                source.Add( SectionGenerator.GetSingleSection() );
                source.Save();
                sourceFile = source.FullPath;
                // we should now have a file on the hdd with the settings we want.
                string sourceAsXml = XmlConfigurationSource.ToXml( source );

                // Now create a new instance so it can load the data.
                var newSource = new IsoStorageConfigurationSource( source.Scope, _fileName );
                string newSourceAsXml = XmlConfigurationSource.ToXml( newSource );
                Assert.Equal( sourceAsXml, newSourceAsXml );
            }
            finally
            {
                if ( File.Exists( sourceFile ) )
                {
                    File.Delete( sourceFile );
                }
            }
        }
    }
}