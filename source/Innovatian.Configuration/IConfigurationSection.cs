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
using System.ComponentModel;

#endregion

namespace Innovatian.Configuration
{
    /// <summary>
    /// Defines a named section of a configuration exposing key value setting
    /// pairs. All settings are stored as strings until requested.
    /// </summary>
    public interface IConfigurationSection : INotifyPropertyChanged, ICollection<KeyValuePair<string, string>>
    {
        /// <summary>
        /// The name of the section.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Returns the value of the setting name <paramref name="key"/> as a
        /// <c>T</c> instance.
        /// </summary>
        /// <typeparam name="T">
        /// The type needed by the user.
        /// </typeparam>
        /// <param name="key">
        /// The name of the setting.
        /// </param>
        /// <returns>returns the setting value as a <c>T</c> instance or
        /// defatult(T) if the key does not exist.
        /// </returns>
        T Get<T>( string key );

        /// <summary>
        /// Returns the value of the setting name <paramref name="key"/> as a
        /// <c>T</c> instance.
        /// </summary>
        /// <typeparam name="T">
        /// The type needed by the user.
        /// </typeparam>
        /// <param name="key">
        /// The name of the setting.
        /// </param>
        /// <param name="defaultValue">
        /// The default value.
        /// </param>
        /// <returns>returns the setting value as a <c>T</c> instance or <paramref
        /// name="defaultValue"/> if the key does not exist.
        /// </returns>
        T Get<T>( string key, T defaultValue );

        /// <summary>
        /// Removes the specified key and its value from the section.
        /// </summary>
        /// <param name="key">
        /// The name of the setting.
        /// </param>
        /// <returns>
        /// <c>true</c> if the setting named <paramref name="key"/> existed and was
        /// removed; <c>false</c> otherwise.
        /// </returns>
        bool Remove( string key );

        /// <summary>
        /// Sets the value of the setting name <paramref name="key"/>. If the setting
        /// does not exist, it is created.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the setting being saved.
        /// </typeparam>
        /// <param name="key">
        /// The name of the setting.
        /// </param>
        /// <param name="value">
        /// The value to save in this section.
        /// </param>
        void Set<T>( string key, T value );
    }
}