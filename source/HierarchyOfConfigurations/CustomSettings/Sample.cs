#region Using Directives

using System;
using System.Reflection;

#endregion

namespace Innovatian.Configuration.Samples.CustomSettings
{
    internal class Sample
    {
        public static void Run()
        {
            // make sure to do this at the very beginning of your application!
            SettingsManager.Initialize();

            var settings = new NetworkSettings(); // the ctor which takes an env is for testing.

            var properties = typeof (NetworkSettings).GetProperties( BindingFlags.Public |
                                                    BindingFlags.Instance |
                                                    BindingFlags.DeclaredOnly );
            foreach ( var property in properties )
            {
                Console.WriteLine("{0}:\t{1}", property.Name, property.GetValue( settings, null ));
            }
            Console.ReadLine();
        }
    }
}