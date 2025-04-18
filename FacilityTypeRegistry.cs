﻿using KerbalColonies.colonyFacilities;
using System;
using System.Collections.Generic;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (C) 2024 AMPW, Halengar

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies
{
    internal static class KCFacilityTypeRegistry
    {
        private static Dictionary<string, Type> _registeredTypes = new Dictionary<string, Type>();

        // Register a new type by a unique string key
        public static void RegisterType<T>() where T : KCFacilityBase
        {
            string key = typeof(T).FullName;
            _registeredTypes[key] = typeof(T);
        }

        // Get a registered type by key
        public static Type GetType(string typeName)
        {
            return _registeredTypes.TryGetValue(typeName, out var type) ? type : null;
        }
        public static IEnumerable<string> GetAllRegisteredTypes()
        {
            return _registeredTypes.Keys;
        }
    }
}
