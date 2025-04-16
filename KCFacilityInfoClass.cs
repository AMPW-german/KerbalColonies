﻿using System;
using System.Collections.Generic;
using System.Linq;
using KerbalColonies.colonyFacilities;

namespace KerbalColonies
{
    /// <summary>
    /// A class that's used to check and remove ressources from an vessel or other places. Currently only vessels are supported
    /// <para>
    /// It's also used to check to cost for upgrades of a facility.
    /// </para>
    /// <para>Level 0 is passed when building this facility.</para>
    /// <para>The maximum level is the inclusive upper limit, e.g. a facility with maxLevel = 1 can be built and then upgraded once to level 1.</para>
    /// <para>Repurpose: Now it's also used for creating the facilties from config files so values like cost and custom production parameters aren't hardcoded</para>
    /// </summary>
    public class KCFacilityInfoClass
    {
        /// <summary>
        /// This dictionary contains FacilityBase Types as key and the corresponding InfoClass Type as value.
        /// <para>It's used to create custom InfoClasses to perform the custom check.</para>
        /// <para>If the facility is not in this dictionary then the default InfoClass will be used</para>
        /// </summary>
        public static Dictionary<Type, Type> FacilityBaseInfos { get; private set; } = new Dictionary<Type, Type>();

        public static void RegisterFacilityBaseInfo<T, I>() where T : KCFacilityBase where I : KCFacilityInfoClass
        {
            if (!FacilityBaseInfos.ContainsKey(typeof(T)))
            {
                FacilityBaseInfos.Add(typeof(T), typeof(I));
            }
        }

        public static KCFacilityInfoClass GetInfoClass(ConfigNode node)
        {
            Type facilityType = KCFacilityTypeRegistry.GetType(node.GetValue("type"));

            if (FacilityBaseInfos.ContainsKey(facilityType))
            {
                return (KCFacilityInfoClass)Activator.CreateInstance(FacilityBaseInfos[facilityType], node);
            }
            else
            {
                return new KCFacilityInfoClass(node);
            }
        }


        public Type type { get; private set; }
        public ConfigNode facilityConfig { get; private set; }
        public string name { get; private set; }
        public string displayName { get; private set; }

        /// <summary>
        /// Level, Resource, Amount
        /// </summary>
        public Dictionary<int, Dictionary<PartResourceDefinition, double>> resourceCost { get; private set; }
        /// <summary>
        /// currently unused
        /// </summary>
        public Dictionary<int, double> Electricity { get; private set; } = new Dictionary<int, double> { };
        public Dictionary<int, double> Funds { get; private set; } = new Dictionary<int, double> { };
        public Dictionary<int, UpgradeType> UpgradeTypes { get; private set; } = new Dictionary<int, UpgradeType> { };
        public Dictionary<int, string> BasegroupNames { get; private set; } = new Dictionary<int, string> { };

        // 1 Kerbin day = 0.25 days
        // 100 per day * 5 engineers = 500 per day
        // 500 per day * 4 kerbin days = 500
        // 500 per day * 2 kerbin days = 250
        public Dictionary<int, float> UpgradeTimes { get; private set; } = new Dictionary<int, float> { };

        /// <summary>
        /// Used for custom checks (e.g. if a specific facility already exists in the colony), returns true if the facility can be built or upgraded.
        /// </summary>
        public virtual bool customCheck(int level, colonyClass colony)
        {
            return true;
        }

        public bool checkResources(int level, colonyClass colony)
        {
            if (!customCheck(level, colony))
            {
                return false;
            }

            foreach (KeyValuePair<PartResourceDefinition, double> resource in resourceCost[level])
            {
                double vesselAmount = 0;
                double colonyAmount = KCStorageFacility.colonyResources(resource.Key, colony);

                if (colony.CAB.PlayerInColony)
                {
                    FlightGlobals.ActiveVessel.GetConnectedResourceTotals(resource.Key.id, out double amount, out double maxAmount);

                    vesselAmount = amount;
                }

                if (vesselAmount >= resource.Value) continue;
                else if (colonyAmount >= resource.Value) continue;
                else if (vesselAmount + colonyAmount >= resource.Value) continue;
                else return false;
            }

            if (Funding.Instance != null)
            {
                if (Funding.Instance.Funds < Funds[level])
                {
                    return false;
                }
            }

            return true;
        }

