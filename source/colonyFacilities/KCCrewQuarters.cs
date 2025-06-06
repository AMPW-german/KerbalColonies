﻿using KerbalColonies.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar

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

namespace KerbalColonies.colonyFacilities
{
    internal class KCCrewQuartersWindow : KCFacilityWindowBase
    {
        KCCrewQuarters CrewQuarterFacility;
        public KerbalGUI kerbalGUI;

        protected override void CustomWindow()
        {
            GUILayout.BeginVertical();
            kerbalGUI.StaffingInterface();
            GUILayout.EndVertical();
        }

        protected override void OnClose()
        {
            if (kerbalGUI != null && kerbalGUI.ksg != null)
            {
                kerbalGUI.ksg.Close();
                kerbalGUI.transferWindow = false;
            }
        }

        public KCCrewQuartersWindow(KCCrewQuarters CrewQuarterFacility) : base(CrewQuarterFacility, Configuration.createWindowID())
        {
            this.CrewQuarterFacility = CrewQuarterFacility;
            this.kerbalGUI = new KerbalGUI(CrewQuarterFacility, false);
            toolRect = new Rect(100, 100, 400, 600);
        }
    }

    internal class KCCrewQuarters : KCKerbalFacilityBase
    {
        public static List<KCCrewQuarters> CrewQuartersInColony(colonyClass colony)
        {
            return colony.Facilities.Where(f => f is KCCrewQuarters).Select(f => (KCCrewQuarters)f).ToList();
        }

        public static int ColonyKerbalCapacity(colonyClass colony)
        {
            return CrewQuartersInColony(colony).Sum(crewQuarter => crewQuarter.MaxKerbals);
        }

        public static KCCrewQuarters FindKerbalInCrewQuarters(colonyClass colony, ProtoCrewMember kerbal)
        {
            List<KCKerbalFacilityBase> facilitiesWithKerbal = KCKerbalFacilityBase.findKerbal(colony, kerbal);
            return (KCCrewQuarters)(facilitiesWithKerbal.Where(fac => fac is KCCrewQuarters).FirstOrDefault());
        }

        public static bool AddKerbalToColony(colonyClass colony, ProtoCrewMember kerbal)
        {
            if (FindKerbalInCrewQuarters(colony, kerbal) != null) { return false; }

            foreach (KCCrewQuarters crewQuarter in CrewQuartersInColony(colony))
            {
                if (crewQuarter.kerbals.Count < crewQuarter.MaxKerbals)
                {
                    Configuration.writeDebug($"Adding {kerbal.name} to {crewQuarter.name}");
                    crewQuarter.AddKerbal(kerbal);
                    return true;
                }
            }

            return false;
        }

        private KCCrewQuartersWindow crewQuartersWindow;

        /// <summary>
        /// Adds the member to this crew quarrter or moves it from another crew quarter over to this one if the member is already assigned to a crew quarter in this Colony
        /// </summary>
        /// <param name="kerbal"></param>
        public override void AddKerbal(ProtoCrewMember kerbal)
        {
            KCCrewQuarters oldCrewQuarter = FindKerbalInCrewQuarters(Colony, kerbal);

            if (oldCrewQuarter != null)
            {
                int status = oldCrewQuarter.kerbals[kerbal];
                oldCrewQuarter.kerbals.Remove(kerbal);
                kerbals.Add(kerbal, status);
            }
            else
            {
                kerbals.Add(kerbal, 0);
            }
        }

        /// <summary>
        /// Removes the member from the crew quarters and all other facilities that the member is assigned to
        /// </summary>
        public override void RemoveKerbal(ProtoCrewMember member)
        {
            if (kerbals.Any(k => k.Key.name == member.name))
            {
                KCKerbalFacilityBase.findKerbal(Colony, member).Where(x => !(x is KCCrewQuarters)).ToList().ForEach(facility =>
                {
                    facility.Update();
                    facility.RemoveKerbal(member);
                });

                kerbals.Remove(kerbals.First(kerbal => kerbal.Key.name == member.name).Key);
            }
        }

        public override void Update()
        {
            base.Update();
            if (!HighLogic.LoadedSceneIsFlight) kerbals.Keys.ToList().ForEach(kerbal => kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned);
        }

        public override void OnBuildingClicked()
        {
            if (crewQuartersWindow == null) crewQuartersWindow = new KCCrewQuartersWindow(this);
            crewQuartersWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            if (crewQuartersWindow == null) crewQuartersWindow = new KCCrewQuartersWindow(this);
            crewQuartersWindow.Toggle();
        }

        public override string GetFacilityProductionDisplay() => $"{kerbals.Count} / {MaxKerbals} kerbals assigned";

        public KCCrewQuarters(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            this.crewQuartersWindow = null;
            if (HighLogic.LoadedSceneIsFlight) kerbals.Keys.ToList().ForEach(kerbal => kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Available);
            else kerbals.Keys.ToList().ForEach(kerbal => kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned);

        }

        public KCCrewQuarters(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, true)
        {
            this.crewQuartersWindow = null;
        }
    }
}
