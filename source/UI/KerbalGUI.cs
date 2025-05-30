﻿using KerbalColonies.colonyFacilities;
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

namespace KerbalColonies.UI
{
    public class KerbalSelectorGUI : KCWindowBase
    {
        private static KerbalSelectorGUI instance;

        public enum SwitchModes
        {
            ActiveVessel,
            Colony,
            Facility
        }
        public SwitchModes mode = SwitchModes.ActiveVessel;

        private KCKerbalFacilityBase fromFac;
        private string fromName;
        private string toName;
        private KerbalGUI kGUI;
        private List<ProtoCrewMember> fromList;
        private List<ProtoCrewMember> toList;
        Vessel toVessel;
        private int fromCapacity;
        private int toCapacity;
        List<ProtoCrewMember> fromListModifierList = new List<ProtoCrewMember>() { }; // Kerbals to be removed from the vessel
        List<ProtoCrewMember> toListModifierList = new List<ProtoCrewMember>() { }; // Kerbals to be added to the vessel

        Vector2 fromScrollPos;
        Vector2 toScrollPos;

        colonyClass colony;

        protected override void OnOpen()
        {
            if (instance == null) instance = this;
            else if (instance != null && instance != this) { instance.Close(); instance = this; }

            switch (mode)
            {
                case SwitchModes.ActiveVessel:
                    this.fromList = fromFac.filterKerbals(toVessel.GetVesselCrew());
                    this.toList = new List<ProtoCrewMember>(fromFac.getKerbals());
                    break;
                case SwitchModes.Colony:
                    this.fromList = fromFac.filterKerbals(KCKerbalFacilityBase.GetAllKerbalsInColony(colony).Where(kvp => kvp.Value == 0).ToDictionary(i => i.Key, i => i.Value).Keys.ToList());
                    this.toList = fromFac.getKerbals();
                    break;
            }
        }

        protected override void OnClose()
        {
            foreach (ProtoCrewMember member in fromListModifierList)
            {
                fromFac.AddKerbal(member);

                if (mode == SwitchModes.ActiveVessel)
                {

                    InternalSeat seat = member.seat;
                    if (member.seat != null)
                    {
                        seat.part.RemoveCrewmember(member); // Remove from seat
                        member.seat = null;
                    }

                    foreach (Part p in toVessel.Parts)
                    {
                        if (p.protoModuleCrew.Contains(member))
                        {
                            p.protoModuleCrew.Remove(member);
                            int index = p.protoPartSnapshot.GetCrewIndex(member.name);
                            Configuration.writeDebug(index.ToString());
                            Configuration.writeDebug(member.seatIdx.ToString());
                            p.protoPartSnapshot.RemoveCrew(member);
                            p.RemoveCrewmember(member);
                            p.ModulesOnUpdate();
                            break;
                        }
                    }

                    toVessel.RemoveCrew(member);
                    HighLogic.CurrentGame.CrewRoster.AddCrewMember(member);

                    toVessel.SpawnCrew();
                }
                else if (mode == SwitchModes.Colony)
                {
                    KCCrewQuarters.FindKerbalInCrewQuarters(colony, member).modifyKerbal(member, 1);
                }

            }
            fromListModifierList.Clear();

            foreach (ProtoCrewMember member in toListModifierList)
            {
                fromFac.RemoveKerbal(member);

                if (mode == SwitchModes.ActiveVessel)
                {
                    foreach (Part p in toVessel.Parts)
                    {
                        if (p.CrewCapacity > 0)
                        {
                            if (p.protoModuleCrew.Count >= p.CrewCapacity)
                            {
                                continue;
                            }
                            List<int> freeSeats = new List<int>();
                            for (int i = 0; i < p.CrewCapacity; i++) { freeSeats.Add(i); }

                            foreach (ProtoCrewMember pcm in p.protoModuleCrew)
                            {
                                freeSeats.Remove(pcm.seatIdx);
                            }
                            if (freeSeats.Count > 0)
                            {
                                p.AddCrewmemberAt(member, freeSeats[0]);
                                p.RegisterCrew();
                                break;
                            }
                        }
                    }

                    member.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                    //toVessel.RebuildCrewList();
                    toVessel.RebuildCrewList();
                    toVessel.SpawnCrew();
                    Vessel.CrewWasModified(toVessel);
                    Game currentGame = HighLogic.CurrentGame.Updated();
                }
                else if (mode == SwitchModes.Colony)
                {
                    KCCrewQuarters.FindKerbalInCrewQuarters(colony, member).modifyKerbal(member, 0);
                }
            }
            toListModifierList.Clear();

            kGUI.transferWindow = false;

            if (instance == this) instance = null;
        }

