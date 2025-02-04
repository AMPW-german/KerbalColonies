﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.colonyFacilities
{
    [System.Serializable]
    public abstract class KCKerbalFacilityBase : KCFacilityBase
    {

        /// <summary>
        /// Returns a list of all kerbals in the colony that are registered in a crew quarter
        /// </summary>
        /// <returns>An empty dictionary if any of the parameters are invalid, no KCCrewQuarter facilities exist or no KCCrewQuarter has any kerbals assigned</returns>
        public static Dictionary<ProtoCrewMember, int> GetAllKerbalsInColony(string saveGame, int bodyIndex, string colonyName)
        {
            if (!Configuration.coloniesPerBody.ContainsKey(saveGame)) { return new Dictionary<ProtoCrewMember, int> { }; }
            else if (!Configuration.coloniesPerBody[saveGame].ContainsKey(bodyIndex)) { return new Dictionary<ProtoCrewMember, int> { }; }
            else if (!Configuration.coloniesPerBody[saveGame][bodyIndex].ContainsKey(colonyName)) { return new Dictionary<ProtoCrewMember, int> { }; }

            Dictionary<ProtoCrewMember, int> kerbals = new Dictionary<ProtoCrewMember, int> { };
            KCCrewQuarters.CrewQuartersInColony(saveGame, bodyIndex, colonyName).ForEach(crewQuarter =>
            {
                crewQuarter.kerbals.Keys.ToList().ForEach(k =>
                {
                    if (!kerbals.ContainsKey(k))
                    {
                        kerbals.Add(k, crewQuarter.kerbals[k]);
                    }
                });
            });

            return kerbals;
        }

        /// <summary>
        /// Returns a list of all facilities that the kerbal is assigned to
        /// </summary>
        /// <returns>Returns an empty list if any of the parameters are invalid or the kerbal wasn't found</returns>
        public static List<KCKerbalFacilityBase> findKerbal(string saveGame, int bodyIndex, string colonyName, ProtoCrewMember kerbal)
        {
            if (!Configuration.coloniesPerBody.ContainsKey(saveGame)) { return new List<KCKerbalFacilityBase>(); }
            else if (!Configuration.coloniesPerBody[saveGame].ContainsKey(bodyIndex)) { return new List<KCKerbalFacilityBase>(); }
            else if (!Configuration.coloniesPerBody[saveGame][bodyIndex].ContainsKey(colonyName)) { return new List<KCKerbalFacilityBase>(); }

            List<KCKerbalFacilityBase> facilities = new List<KCKerbalFacilityBase>();
            Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName].Values.ToList().ForEach(UUIDdict =>
            {
                UUIDdict.Values.ToList().ForEach(colonyFacilitys =>
                {
                    colonyFacilitys.ForEach(colonyFacility =>
                    {
                        KCKerbalFacilityBase kCKerbalFacilityBase = colonyFacility as KCKerbalFacilityBase;
                        if (kCKerbalFacilityBase != null)
                        {
                            if (kCKerbalFacilityBase.kerbals.ContainsKey(kerbal))
                            {
                                facilities.Add(kCKerbalFacilityBase);
                            }
                        }
                    });
                });
            });

            return facilities;
        }

        /// <summary>
        /// A list of kerbals in the facility and their current status
        /// <para>Value: 0 means unassigned, the other values are custom</para>
        /// <para>Kerbals with value 0 can get removed from the facility, e.g. to add them to a different facility or retrive them</para>
        /// <para>Don't remove the kerbals from the crewquarters to assign them, only change the availability in the crewquarters</para>
        /// </summary>
        public int maxKerbals;
        protected Dictionary<ProtoCrewMember, int> kerbals;

        public int MaxKerbals { get { return maxKerbals; } }

        public bool modifyKerbal(ProtoCrewMember kerbal, int status)
        {
            if (kerbals.ContainsKey(kerbal))
            {
                kerbals[kerbal] = status;
                return true;
            }
            return false;
        }

        public List<ProtoCrewMember> getKerbals() { return kerbals.Keys.ToList(); }
        public virtual void RemoveKerbal(ProtoCrewMember member) { kerbals.Remove(member); }
        public virtual void AddKerbal(ProtoCrewMember member) { kerbals.Add(member, 0); }

        /// <summary>
        /// Returns an encoded string with the kerbal ids
        /// </summary>
        public static string CreateKerbalString(Dictionary<ProtoCrewMember, int> kerbals)
        {
            string s = "";
            if (kerbals.Count > 0)
            {
                s = $"k{0}&{kerbals.Keys.ToList()[0].name}&{kerbals.Values.ToList()[0]}";
                for (int i = 1; i < kerbals.Count; i++)
                {
                    s = $"{s}|k{i}&{kerbals.Keys.ToList()[i].name}&{kerbals.Values.ToList()[i]}";
                }
            }
            return s;
        }

        /// <summary>
        /// Expects the part from the datastring with the kerbal persistent ids. Don't pass other data to it.
        /// </summary>
        public static Dictionary<ProtoCrewMember, int> CreateKerbalList(string kerbalString)
        {
            Dictionary<ProtoCrewMember, int> kerbals = new Dictionary<ProtoCrewMember, int>();
            foreach (string s in kerbalString.Split('|'))
            {
                string kName = s.Split('&')[1];
                int kStatus = Convert.ToInt32(s.Split('&')[2]);
                foreach (ProtoCrewMember k in HighLogic.CurrentGame.CrewRoster.Crew)
                {
                    if (k.name == kName)
                    {
                        kerbals.Add(k, kStatus);
                        break;
                    }
                }
            }
            return kerbals;
        }

        public virtual List<ProtoCrewMember> filterKerbals(List<ProtoCrewMember> kerbals)
        {
            return kerbals;
        }

        /// <summary>
        /// Default method for the kerbalFacilities, only saves the kerbal list
        /// </summary>
        public override void EncodeString()
        {
            string kerbalString = CreateKerbalString(kerbals);
            facilityData = $"maxKerbals&{maxKerbals}{((kerbalString != "") ? $"|{kerbalString}" : "")}";
        }

        /// <summary>
        /// This only works if no other custom fields are saved in the facilityData
        /// </summary>
        public override void DecodeString()
        {
            if (facilityData != "")
            {
                string[] facilityDatas = facilityData.Split(new[] { '|' }, 2);
                maxKerbals = Convert.ToInt32(facilityDatas[0].Split('&')[1]);
                if (facilityDatas.Length > 1)
                {
                    kerbals = CreateKerbalList(facilityDatas[1]);
                }
            }
        }

        public override void Initialize(string facilityData)
        {
            kerbals = new Dictionary<ProtoCrewMember, int> { };
            base.Initialize(facilityData);
        }

        public KCKerbalFacilityBase(string facilityName, bool enabled, int maxKerbals = 8, string facilityData = "", int level = 0, int maxLevel = 0) : base(facilityName, enabled, facilityData, level, maxLevel)
        {
            this.maxKerbals = maxKerbals;
            kerbals = new Dictionary<ProtoCrewMember, int> { };
        }
    }
}