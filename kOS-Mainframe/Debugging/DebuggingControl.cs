using System.Collections.Generic;
using UnityEngine;
using kOSMainframe.Orbital;

namespace kOSMainframe.Debugging {
    public class DebuggingControl {
        Rect WindowRect = new Rect(50, 50, 10, 10);

        GUILayoutOption[] WindowOptions = new[] { GUILayout.ExpandHeight(true) };

        List<IWindowContent> Contents;

        private Vessel Vessel {
            get {
                return FlightGlobals.ActiveVessel;
            }
        }

        public DebuggingControl() {
            Contents = new List<IWindowContent> {
                new Button("ReturnFromMoon", ReturnFromMoon),
            };
        }

        public void Draw(int instanceId) {
            GUI.skin = HighLogic.Skin;
            GUILayout.Window(instanceId, WindowRect, DrawWindow, "kOS-MainFrame Debug", WindowOptions);
        }

        private void DrawWindow(int id) {
            GUILayout.BeginVertical();
            foreach (var content in Contents)
                content.Draw();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void ReturnFromMoon() {
            double burnUT = 0;
            var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(Vessel.orbit, Vessel.missionTime, 100000, out burnUT);

            AddNodeFromDeltaV(deltaV, burnUT);
        }

        private void AddNodeFromDeltaV(Vector3d deltaV, double UT) {
            foreach (var n in Vessel.patchedConicSolver.maneuverNodes) {
                Vessel.patchedConicSolver.RemoveManeuverNode(n);
            }

            var nodeV = Vessel.orbit.DeltaVToManeuverNodeCoordinates(UT, deltaV);
            var node = Vessel.patchedConicSolver.AddManeuverNode(UT);

            node.DeltaV = nodeV;

            Vessel.patchedConicSolver.UpdateFlightPlan();
        }
    }
}
