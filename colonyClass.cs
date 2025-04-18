﻿using KerbalColonies.colonyFacilities;
using System.Collections.Generic;
using System.Linq;

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
    public class colonyClass
    {
        public string Name;

        public KC_CAB_Facility CAB;

        public List<KCFacilityBase> Facilities;
        public List<ConfigNode> sharedColonyNodes;

        public ConfigNode CreateConfigNode()
        {
            ConfigNode node = new ConfigNode("colonyClass");
            node.AddValue("name", Name);
            ConfigNode colonyNodes = new ConfigNode("sharedColonyNodes");
            this.sharedColonyNodes.ForEach(x => colonyNodes.AddNode(x));
            node.AddNode(colonyNodes);

            ConfigNode CABNode = new ConfigNode("CAB");
            CABNode.AddValue("type", CAB.GetType().FullName);

            CABNode.AddNode(CAB.getConfigNode());
            node.AddNode(CABNode);

            foreach (KCFacilityBase facility in Facilities)
            {
                ConfigNode facilityNode = new ConfigNode("facility");
                facilityNode.AddValue("type", facility.GetType().FullName);

                facilityNode.AddNode(facility.getConfigNode());
                node.AddNode(facilityNode);
            }
            return node;
        }

        public void UpdateColony()
        {
            CAB.Update();
            Facilities.ForEach(f => f.Update());
        }

        public colonyClass(string name)
        {
            Name = name;
            CAB = new KC_CAB_Facility(this);
            Facilities = new List<KCFacilityBase>();
            sharedColonyNodes = new List<ConfigNode>();
        }

        public colonyClass(ConfigNode node)
        {
            Name = node.GetValue("name");
            Facilities = new List<KCFacilityBase>();
            sharedColonyNodes = node.GetNode("sharedColonyNodes").GetNodes().ToList();

            foreach (ConfigNode facilityNode in node.GetNodes("facility"))
            {
                Facilities.Add(Configuration.CreateInstance(
                    KCFacilityTypeRegistry.GetType(facilityNode.GetValue("type")),
                    this,
                    facilityNode.GetNodes().First()
                ));
            }

            ConfigNode CABNode = node.GetNode("CAB");

            CAB = new KC_CAB_Facility(this, CABNode.GetNodes().First());
        }
    }
}
