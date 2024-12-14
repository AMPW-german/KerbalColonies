﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.colonyFacilities
{
    abstract class KCKerbalFacilityBase : KCFacilityBase
    {
        protected List<ProtoCrewMember> kerbals;

        public List<ProtoCrewMember> getKerbals() { return kerbals; }
        public void RemoveKerbal(ProtoCrewMember member) { kerbals.Remove(member); }
        public void AddKerbal(ProtoCrewMember member) { kerbals.Add(member); }

        /// <summary>
        /// Returns an encoded string with the kerbal ids
        /// </summary>
        public static string CreateKerbalString(List<ProtoCrewMember> kerbals)
        {
            string s = "";
            if (kerbals.Count > 0)
            {
                s = $"k{0}&{kerbals[0].name}";
                for (int i = 1; i < kerbals.Count; i++)
                {
                    s = $"{s}|k{i}&{kerbals[i].name}";
                }
            }
            return s;
        }

        /// <summary>
        /// Expects the part from the datastring with the kerbal persistent ids. Don't pass other data to it.
        /// </summary>
        public static List<ProtoCrewMember> CreateKerbalList(string kerbalString)
        {
            List<ProtoCrewMember> kerbals = new List<ProtoCrewMember>();
            foreach (string s in kerbalString.Split('|'))
            {
                string kName = s.Split('&')[1];
                foreach (ProtoCrewMember k in HighLogic.CurrentGame.CrewRoster.Crew)
                {
                    if (k.name == kName)
                    {
                        kerbals.Add(k);
                        break;
                    }
                }
            }
            return kerbals;
        }

        /// <summary>
        /// Default method for the kerbalFacilities, only saves the kerbal list
        /// </summary>
        public override void EncodeString()
        {
            facilityData = CreateKerbalString(kerbals);
        }

        /// <summary>
        /// This only works if no other custom fields are saved in the facilityData
        /// </summary>
        public override void DecodeString()
        {
            if (facilityData != "")
            {
                kerbals = CreateKerbalList(facilityData);
            }
        }

        internal override void Initialize(string facilityName, int id, string facilityData, bool enabled)
        {
            kerbals = new List<ProtoCrewMember> { };
            base.Initialize(facilityName, id, facilityData, enabled);
        }
    }
}
