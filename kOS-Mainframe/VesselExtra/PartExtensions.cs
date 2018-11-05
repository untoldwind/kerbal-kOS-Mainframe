using System;
using System.Collections.Generic;
using CompoundParts;

namespace kOSMainframe.VesselExtra
{
    public static class PartExtensions
    {
        /// <summary>
        ///     Gets whether the part contains a specific resource.
        /// </summary>
        public static bool ContainsResource(this Part part, int resourceId)
        {
            return part.Resources.Contains(resourceId);
        }

        /// <summary>
        ///     Gets whether the part contains resources.
        /// </summary>
        public static bool ContainsResources(this Part part)
        {
            for (int i = 0; i < part.Resources.dict.Count; ++i)
            {
                if (part.Resources.dict.At(i).amount > 0.0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Gets the dry mass of the part.
        /// </summary>
        public static double GetDryMass(this Part part)
        {
            return (part.physicalSignificance == Part.PhysicalSignificance.FULL) ? part.mass: 0d;
        }

        /// <summary>
        ///     Gets a typed list of PartModules.
        /// </summary>
        public static List<T> GetModules<T>(this Part part) where T : PartModule
        {
            List<T> list = new List<T>();
            for (int i = 0; i < part.Modules.Count; ++i)
            {
                T module = part.Modules[i] as T;
                if (module != null)
                {
                    list.Add(module);
                }
            }
            return list;
        }

        /// <summary>
        ///     Gets the first typed PartModule in the part's module list.
        /// </summary>
        public static T GetModule<T>(this Part part) where T : PartModule
        {
            for (int i = 0; i < part.Modules.Count; i++)
            {
                PartModule pm = part.Modules[i];
                if (pm is T)
                    return (T)pm;
            }
            return null;
        }

        /// <summary>
        ///     Gets whether the part contains a PartModule.
        /// </summary>
        public static bool HasModule<T>(this Part part) where T : PartModule
        {
            for (int i = 0; i < part.Modules.Count; i++)
            {
                if (part.Modules[i] is T)
                    return true;
            }
            return false;
        }

        /// <summary>
        ///     Gets whether the part is a decoupler.
        /// </summary>
        public static bool IsDecoupler(this Part part)
        {
            return HasModule<ModuleDecouple>(part) || HasModule<ModuleAnchoredDecoupler>(part);
        }

        /// <summary>
        ///     Gets whether the part is an active engine.
        /// </summary>
        public static bool IsEngine(this Part part)
        {
            return HasModule<ModuleEngines>(part);
        }

        /// <summary>
        ///     Gets whether the part is a fuel line.
        /// </summary>
        public static bool IsFuelLine(this Part part)
        {
            return HasModule<CModuleFuelLine>(part);
        }

        /// <summary>
        ///     Gets whether the part is a generator.
        /// </summary>
        public static bool IsGenerator(this Part part)
        {
            return HasModule<ModuleGenerator>(part);
        }

        /// <summary>
        ///     Gets whether the part is a launch clamp.
        /// </summary>
        public static bool IsLaunchClamp(this Part part)
        {
            return HasModule<LaunchClamp>(part);
        }

        /// <summary>
        ///     Gets whether the part is a parachute.
        /// </summary>
        public static bool IsParachute(this Part part)
        {
            return HasModule<ModuleParachute>(part);
        }

    }
}
