using System.Collections.Generic;
using UnityEngine;
using kOSMainframe.Orbital;
using kOSMainframe.Utils;
using kOSMainframe.Landing;

namespace kOSMainframe.Debugging {
    public class DebuggingControl : IUIDrawable {
        Rect WindowRect = new Rect(100, 100, 10, 10);

        List<IWindowContent> Contents;

        private Vessel Vessel {
            get {
                return FlightGlobals.ActiveVessel;
            }
        }

        public static void Start() {
            UIDrawer.Instance.AddDrawable(new DebuggingControl());
        }

        public DebuggingControl() {
            Contents = new List<IWindowContent> {
                new Button("Circularize", Circularize),
                new Param1Action("ReturnFromMoon", 100000, ReturnFromMoon),
                new Param2Action("Start Landing Sim", 0, 0, StartLandingSim),
                new Button("Stop Landing Sim", StopLandingSim),
                new Button("Biinjective transfer", BiinjectiveTransfer),
                new Button("Dump Orbit", DumpOrbit),
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

        private void Circularize() {
            CleanAndAddNode(OrbitChange.Circularize(Vessel.orbit, Planetarium.GetUniversalTime() + 20));
        }

        private void ReturnFromMoon(double targetPrimaryPeriapsis) {
            var nodeParams = OrbitSOIChange.MoonReturnEjection(Vessel.orbit, Planetarium.GetUniversalTime(), targetPrimaryPeriapsis);

            var node = CleanAndAddNode(nodeParams);

            Logging.DumpOrbit("Current Orbit", Vessel.orbit);
            var result = Vessel.orbit.PerturbedOrbit(nodeParams.time, nodeParams.deltaV);
            Logging.DumpOrbit("Result Orbit", result);
            Logging.DumpOrbit("Next node patch", node.nextPatch);
            Logging.DumpOrbit("Next node patch next", node.nextPatch.nextPatch);
            var nextTime = result.NextTimeOfRadius(Planetarium.GetUniversalTime(), result.referenceBody.sphereOfInfluence);
            Logging.Debug("NextTimeofRadius {0} {1}", nextTime, result.nextTT);
            Logging.Debug("Next node time {0}", node.nextPatch.nextPatch.StartUT);
        }

        private void StartLandingSim(double lat, double lon) {
            LandingSimulation.Start(Vessel, lat, lon);
        }

        private void StopLandingSim() {
            LandingSimulation.Stop();
        }

        private void BiinjectiveTransfer() {
            var target = Vessel.targetObject;
            if (target == null) return;
            var nodeParams = OrbitIntercept.BiImpulsiveAnnealed(Vessel.orbit, target.GetOrbit(), Planetarium.GetUniversalTime());

            CleanAndAddNode(nodeParams);
        }

        private ManeuverNode CleanAndAddNode(NodeParameters nodeParams) {
            if(Vessel.patchedConicSolver.maneuverNodes.Count > 0 ) {
                Vessel.patchedConicSolver.maneuverNodes[0].RemoveSelf();
            }

            return nodeParams.AddToVessel(Vessel);
        }

        private void DumpOrbit() {
            double UT = Planetarium.GetUniversalTime();
            var orbit = Vessel.orbit.referenceBody.orbit;
            Logging.DumpOrbit("Current Orbit", orbit);
            Logging.Debug("Orbit X: {0}", orbit.OrbitFrame.X);
            Logging.Debug("Orbit Y: {0}", orbit.OrbitFrame.Y);
            Logging.Debug("Orbit Z: {0}", orbit.OrbitFrame.Z);
            Logging.Debug("Orbit: meanMotion={0}", orbit.meanMotion);
            double ta = orbit.TrueAnomalyAtUT(UT);
            Logging.Debug("Orbit: UT={0} tA={1} slr={2}", UT, ta, orbit.semiLatusRectum);
            Logging.Debug("Orbit: UT={0} p={1} v={2}", UT, Planetarium.Zup.LocalToWorld( orbit.getRelativePositionAtUT(UT)), Planetarium.Zup.LocalToWorld( orbit.getOrbitalVelocityAtUT(UT)));
            Logging.Debug("Zup: X={0} Y={1} Z={2}", Planetarium.Zup.X, Planetarium.Zup.Y, Planetarium.Zup.Z);
            Logging.Debug("InvRot: {0}", Planetarium.InverseRotAngle);
        }

    }
}
