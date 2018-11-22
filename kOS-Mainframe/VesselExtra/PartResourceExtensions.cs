using System;
namespace kOSMainframe.VesselExtra {
    public static class PartResourceExtensions {
        /// <summary>
        ///     Gets the definition object for the resource.
        /// </summary>
        public static PartResourceDefinition GetDefinition(this PartResource resource) {
            return PartResourceLibrary.Instance.GetDefinition(resource.info.id);
        }

        /// <summary>
        ///     Gets the density of the resource.
        /// </summary>
        public static double GetDensity(this PartResource resource) {
            return resource.GetDefinition().density;
        }

        /// <summary>
        ///     Gets the mass of the resource.
        /// </summary>
        public static double GetMass(this PartResource resource) {
            return resource.amount * resource.GetDensity();
        }
    }
}
