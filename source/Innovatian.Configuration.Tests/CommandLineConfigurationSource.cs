#region Using Directives

using System;
using Xunit;

#endregion

namespace Innovatian.Configuration.Tests
{
    public class CommandLineConfigurationSourceTests
    {
        [Fact]
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