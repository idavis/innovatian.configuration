#region License

//
// Copyright © 2009 Ian Davis <ian.f.davis@gmail.com>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

#endregion

#region Using Directives

using System.Collections.Generic;

#endregion

namespace Innovatian.Configuration
{
    /// <summary>
    /// Defines methods and properties representing a configuration data source.
    /// </summary>
    public interface IConfigurationSource : IEnumerable<IConfigurationSection>
    {
        /// <summary>
        /// if <see cref="AutoSave"/> is <c>true</c>, the inheritor should save
        /// every time a key is updated; otherwise, updates should be ignored.
        /// </summary>
        bool AutoSave { get; set; }

        /// <summary>
        /// if <see cref="Encrypt"/> is <c>true</c>, the inheritor should load
        /// and save the settings encrypted.
        /// </summary>
        /// <remarks>
        /// If <c>true</c>, you will need to set the <see cref="EncryptionKey"/> or
        /// an error will be thrown.
        /// </remarks>
        bool Encrypt { get; set; }

        /// <summary>
        /// The key used to encypt/decrypt the settings if <see cref="Encrypt"/> is <c>true</c>.
        /// </summary>
        string EncryptionKey { get; set; }

        /// <summary>
        /// Provides user access to named configuration sections.
        /// </summary>
        IDictionary<string, IConfigurationSection> Sections { get; }

        /// <summary>
        /// Adds a named configuration section to this source. If the section
        /// name is already in this source, the values from the new source will
        /// override and be added.
        /// </summary>
        /// <param name="section">The named section to add.</param>
        void Add( IConfigurationSection section );

        /// <summary>
        /// Processes all sections expanding configuration variables and saving
        /// the new values.
        /// </summary>
        void ExpandKeyValues();

        /// <summary>
        /// Merges the sources into this instance. Each source's sections will
        /// be added and merged. If the sources contain duplicate sections, they
        /// will be merged.
        /// </summary>
        /// <param name="configurationSources"></param>
        void Merge( params IConfigurationSource[] configurationSources );

        /// <summary>
        /// Discards all sections and merged sources and reloads a fresh set of
        /// settings.
        /// </summary>
        void Reload();

        /// <summary>
        /// Saves all sections. All data merged from other merged sources will
        /// be included.
        /// </summary>
        void Save();

        /// <summary>
        /// Clears all sections, their values, and calls Clear on all internal
        /// configuration sources.
        /// </summary>
        void Clear();
    }
}