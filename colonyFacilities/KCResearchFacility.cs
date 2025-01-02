﻿using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;


namespace KerbalColonies.colonyFacilities
{
    internal class KCResearchFacilityCost : KCFacilityCostClass
    {
        public override bool VesselHasRessources(Vessel vessel, int level)
        {
            for (int i = 0; i < resourceCost.Count; i++)
            {
                vessel.GetConnectedResourceTotals(resourceCost.ElementAt(i).Key.id, false, out double amount, out double maxAmount);

                if (amount < resourceCost.ElementAt(i).Value)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool RemoveVesselRessources(Vessel vessel, int level)
        {
            if (VesselHasRessources(vessel, 0))
            {
                for (int i = 0; i < resourceCost.Count; i++)
                {
                    vessel.RequestResource(vessel.rootPart, resourceCost.ElementAt(i).Key.id, resourceCost.ElementAt(i).Value, true);
                }
                return true;
            }
            return false;
        }

        public KCResearchFacilityCost()
        {
            resourceCost = new Dictionary<PartResourceDefinition, float> { { PartResourceLibrary.Instance.GetDefinition("Ore"), 200f }, { PartResourceLibrary.Instance.GetDefinition("MonoPropellant"), 100f } };
        }
    }

    internal class KCResearchFacilityWindow : KCWindowBase
    {
        KCResearchFacility researchFacility;
        VesselKerbalGUI kerbalGUI;

        protected override void CustomWindow()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Science Points: " + researchFacility.SciencePoints);
            GUILayout.Label("Max Science Points: " + researchFacility.MaxSciencePoints);
            GUILayout.EndHorizontal();

            kerbalGUI.StaffingInterface();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Retrieve Science Points"))
            {
                researchFacility.RetrieveSciencePoints();
            }
            GUILayout.EndHorizontal();
        }

        public KCResearchFacilityWindow(KCResearchFacility researchFacility) : base(Configuration.createWindowID(researchFacility), "Researchfacility")
        {
            this.researchFacility = researchFacility;
            this.kerbalGUI = new VesselKerbalGUI(researchFacility);
            toolRect = new Rect(100, 100, 400, 800);
        }
    }


    [System.Serializable]
    internal class KCResearchFacility : KCKerbalFacilityBase
    {
        private KCResearchFacilityWindow researchFacilityWindow;

        public float sciencePoints;

        public float MaxSciencePoints { get { return maxSciencePointList[level]; } }
        public float SciencePoints { get { return sciencePoints; } }

        private List<float> maxSciencePointList = new List<float> { 50, 100, 200, 400 };
        private List<float> researchpointsPerDayperResearcher = new List<float> { 0.25f, 0.3f, 0.35f, 0.4f };
        private List<int> maxKerbalsPerLevel = new List<int> { 4, 6, 8, 12 };

        public override List<ProtoCrewMember> filterKerbals(List<ProtoCrewMember> kerbals)
        {
            return kerbals.Where(k => k.experienceTrait.Title == "Scientist").ToList();
        }

        public override void Update()
        {
            double deltaTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            lastUpdateTime = Planetarium.GetUniversalTime();
            sciencePoints = Math.Min(maxSciencePointList[level], sciencePoints + (float)((researchpointsPerDayperResearcher[level] / 24 / 60 / 60) * deltaTime) * kerbals.Count);
            //ResearchAndDevelopment.Instance.AddScience((float) (researchpointsPerDayperResearcher[level] / 24 / 60 / 60 * deltaTime) * kerbals.Count, TransactionReasons.Cheating);
            Configuration.SaveColonies();
        }

        public override void OnBuildingClicked()
        {
            researchFacilityWindow.Toggle();
        }

        public bool RetrieveSciencePoints()
        {
            if (sciencePoints > 0)
            {
                ResearchAndDevelopment.Instance.AddScience(sciencePoints, TransactionReasons.Cheating);
                sciencePoints = 0;
                return true;
            }
            return false;
        }

        public override bool UpgradeFacility(int level)
        {
            base.UpgradeFacility(level);
            maxKerbals = maxKerbalsPerLevel[level];
            return true;
        }

        public override void Initialize(string facilityData)
        {
            base.Initialize(facilityData);
            this.researchFacilityWindow = new KCResearchFacilityWindow(this);
            this.baseGroupName = "KC_CAB";

            maxSciencePointList = new List<float> { 50, 100, 200, 400 };
            researchpointsPerDayperResearcher = new List<float> { 0.25f, 0.3f, 0.35f, 0.4f };
            maxKerbalsPerLevel = new List<int> { 4, 6, 8, 12 };

            this.maxKerbals = maxKerbalsPerLevel[level];
        }

        public KCResearchFacility(bool enabled, string facilityData = "") : base("KCResearchFacility", true, 8, "", 0, 3) {
            sciencePoints = 0;
        }
    }
}