        public bool removeResources(int level, colonyClass colony)
        {
            if (checkResources(level, colony))
            {
                foreach (KeyValuePair<PartResourceDefinition, double> resource in resourceCost[level])
                {
                    double remainingAmount = resource.Value;

                    double vesselAmount = 0;
                    double colonyAmount = KCStorageFacility.colonyResources(resource.Key, colony);
                    if (colony.CAB.PlayerInColony)
                    {
                        FlightGlobals.ActiveVessel.GetConnectedResourceTotals(resource.Key.id, out double amount, out double maxAmount);
                        vesselAmount = amount;
                    }

                    if (vesselAmount >= resource.Value)
                    {
                        FlightGlobals.ActiveVessel.RequestResource(FlightGlobals.ActiveVessel.rootPart, resource.Key.id, resource.Value, true);
                    }
                    else
                    {
                        if (colony.CAB.PlayerInColony)
                        {
                            FlightGlobals.ActiveVessel.RequestResource(FlightGlobals.ActiveVessel.rootPart, resource.Key.id, vesselAmount, true);
                        }
                        remainingAmount -= vesselAmount;

                        KCStorageFacility.addResourceToColony(resource.Key, -remainingAmount, colony);
                    }
                }
                if (Funding.Instance != null)
                {
                    Funding.Instance.AddFunds(-Funds[level], TransactionReasons.None);
                }
                return true;
            }
            return false;
        }

        public KCFacilityInfoClass(ConfigNode node)
        {
            facilityConfig = node;

            type = KCFacilityTypeRegistry.GetType(node.GetValue("type"));
            name = node.GetValue("name");
            displayName = node.GetValue("displayName");

            resourceCost = new Dictionary<int, Dictionary<PartResourceDefinition, double>>();
            Electricity = new Dictionary<int, double>();
            Funds = new Dictionary<int, double>();
            UpgradeTypes = new Dictionary<int, UpgradeType>();
            BasegroupNames = new Dictionary<int, string>();

            ConfigNode levelNode = node.GetNode("level");
            levelNode.GetNodes().ToList().ForEach(n =>
            {
                int level = int.Parse(n.name);
                if (n.HasValue("upgradeType")) UpgradeTypes.Add(level, (UpgradeType)Enum.Parse(typeof(UpgradeType), n.GetValue("upgradeType")));
                else UpgradeTypes.Add(level, UpgradeType.withoutGroupChange);

                if (n.HasValue("basegroupName")) BasegroupNames.Add(level, n.GetValue("basegroupName"));
                else if (level != 0) BasegroupNames.Add(level, BasegroupNames[level - 1]);
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no basegroupName (at least for level 0).");

                if (n.HasNode("resources"))
                {
                    ConfigNode resourceNode = n.GetNode("resources");
                    Dictionary<PartResourceDefinition, double> resourceList = new Dictionary<PartResourceDefinition, double>();
                    foreach(ConfigNode.Value v in resourceNode.values)
                    {
                        PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(v.name);
                        double amount = double.Parse(v.value);
                        resourceList.Add(resourceDef, amount);
                    }
                    resourceCost.Add(level, resourceList);
                }
                else
                {
                    resourceCost.Add(level, new Dictionary<PartResourceDefinition, double>());
                }

                if (n.HasValue("Electricity")) Electricity.Add(level, double.Parse(n.GetValue("Electricity")));
                else Electricity.Add(level, 0);

                if (n.HasValue("Funds")) Funds.Add(level, double.Parse(n.GetValue("Funds")));
                else Funds.Add(level, 0);

                if (n.HasValue("upgradeTime")) UpgradeTimes.Add(level, float.Parse(n.GetValue("upgradeTime")));
                else UpgradeTimes.Add(level, 0);
            });
        }
    }
}