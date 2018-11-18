using kOS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed;
using kOS.Serialization;
using kOS.Safe.Exceptions;
using kOSMainframe.Orbital;
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
        private readonly Orbit orbit;
        private double minUT;

		public Maneuvers(SharedObjects sharedObj, OrbitInfo orbitInfo)
        {
			Shared = sharedObj;
            this.orbit = GetOrbitFromOrbitInfo(orbitInfo);
            this.minUT = Planetarium.GetUniversalTime();
            InitializeSuffixes();
        }

		private void InitializeSuffixes()
		{
            AddSuffix("MIN_TIME", new SetSuffix<TimeSpan>(GetMinTime, SetMinTime));
			AddSuffix("CIRCULARIZE", new NoArgsSuffix<Node>(CircularizeOrbit, "Circularize the given orbit at apoapsis if possible, otherwise at periapsis"));
			AddSuffix("CIRCULARIZE_AT", new OneArgsSuffix<Node, ScalarValue>(CircularizeOrbitAt, "Circularize the given orbit at the given time"));
			AddSuffix("ELLIPTICIZE", new ThreeArgsSuffix<Node, ScalarValue, ScalarValue, ScalarValue>(EllipticizeOrbit, "Ellipticize the given orbit (UT, newPeR, newApR"));
			AddSuffix("CHANGE_PERIAPSIS", new TwoArgsSuffix<Node, ScalarValue, ScalarValue>(ChangePeriapsis, "Change periapsis of given orbit (UT, newPeR"));
			AddSuffix("CHANGE_APOAPSIS", new TwoArgsSuffix<Node, ScalarValue, ScalarValue>(ChangeApoapsis, "Change apoapsis of given orbit (UT, newApR"));
			AddSuffix("MATCH_PLANES", new OneArgsSuffix<Node, OrbitInfo>(MatchPlanes, "Match planes of given orbit with target orbit"));
			AddSuffix("HOHMANN", new OneArgsSuffix<Node, OrbitInfo>(Hohmann, "Regular Hohmann transfer from given orbit to target orbit"));
			AddSuffix("BIIMPULSIVE", new OneArgsSuffix<Node, OrbitInfo>(BiImpulsive));
		}

        private TimeSpan GetMinTime()
        {
            return new TimeSpan(minUT);
        }

        private void SetMinTime(TimeSpan minTime)
        {
            this.minUT = minTime.ToUnixStyleTime();
        }

        private Node CircularizeOrbit() 
		{
            var UT = minUT;
			if(orbit.eccentricity < 1) {
				UT = orbit.NextApoapsisTime(UT);
			} else {
				UT = orbit.NextPeriapsisTime(UT);
			}
			return CircularizeOrbitAt(UT);
		}
        
		private Node CircularizeOrbitAt(ScalarValue UT) 
		{
			var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, System.Math.Max(UT, minUT));

			return NodeFromDeltaV(deltaV, UT);
		}

		private Node EllipticizeOrbit(ScalarValue UT, ScalarValue newPeR, ScalarValue newApR)
		{
			var deltaV = OrbitalManeuverCalculator.DeltaVToEllipticize(orbit, System.Math.Max(UT, minUT), newPeR, newApR);

			return NodeFromDeltaV(deltaV, UT);
		}

		private Node ChangePeriapsis(ScalarValue UT, ScalarValue newPeR)
		{
			var deltaV = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(orbit, System.Math.Max(UT, minUT), newPeR);

            return NodeFromDeltaV(deltaV, UT);
		}

		private Node ChangeApoapsis(ScalarValue UT, ScalarValue newApR)
		{
			var deltaV = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(orbit, System.Math.Max(UT, minUT), newApR);

			return NodeFromDeltaV(deltaV, UT);
		}

		private Node MatchPlanes(OrbitInfo targetInfo) 
		{
			var target = GetOrbitFromOrbitInfo(targetInfo);
			var anExists = orbit.AscendingNodeExists(target);
			var dnExists = orbit.DescendingNodeExists(target);
			double anTime = 0;
			double dnTime = 0;
			var anDeltaV = anExists ? OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, target, minUT, out anTime) : Vector3d.zero;
			var dnDeltaV = dnExists ? OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(orbit, target, minUT, out dnTime) : Vector3d.zero;

			if(!anExists && !dnExists) {
				throw new KOSException("neither ascending nor descending node with target exists.");
			} else if(!dnExists || anTime < dnTime) {
				return NodeFromDeltaV(anDeltaV, anTime);
			} else {
				return NodeFromDeltaV(dnDeltaV, dnTime);
			}
		}

		private Node Hohmann(OrbitInfo targetInfo)
		{
			var UT = Planetarium.GetUniversalTime();
            var target = GetOrbitFromOrbitInfo(targetInfo);
			double burnUT = 0;
			var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(orbit, target, minUT, out burnUT);

			return NodeFromDeltaV(deltaV, burnUT);
		}

		private Node BiImpulsive(OrbitInfo targetInfo)
		{
			var UT = Planetarium.GetUniversalTime();
            var target = GetOrbitFromOrbitInfo(targetInfo);
			double burnUT = 0;
			var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForBiImpulsiveAnnealed(orbit, target, minUT, out burnUT);

            return NodeFromDeltaV(deltaV, burnUT);
		}

		private Node NodeFromDeltaV(Vector3d deltaV, double UT) {
			var nodeV = orbit.DeltaVToManeuverNodeCoordinates(UT, deltaV);

            return new Node(UT, nodeV.x, nodeV.y, nodeV.z, Shared);
		}

		private static Orbit GetOrbitFromOrbitInfo(OrbitInfo orbitInfo) {
			return (Orbit)OrbitField.GetValue(orbitInfo);
		}
	}
}
