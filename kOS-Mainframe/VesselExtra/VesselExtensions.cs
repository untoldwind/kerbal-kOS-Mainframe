using System;
using System.Collections.Generic;

namespace kOSMainframe.VesselExtra
{
	public static class VesselExtensions
    {
		public static List<T> GetModules<T>(this Vessel vessel) where T : PartModule
        {
            List<Part> parts;
            if (HighLogic.LoadedSceneIsEditor && EditorLogic.fetch != null) parts = EditorLogic.fetch.ship.parts;
            else if (vessel == null) return new List<T>();
            else parts = vessel.Parts;

            List<T> list = new List<T>();
            for (int p = 0; p < parts.Count; p++)
            {
                Part part = parts[p];

                int count = part.Modules.Count;

                for (int m = 0; m < count; m++)
                {
                    T mod = part.Modules[m] as T;

                    if (mod != null)
                        list.Add(mod);
                }
            }
            return list;
        }

		public static double TotalResourceAmount(this Vessel vessel, PartResourceDefinition definition)
        {
            if (definition == null) return 0;
            List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : vessel.parts);

            double amount = 0;
            for (int i = 0; i < parts.Count; i++)
            {
                Part p = parts[i];
                for (int j = 0; j < p.Resources.Count; j++)
                {
                    PartResource r = p.Resources[j];

                    if (r.info.id == definition.id)
                    {
                        amount += r.amount;
                    }
                }
            }

            return amount;
        }

        public static double TotalResourceAmount(this Vessel vessel, string resourceName)
        {
            return vessel.TotalResourceAmount(PartResourceLibrary.Instance.GetDefinition(resourceName));
        }

        public static double TotalResourceAmount(this Vessel vessel, int resourceId)
        {
            return vessel.TotalResourceAmount(PartResourceLibrary.Instance.GetDefinition(resourceId));
        }

        public static double TotalResourceMass(this Vessel vessel, string resourceName)
        {
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            return vessel.TotalResourceAmount(definition) * definition.density;
        }

        public static double TotalResourceMass(this Vessel vessel, int resourceId)
        {
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resourceId);
            return vessel.TotalResourceAmount(definition) * definition.density;
        }
        
		public static bool HasElectricCharge(this Vessel vessel)
        {
            if (vessel == null)
                return false;

            List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : vessel.parts);
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(PartResourceLibrary.ElectricityHashcode);
            if (definition == null) return false;

            PartResource r;
            if (vessel.GetReferenceTransformPart() != null)
            {
                r = vessel.GetReferenceTransformPart().Resources.Get(definition.id);
                // check the command pod first since most have their batteries
                if (r != null && r.amount > 0)
                    return true;
            }

            for (int i = 0; i < parts.Count; i++)
            {
                Part p = parts[i];
                r = p.Resources.Get(definition.id);
                if (r != null && r.amount > 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
