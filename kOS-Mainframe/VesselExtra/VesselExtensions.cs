using System;
using System.Collections.Generic;
using System.Linq;
using Smooth.Slinq;
using kOSMainframe.Orbital;

namespace kOSMainframe.VesselExtra {
    public static class VesselExtensions {
        public static List<T> GetModules<T>(this Vessel vessel) where T : PartModule {
            List<Part> parts;
            if (HighLogic.LoadedSceneIsEditor && EditorLogic.fetch != null) parts = EditorLogic.fetch.ship.parts;
            else if (vessel == null) return new List<T>();
            else parts = vessel.Parts;

            List<T> list = new List<T>();
            for (int p = 0; p < parts.Count; p++) {
                Part part = parts[p];

                int count = part.Modules.Count;

                for (int m = 0; m < count; m++) {
                    T mod = part.Modules[m] as T;

                    if (mod != null)
                        list.Add(mod);
                }
            }
            return list;
        }

        public static double TotalResourceAmount(this Vessel vessel, PartResourceDefinition definition) {
            if (definition == null) return 0;
            List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : vessel.parts);

            double amount = 0;
            for (int i = 0; i < parts.Count; i++) {
                Part p = parts[i];
                for (int j = 0; j < p.Resources.Count; j++) {
                    PartResource r = p.Resources[j];

                    if (r.info.id == definition.id) {
                        amount += r.amount;
                    }
                }
            }

            return amount;
        }

        public static double TotalResourceAmount(this Vessel vessel, string resourceName) {
            return vessel.TotalResourceAmount(PartResourceLibrary.Instance.GetDefinition(resourceName));
        }

        public static double TotalResourceAmount(this Vessel vessel, int resourceId) {
            return vessel.TotalResourceAmount(PartResourceLibrary.Instance.GetDefinition(resourceId));
        }

        public static double TotalResourceMass(this Vessel vessel, string resourceName) {
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            return vessel.TotalResourceAmount(definition) * definition.density;
        }

        public static double TotalResourceMass(this Vessel vessel, int resourceId) {
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resourceId);
            return vessel.TotalResourceAmount(definition) * definition.density;
        }

        public static bool HasElectricCharge(this Vessel vessel) {
            if (vessel == null)
                return false;

            List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : vessel.parts);
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(PartResourceLibrary.ElectricityHashcode);
            if (definition == null) return false;

            PartResource r;
            if (vessel.GetReferenceTransformPart() != null) {
                r = vessel.GetReferenceTransformPart().Resources.Get(definition.id);
                // check the command pod first since most have their batteries
                if (r != null && r.amount > 0)
                    return true;
            }

            for (int i = 0; i < parts.Count; i++) {
                Part p = parts[i];
                r = p.Resources.Get(definition.id);
                if (r != null && r.amount > 0) {
                    return true;
                }
            }
            return false;
        }

        public static double GetAltitudeASL(this Vessel vessel) {
            return vessel.mainBody.GetAltitude(vessel.CoMD);
        }

        public static Vector3d GetSurfaceVelocity(this Vessel vessel) {
            return vessel.obt_velocity - vessel.mainBody.getRFrmVel(vessel.CoMD);
        }

        public static double GetSpeedSurface(this Vessel vessel) {
            return vessel.GetSurfaceVelocity().magnitude;
        }

        public static double GetSpeedSurfaceHorizontal(this Vessel vessel) {
            Vector3d up = (vessel.CoMD - vessel.mainBody.position).normalized;
            return Vector3d.Exclude(up, vessel.GetSurfaceVelocity()).magnitude;
        }

        public static double GetSpeedVertical(this Vessel vessel) {
            Vector3d up = (vessel.CoMD - vessel.mainBody.position).normalized;
            return Vector3d.Dot(vessel.GetSurfaceVelocity(), up);
        }

        //Computes the angle between two orbital planes. This will be a number between 0 and 180
        //Note that in the convention used two objects orbiting in the same plane but in
        //opposite directions have a relative inclination of 180 degrees.
        public static double RelativeInclination(this Orbit a, Orbit b) {
            return Math.Abs(Vector3d.Angle(a.SwappedOrbitNormal(), b.SwappedOrbitNormal()));
        }

        //If there is a maneuver node on this patch, returns the patch that follows that maneuver node
        //Otherwise, if this patch ends in an SOI transition, returns the patch that follows that transition
        //Otherwise, returns null
        public static Orbit GetNextPatch(this Vessel vessel, Orbit patch, ManeuverNode ignoreNode = null) {
            //Determine whether this patch ends in an SOI transition or if it's the final one:
            bool finalPatch = (patch.patchEndTransition == Orbit.PatchTransitionType.FINAL);

            //See if any maneuver nodes occur during this patch. If there is one
            //return the patch that follows it
            var nodes = vessel.patchedConicSolver.maneuverNodes.Slinq().Where((n, p) => n.patch == p && n != ignoreNode, patch);
            // Slinq is nice but you can only enumerate it once
            var first = nodes.FirstOrDefault();
            if (first != null) return first.nextPatch;

            //return the next patch, or null if there isn't one:
            if (!finalPatch) return patch.nextPatch;
            else return null;
        }

        //input dV should be in world coordinates
        public static ManeuverNode PlaceManeuverNode(this Vessel vessel, Orbit patch, Vector3d dV, double UT) {
            //placing a maneuver node with bad dV values can really mess up the game, so try to protect against that
            //and log an exception if we get a bad dV vector:
            for (int i = 0; i < 3; i++) {
                if (double.IsNaN(dV[i]) || double.IsInfinity(dV[i])) {
                    throw new Exception("MechJeb VesselExtensions.PlaceManeuverNode: bad dV: " + dV);
                }
            }

            if (double.IsNaN(UT) || double.IsInfinity(UT)) {
                throw new Exception("MechJeb VesselExtensions.PlaceManeuverNode: bad UT: " + UT);
            }

            //It seems that sometimes the game can freak out if you place a maneuver node in the past, so this
            //protects against that.
            UT = Math.Max(UT, Planetarium.GetUniversalTime());

            //convert a dV in world coordinates into the coordinate system of the maneuver node,
            //which uses (x, y, z) = (radial+, normal-, prograde)
            NodeParameters nodeParams = patch.DeltaVToNode(UT, dV);
            ManeuverNode mn = vessel.patchedConicSolver.AddManeuverNode(UT);
            mn.DeltaV = nodeParams.NodeDeltaV;
            vessel.patchedConicSolver.UpdateFlightPlan();
            return mn;
        }

        public static void UpdateNode(this ManeuverNode node, NodeParameters nodeParameters) {
            node.DeltaV = nodeParameters.NodeDeltaV;
            node.UT = nodeParameters.time;
            node.solver.UpdateFlightPlan();
            if (node.attachedGizmo == null)
                return;
            node.attachedGizmo.patchBefore = node.patch;
            node.attachedGizmo.patchAhead = node.nextPatch;
        }
    }
}
