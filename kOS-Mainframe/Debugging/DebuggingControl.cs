using System.Collections.Generic;
using UnityEngine;
using kOSMainframe.Orbital;

namespace kOSMainframe.Debugging {
    public class DebuggingControl {
        Rect WindowRect = new Rect(100, 100, 10, 10);

        List<IWindowContent> Contents;

        private Vessel Vessel {
            get {
                return FlightGlobals.ActiveVessel;
            }
        }

        public DebuggingControl() {
            Contents = new List<IWindowContent> {
                new Button("Circularize", Circularize),
                new Param1Action("ReturnFromMoon", 100000, ReturnFromMoon),
            };
        }

        public void Draw(int instanceId) {
            GUI.skin = HighLogic.Skin;
            WindowRect = GUILayout.Window(instanceId, WindowRect, DrawWindow, "kOS-MainFrame Debug", GUILayout.ExpandHeight(true));
        }

        private void DrawWindow(int id) {
            GUILayout.BeginVertical();
            foreach (var content in Contents)
                content.Draw();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void Circularize()
        {
            CleanAndAddNode(OrbitChange.Circularize(Vessel.orbit, Planetarium.GetUniversalTime() + 20));
        }
        
        private void ReturnFromMoon(double targetPrimaryPeriapsis) {
            double burnUT = 0;
            var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(Vessel.orbit, Planetarium.GetUniversalTime(), targetPrimaryPeriapsis, out burnUT);

            var node = CleanAndAddNode(Vessel.orbit.DeltaVToNode(burnUT, deltaV));

            Logging.DumpOrbit("Current Orbit", Vessel.orbit);
            var result = Vessel.orbit.PerturbedOrbit(burnUT, deltaV);
            Logging.DumpOrbit("Result Orbit", result);
            Logging.DumpOrbit("Next node patch", node.nextPatch);
            Logging.DumpOrbit("Next node patch next", node.nextPatch.nextPatch);
            var nextTime = result.NextTimeOfRadius(Planetarium.GetUniversalTime(), result.referenceBody.sphereOfInfluence);
            Logging.Debug("NextTimeofRadius {0} {1}", nextTime, result.nextTT);
            Logging.Debug("Next node time {0}", node.nextPatch.nextPatch.StartUT);
        }

        private ManeuverNode CleanAndAddNode(NodeParameters nodeParams) {
            if(Vessel.patchedConicSolver.maneuverNodes.Count > 0 ) { 
            Vessel.patchedConicSolver.maneuverNodes[0].RemoveSelf();
            }

            return nodeParams.AddToVessel(Vessel);
        }
            }
}
