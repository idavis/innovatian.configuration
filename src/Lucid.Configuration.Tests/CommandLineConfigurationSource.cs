#region Using Directives

using System;
using NUnit;

#endregion

namespace Lucid.Configuration.Tests
{
    [TestFixture]
    public class CommandLineConfigurationSourceTests
    {
        [Test]
        public void Do()
        {
            var arguments = new[] {"/?", "--help", "-h", "/platform:x86"};
            var source = new CommandLineConfigurationSource( arguments );
            source.AddSwitch( "Default", "/?", "help", "h", "platform" );
            IConfigurationSection section = source.Sections["Default"];
            Console.WriteLine( source );
        }
    }
}