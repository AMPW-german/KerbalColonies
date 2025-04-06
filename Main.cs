﻿using KerbalColonies.colonyFacilities;
using KerbalColonies.Serialization;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using CommNet.Network;
using KerbalKonstructs.Modules;

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
    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class KerbalColonies : MonoBehaviour
    {
        double lastTime = 0;
        bool despawned = false;
        int waitCounter = 0;

        protected void Awake()
        {
            GameEvents.onGameStateCreated.Add(GameLoad);
            GameEvents.onGameStateSaved.Add(GameSave);

            KSPLog.print("KC awake");
            Configuration.LoadConfiguration(Configuration.APP_NAME.ToUpper());
            KCFacilityTypeRegistry.RegisterType<KCStorageFacility>();
            KCFacilityTypeRegistry.RegisterType<KCCrewQuarters>();
            KCFacilityTypeRegistry.RegisterType<KCResearchFacility>();
            KCFacilityTypeRegistry.RegisterType<KC_CAB_Facility>();
            KCFacilityTypeRegistry.RegisterType<KCMiningFacility>();
            KCFacilityTypeRegistry.RegisterType<KCProductionFacility>();
            KCFacilityTypeRegistry.RegisterType<KCResourceConverterFacility>();
            KCFacilityTypeRegistry.RegisterType<KCHangarFacility>();
            KCFacilityTypeRegistry.RegisterType<KCLaunchpadFacility>();
            KCFacilityTypeRegistry.RegisterType<KCCommNetFacility>();
            Configuration.RegisterBuildableFacility(typeof(KCStorageFacility), new KCStorageFacilityCost());
            Configuration.RegisterBuildableFacility(typeof(KCCrewQuarters), new KCCrewQuarterCost());
            Configuration.RegisterBuildableFacility(typeof(KCResearchFacility), new KCResearchFacilityCost());
            Configuration.RegisterBuildableFacility(typeof(KCMiningFacility), new KCMiningFacilityCost());
            Configuration.RegisterBuildableFacility(typeof(KCProductionFacility), new KCProductionFacilityCost());
            Configuration.RegisterBuildableFacility(typeof(KCResourceConverterFacility), new KCResourceConverterFacilityCost());
            Configuration.RegisterBuildableFacility(typeof(KCHangarFacility), new KCHangarFacilityCost());
            Configuration.RegisterBuildableFacility(typeof(KCLaunchpadFacility), new KCLaunchPadCost());
            Configuration.RegisterBuildableFacility(typeof(KCCommNetFacility), new KCCommNetCost());

            KC_CAB_Facility.addPriorityDefaultFacility(typeof(KCLaunchpadFacility), 1);
            KC_CAB_Facility.addDefaultFacility(typeof(KCStorageFacility), 1);
            KC_CAB_Facility.addDefaultFacility(typeof(KCCrewQuarters), 1);
            KC_CAB_Facility.addDefaultFacility(typeof(KCProductionFacility), 1);

            KerbalKonstructs.API.RegisterOnStaticClicked(KCFacilityBase.OnBuildingClickedHandler);
        }

        protected void Start()
        {
            GameEvents.onGamePause.Add(onPause);

            KSPLog.print("KC start");
            Configuration.coloniesPerBody.Clear();
            Configuration.LoadColonies("KCCD");
        }

        public void GameLoad(Game game)
        {
            Configuration.writeDebug(game.config.HasNode("KCtestNode") ? game.config.GetNode("KCtestNode").ToString() : "");
        }
        public void GameSave(Game game)
        {
            Configuration.writeDebug("Game saved");
            game.config.SetNode("KCtestNode", new ConfigNode("KCtestNode"), true);
        }

        public void FixedUpdate()
        {
            if (lastTime - Planetarium.GetUniversalTime() >= 10)
            {
                lastTime = Planetarium.GetUniversalTime();
                string saveGame = HighLogic.CurrentGame.Seed.ToString();
                if (Configuration.coloniesPerBody.ContainsKey(saveGame))
                {
                    foreach (int bodyIndex in Configuration.coloniesPerBody[saveGame].Keys)
                    {
                        foreach (string colonyName in Configuration.coloniesPerBody[saveGame][bodyIndex].Keys)
                        {
                            List<KCFacilityBase> colonyFacilities = KCFacilityBase.GetFacilitiesInColony(saveGame, bodyIndex, colonyName);
                            colonyFacilities.ForEach(facility => facility.Update());
                        }
                    }
                }
            }
            else
            {
                lastTime += Planetarium.GetUniversalTime();
            }

            if (ColonyBuilding.placedGroup)
            {
                if (!ColonyBuilding.nextFrame)
                {
                    ColonyBuilding.nextFrame = true;
                }
                else
                {
                    if (ColonyBuilding.buildQueue.Count() > 0)
                    {
                        KerbalKonstructs.API.CreateGroup(ColonyBuilding.buildQueue.Peek().groupName);
                        KerbalKonstructs.API.CopyGroup(ColonyBuilding.buildQueue.Peek().groupName, ColonyBuilding.buildQueue.Peek().fromGroupName, fromBodyName: "Kerbin");
                        KerbalKonstructs.API.GetGroupStatics(ColonyBuilding.buildQueue.Peek().groupName).ForEach(instance => instance.ToggleAllColliders(false));
                        KerbalKonstructs.API.OpenGroupEditor(ColonyBuilding.buildQueue.Peek().groupName);
                        KerbalKonstructs.API.RegisterOnGroupSaved(ColonyBuilding.PlaceNewGroupSave);
                        ColonyBuilding.buildQueue.Peek().Facility.KKgroups.Add(ColonyBuilding.buildQueue.Peek().groupName, FlightGlobals.GetBodyIndex(FlightGlobals.currentMainBody)); // add the group to the facility groups
                    }
                    ColonyBuilding.placedGroup = false;
                }
            }

            if (waitCounter < 10)
            {
                waitCounter++;
                return;
            }
            else
            {
                if (!despawned)
                {
                    foreach (string saveGame in Configuration.coloniesPerBody.Keys)
                    {
                        if (saveGame == HighLogic.CurrentGame.Seed.ToString()) { continue; }

                        foreach (int bodyIndex in Configuration.coloniesPerBody[saveGame].Keys)
                        {
                            foreach (string colonyName in Configuration.coloniesPerBody[saveGame][bodyIndex].Keys)
                            {
                                foreach (GroupPlaceHolder gph in Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName].Keys)
                                {
                                    foreach (string UUID in Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName][gph].Keys)
                                    {
                                        KerbalKonstructs.API.DeactivateStatic(UUID);
                                    }
                                }
                            }
                        }
                    }

                    foreach (string saveGame in Configuration.coloniesPerBody.Keys)
                    {
                        if (saveGame != HighLogic.CurrentGame.Seed.ToString()) { continue; }

                        foreach (int bodyIndex in Configuration.coloniesPerBody[saveGame].Keys)
                        {
                            foreach (string colonyName in Configuration.coloniesPerBody[saveGame][bodyIndex].Keys)
                            {
                                foreach (GroupPlaceHolder gph in Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName].Keys)
                                {
                                    //"4470f197-e8c2-407d-8544-49c647bb5996"
                                    foreach (string UUID in Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName][gph].Keys)
                                    {
                                        KerbalKonstructs.API.ActivateStatic(UUID);
                                    }
                                }
                            }
                        }
                    }
                    despawned = true;
                }
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                KCResourceConverterFacility.resourceTypes.ToString();
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                KerbalKonstructs.API.ActivateStatic("33846799-30d8-4309-9196-8a94f2927af2");
                KerbalKonstructs.API.ActivateStatic("692b2f7a-ef4a-4070-a583-05fce1ebf80f");
            }
            else if (Input.GetKeyDown(KeyCode.H))
            {
                KerbalKonstructs.API.DeactivateStatic("33846799-30d8-4309-9196-8a94f2927af2");
                KerbalKonstructs.API.DeactivateStatic("692b2f7a-ef4a-4070-a583-05fce1ebf80f");
            }
        }

        void onPause()
        {
            Configuration.SaveColonies();
        }

        public void LateUpdate()
        {
        }

        protected void OnDestroy()
        {
            GameEvents.onGameStateCreated.Remove(GameLoad);
            GameEvents.onGameStateSaved.Remove(GameSave);

            GameEvents.onGamePause.Remove(onPause);

            foreach (string saveGame in Configuration.coloniesPerBody.Keys)
            {
                if (saveGame == HighLogic.CurrentGame.Seed.ToString()) { continue; }

                foreach (int bodyIndex in Configuration.coloniesPerBody[saveGame].Keys)
                {
                    foreach (string colonyName in Configuration.coloniesPerBody[saveGame][bodyIndex].Keys)
                    {
                        foreach (GroupPlaceHolder gph in Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName].Keys)
                        {
                            foreach (string UUID in Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName][gph].Keys)
                            {
                                KerbalKonstructs.API.ActivateStatic(UUID);
                            }
                        }
                    }
                }
            }

            Configuration.SaveColonies();
            KerbalKonstructs.API.UnRegisterOnStaticClicked(KCFacilityBase.OnBuildingClickedHandler);
            Configuration.coloniesPerBody.Clear();
        }
    }
}
