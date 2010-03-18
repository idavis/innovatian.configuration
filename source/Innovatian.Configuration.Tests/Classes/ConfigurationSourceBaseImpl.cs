#region Using Directives

using System;
using System.Collections.Generic;

#endregion

namespace Innovatian.Configuration.Tests.Classes
{
    public class ConfigurationSourceBaseImpl : AbstractConfigurationSource
    {
        #region Overrides of AbstractConfigurationSource

        public override void Save()
        {
            throw new NotSupportedException();
        }

        public override void Reload()
        {
            throw new NotSupportedException();
        }

        #endregion

        public IList<IConfigurationSource> ConfigurationSources
        {
            get { return base.ConfigurationSources; }
        }

        public void Clear()
        {
            base.Clear();
        }
    }
}