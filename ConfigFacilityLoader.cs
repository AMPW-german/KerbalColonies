﻿using KerbalColonies.colonyFacilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public static class ConfigFacilityLoader
    {
        // A config file with facility nodes
        // Each facility node has the standart parameters and a list of custom parameters
        // Also includes a node for the base group names
        // display name and facility type

        public static void LoadFacilityConfigs()
        {
            ConfigNode[] facilityConfigs = GameDatabase.Instance.GetConfigNodes("facilityConfigs");
            facilityConfigs.ToList().ForEach(node =>
            {
                node.GetNodes("facility").ToList().ForEach(facilityNode =>
                {
                    Configuration.RegisterBuildableFacility(KCFacilityInfoClass.GetInfoClass(facilityNode));
                });
            });
        }
    }
}
