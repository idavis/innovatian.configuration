#region Using Directives

using System;

#endregion

namespace Lucid.Configuration.Tests.Classes
{
    [Flags]
    internal enum OptionsEnum
    {
        None = 0,
        A = 1,
        B = 2,
        C = 4
    }
}