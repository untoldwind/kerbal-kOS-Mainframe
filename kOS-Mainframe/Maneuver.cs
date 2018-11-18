using kOS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed;
using kOS.Serialization;
using kOS.Safe.Exceptions;
using kOSMainframe.Orbital;
using kOSMainframe.Utils;
using System;
using System.Reflection;


namespace kOSMainframe
{
	[kOS.Safe.Utilities.KOSNomenclature("Maneuvers")]
	public class Maneuvers : Structure, IHasSharedObjects
    {
        private const BindingFlags BindFlags = BindingFlags.Instance
                                           | BindingFlags.Public
                                           | BindingFlags.NonPublic;

        private static FieldInfo OrbitField = typeof(OrbitInfo).GetField("orbit", BindFlags);

		public SharedObjects Shared { get; set; }

		public Maneuvers(SharedObjects sharedObj)
        {
			Shared = sharedObj;
			InitializeSuffixes();
        }

		private void InitializeSuffixes()
		{
			AddSuffix("CIRCULARIZE", new OneArgsSuffix<Node, OrbitInfo>(CircularizeOrbit, "Circularize the given orbit at apoapsis if possible, otherwise at periapsis"));
			AddSuffix("CIRCULARIZE_AT", new TwoArgsSuffix<Node, OrbitInfo, ScalarValue>(CircularizeOrbitAt, "Circularize the given orbit at the given time"));
			AddSuffix("ELLIPTICIZE", new FourArgsSuffix<Node, OrbitInfo, ScalarValue, ScalarValue, ScalarValue>(EllipticizeOrbit, "Ellipticize the given orbit (Orbit, UT, newPeR, newApR"));
			AddSuffix("CHANGE_PERIAPSIS", new ThreeArgsSuffix<Node, OrbitInfo, ScalarValue, ScalarValue>(ChangePeriapsis, "Change periapsis of given orbit (Orbit, UT, newPeR"));
			AddSuffix("CHANGE_APOAPSIS", new ThreeArgsSuffix<Node, OrbitInfo, ScalarValue, ScalarValue>(ChangeApoapsis, "Change apoapsis of given orbit (Orbit, UT, newApR"));
			AddSuffix("MATCH_PLANES", new TwoArgsSuffix<Node, OrbitInfo, OrbitInfo>(MatchPlanes, "Match planes of two given orbits (orbit, target)"));
			AddSuffix("HOHMANN", new TwoArgsSuffix<Node, OrbitInfo, OrbitInfo>(Hohmann, "Regular Hohmann transfer to orbit (orbit, target)"));
			AddSuffix("BIIMPULSIVE", new TwoArgsSuffix<Node, OrbitInfo, OrbitInfo>(BiImpulsive));
		}

		private Node CircularizeOrbit(OrbitInfo orbitInfo) 
		{
			var UT = Planetarium.GetUniversalTime();
			var orbit = GetOrbitFromOrbitInfo(orbitInfo);

			if(orbit.eccentricity < 1) {
				UT = orbit.NextApoapsisTime(UT);
			} else {
				UT = orbit.NextPeriapsisTime(UT);
			}
			return CircularizeOrbitAt(orbitInfo, UT);
		}
        
		private Node CircularizeOrbitAt(OrbitInfo orbitInfo, ScalarValue UT) 
		{
			var orbit = GetOrbitFromOrbitInfo(orbitInfo);
			var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, UT);

			return NodeFromDeltaV(orbit, deltaV, UT);
		}

		private Node EllipticizeOrbit(OrbitInfo orbitInfo, ScalarValue UT, ScalarValue newPeR, ScalarValue newApR)
		{
			var orbit = GetOrbitFromOrbitInfo(orbitInfo);
			var deltaV = OrbitalManeuverCalculator.DeltaVToEllipticize(orbit, UT, newPeR, newApR);

			return NodeFromDeltaV(orbit, deltaV, UT);
		}

		private Node ChangePeriapsis(OrbitInfo orbitInfo, ScalarValue UT, ScalarValue newPeR)
		{
			var orbit = GetOrbitFromOrbitInfo(orbitInfo);
			var deltaV = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(orbit, UT, newPeR);

            return NodeFromDeltaV(orbit, deltaV, UT);
		}

		private Node ChangeApoapsis(OrbitInfo orbitInfo, ScalarValue UT, ScalarValue newApR)
		{
			var orbit = GetOrbitFromOrbitInfo(orbitInfo);
			var deltaV = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(orbit, UT, newApR);

			return NodeFromDeltaV(orbit, deltaV, UT);
		}

		private Node MatchPlanes(OrbitInfo orbitInfo, OrbitInfo targetInfo) 
		{
			var UT = Planetarium.GetUniversalTime();
			var orbit = GetOrbitFromOrbitInfo(orbitInfo);
			var target = GetOrbitFromOrbitInfo(targetInfo);
			var anExists = orbit.AscendingNodeExists(target);
			var dnExists = orbit.DescendingNodeExists(target);
			double anTime = 0;
			double dnTime = 0;
			var anDeltaV = anExists ? OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, target, UT, out anTime) : Vector3d.zero;
			var dnDeltaV = dnExists ? OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(orbit, target, UT, out dnTime) : Vector3d.zero;

			if(!anExists && !dnExists) {
				throw new KOSException("neither ascending nor descending node with target exists.");
			} else if(!dnExists || anTime < dnTime) {
				return NodeFromDeltaV(orbit, anDeltaV, anTime);
			} else {
				return NodeFromDeltaV(orbit, dnDeltaV, dnTime);
			}
		}

		private Node Hohmann(OrbitInfo orbitInfo, OrbitInfo targetInfo)
		{
			var UT = Planetarium.GetUniversalTime();
            var orbit = GetOrbitFromOrbitInfo(orbitInfo);
            var target = GetOrbitFromOrbitInfo(targetInfo);
			double burnUT = 0;
			var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(orbit, target, UT, out burnUT);

			return NodeFromDeltaV(orbit, deltaV, burnUT);
		}

		private Node BiImpulsive(OrbitInfo orbitInfo, OrbitInfo targetInfo)
		{
			var UT = Planetarium.GetUniversalTime();
            var orbit = GetOrbitFromOrbitInfo(orbitInfo);
            var target = GetOrbitFromOrbitInfo(targetInfo);
			double burnUT = 0;
			var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForBiImpulsiveAnnealed(orbit, target, UT, out burnUT);

            return NodeFromDeltaV(orbit, deltaV, burnUT);
		}

		private Node NodeFromDeltaV(Orbit orbit, Vector3d deltaV, double UT) {
			var nodeV = orbit.DeltaVToManeuverNodeCoordinates(UT, deltaV);

            return new Node(UT, nodeV.x, nodeV.y, nodeV.z, Shared);
		}

		private static Orbit GetOrbitFromOrbitInfo(OrbitInfo orbitInfo) {
			return (Orbit)OrbitField.GetValue(orbitInfo);
		}
	}
}