        protected override void CustomWindow()
        {
            if (mode == SwitchModes.ActiveVessel)
            {
                this.fromCapacity = toVessel.GetCrewCapacity();
                this.toCapacity = fromFac.MaxKerbals;
            }
            else
            {
                this.fromCapacity = KCCrewQuarters.ColonyKerbalCapacity(colony);
                this.toCapacity = fromFac.MaxKerbals;
            }

            ProtoCrewMember fromListModifier = null;
            ProtoCrewMember toListModifier = null;

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(toolRect.width * 0.48f));
                {
                    GUILayout.Label(fromName, LabelGreen);
                    fromScrollPos = GUILayout.BeginScrollView(fromScrollPos);
                    foreach (ProtoCrewMember k in fromList)
                    {
                        if (GUILayout.Button(k.name, GUILayout.Height(23)))
                        {
                            if (toList.Count + 1 <= toCapacity)
                            {
                                Configuration.writeDebug($"Adding Kerbal {k.name} from {toName} to {fromName}");
                                fromListModifier = k;
                                fromListModifierList.Add(k);
                                toList.Add(k);
                            }
                        }
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(toolRect.width * 0.48f));
                {
                    GUILayout.Label(toName, LabelGreen);
                    toScrollPos = GUILayout.BeginScrollView(toScrollPos);
                    foreach (ProtoCrewMember k in toList)
                    {
                        if (GUILayout.Button(k.name, GUILayout.Height(23)))
                        {
                            if (fromList.Count + 1 <= fromCapacity)
                            {
                                Configuration.writeDebug($"Adding Kerbal {k.name} from {fromName} to {toName}");
                                toListModifier = k;
                                toListModifierList.Add(k);
                                fromList.Add(k);
                            }
                        }
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            if (fromListModifier != null)
            {
                fromList.Remove(fromListModifier);
                if (toListModifierList.Contains(fromListModifier)) toListModifierList.Remove(fromListModifier);
            }
            if (toListModifier != null)
            {
                toList.Remove(toListModifier);
                if (fromListModifierList.Contains(fromListModifier)) fromListModifierList.Remove(fromListModifier);
            }
        }

        internal KerbalSelectorGUI(KCKerbalFacilityBase fac, KerbalGUI kGUI, Vessel fromVessel) : base(Configuration.createWindowID(), fac.name)
        {
            this.fromFac = fac;
            toolRect = new Rect(100, 100, 500, 500);
            this.fromName = fromVessel.GetDisplayName();
            this.toName = fac.DisplayName;
            this.fromList = fac.filterKerbals(fromVessel.GetVesselCrew());
            this.toVessel = fromVessel;
            this.kGUI = kGUI;
            this.toList = new List<ProtoCrewMember>(fromFac.getKerbals());
            this.mode = SwitchModes.ActiveVessel;
            this.fromCapacity = fromVessel.GetCrewCapacity();
            this.toCapacity = fac.MaxKerbals;
        }

        internal KerbalSelectorGUI(KCKerbalFacilityBase fac, KerbalGUI kGUI) : base(Configuration.createWindowID(), fac.name)
        {
            this.fromFac = fac;
            this.colony = fromFac.Colony;
            toolRect = new Rect(100, 100, 500, 500);
            this.fromName = colony.Name;
            this.toName = fac.DisplayName;
            this.fromList = fromFac.filterKerbals(KCKerbalFacilityBase.GetAllKerbalsInColony(colony).Where(kvp => kvp.Value == 0).ToDictionary(i => i.Key, i => i.Value).Keys.ToList());
            this.toList = fromFac.getKerbals();
            this.kGUI = kGUI;
            this.mode = SwitchModes.Colony;
            this.fromCapacity = KCCrewQuarters.ColonyKerbalCapacity(colony);
            this.toCapacity = fromFac.MaxKerbals;
        }
    }

    public class KerbalGUI
    {
        public static GUIStyle LabelInfo;
        public static GUIStyle BoxInfo;
        public static GUIStyle ButtonSmallText;

        private Vector2 scrollPos;
        internal KCKerbalFacilityBase fac;
        public bool transferWindow;

        public static Texture tKerbal = GameDatabase.Instance.GetTexture("KerbalKonstructs/Assets/billeted", false);
        public static Texture tNoKerbal = GameDatabase.Instance.GetTexture("KerbalKonstructs/Assets/unbilleted", false);
        public static Texture tXPGained = GameDatabase.Instance.GetTexture("KerbalKonstructs/Assets/xpgained", false);
        public static Texture tXPUngained = GameDatabase.Instance.GetTexture("KerbalKonstructs/Assets/xpungained", false);
        public KerbalSelectorGUI ksg;

        public void StaffingInterface()
        {
            int kerbalCount = fac.getKerbals().Count;
            LabelInfo = new GUIStyle(GUI.skin.label);
            LabelInfo.normal.background = null;
            LabelInfo.normal.textColor = Color.white;
            LabelInfo.fontSize = 13;
            LabelInfo.fontStyle = FontStyle.Bold;
            LabelInfo.padding.left = 3;
            LabelInfo.padding.top = 0;
            LabelInfo.padding.bottom = 0;

            BoxInfo = new GUIStyle(GUI.skin.box);
            BoxInfo.normal.textColor = Color.cyan;
            BoxInfo.fontSize = 13;
            BoxInfo.padding.top = 2;
            BoxInfo.padding.bottom = 1;
            BoxInfo.padding.left = 5;
            BoxInfo.padding.right = 5;
            BoxInfo.normal.background = null;

            ButtonSmallText = new GUIStyle(GUI.skin.button);
            ButtonSmallText.fontSize = 12;
            ButtonSmallText.fontStyle = FontStyle.Normal;

            if (fac.MaxKerbals > 0)
            {
                GUILayout.Space(5);

                float CountCurrent = fac.getKerbals().Count;
                float CountEmpty = fac.MaxKerbals - CountCurrent;

                scrollPos = GUILayout.BeginScrollView(scrollPos);
                {
                    foreach (ProtoCrewMember pcm in fac.getKerbals())
                    {
                        GUILayout.BeginHorizontal(GUILayout.Height(80));
                        GUILayout.Box(tKerbal, GUILayout.Width(23), GUILayout.Height(38));

                        GUILayout.BeginVertical();
                        GUILayout.Label(pcm.displayName, LabelInfo);
                        GUILayout.Label(pcm.trait, LabelInfo);
                        GUILayout.Label(pcm.gender.ToString(), LabelInfo);
                        GUILayout.Label($"Level: {pcm.experienceLevel}", LabelInfo);
                        GUILayout.EndVertical();

                        GUILayout.EndHorizontal();
                    }

                    while (CountEmpty > 0)
                    {
                        GUILayout.BeginHorizontal(GUILayout.Height(80));

                        GUILayout.Box(tNoKerbal, GUILayout.Width(23), GUILayout.Height(38));
                        CountEmpty = CountEmpty - 1;
                        GUILayout.EndHorizontal();

                    }
                }
                GUILayout.EndScrollView();

                GUI.enabled = true;

                GUILayout.BeginHorizontal();
                GUILayout.Label("Staff: " + kerbalCount.ToString("#0") + "/" + fac.MaxKerbals.ToString("#0"), LabelInfo);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(2);

                if (ksg != null)
                {
                    if (ksg.mode == KerbalSelectorGUI.SwitchModes.ActiveVessel && !fac.Colony.CAB.PlayerInColony) { GUI.enabled = false; }
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Assign/Retrive Kerbals", GUILayout.Height(23)))
                        {
                            if (transferWindow)
                            {
                                ksg.Close();
                                transferWindow = false;
                            }
                            else
                            {
                                ksg.Open();
                                transferWindow = true;
                            }
                        }
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(5);
        }

        public KerbalGUI(KCKerbalFacilityBase fac, bool fromColony)
        {
            transferWindow = false;
            this.fac = fac;

            if (fromColony)
            {
                this.ksg = new KerbalSelectorGUI(fac, this);
            }
            else
            {
                if (FlightGlobals.ActiveVessel != null)
                {
                    this.ksg = new KerbalSelectorGUI(fac, this, FlightGlobals.ActiveVessel);
                }
                else
                {
                    ksg = null;
                }
            }
        }
    }
}
