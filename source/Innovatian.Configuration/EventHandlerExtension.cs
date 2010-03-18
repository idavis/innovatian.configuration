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

using System;
using System.ComponentModel;

#endregion

namespace Innovatian.Configuration
{
    internal static class EventHandlerExtension
    {
        public static void Raise<T>( this EventHandler<T> handler, object sender, T args )
            where T : EventArgs
        {
            if ( handler != null )
            {
                handler( sender, args );
            }
        }

        public static void Raise( this EventHandler handler, object sender, EventArgs args )
        {
            if ( handler != null )
            {
                handler( sender, args );
            }
        }

        public static void Raise( this PropertyChangedEventHandler handler, object sender, PropertyChangedEventArgs args )
        {
            if ( handler != null )
            {
                handler( sender, args );
            }
        }
    }
}