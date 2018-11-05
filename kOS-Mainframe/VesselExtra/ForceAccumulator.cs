using System;
namespace kOSMainframe.VesselExtra
{
    // This class was mostly adapted from FARCenterQuery, part of FAR, by ferram4, GPLv3
    // https://github.com/ferram4/Ferram-Aerospace-Research/blob/master/FerramAerospaceResearch/FARCenterQuery.cs
    // Also see https://en.wikipedia.org/wiki/Resultant_force

    // It accumulates forces and their points of applications, and provides methods for
    // calculating the effective torque at any position, as well as the minimum-torque net force application point.
    //
    // The latter is a non-trivial issue; there is a 1-dimensional line of physically-equivalent solutions parallel
    // to the resulting force vector; the solution closest to the weighted average of force positions is chosen.
    // In the case of non-parallel forces, there usually is an infinite number of such lines, all of which have
    // some amount of residual torque. The line with the least amount of residual torque is chosen.
    public class ForceAccumulator
    {
        // Total force.
        private Vector3d totalForce = Vector3d.zero;
        // Torque needed to compensate if force were applied at origin.
        private Vector3d totalZeroOriginTorque = Vector3d.zero;

        // Weighted average of force application points.
        private WeightedVectorAverager avgApplicationPoint = new WeightedVectorAverager();

        // Feed an force to the accumulator.
        public void AddForce(Vector3d applicationPoint, Vector3d force)
        {
            totalForce += force;
            totalZeroOriginTorque += Vector3d.Cross(applicationPoint, force);
            avgApplicationPoint.Add(applicationPoint, force.magnitude);
        }

        public Vector3d GetAverageForceApplicationPoint()
        {
            return avgApplicationPoint.Get();
        }

        public void AddForce(AppliedForce force)
        {
            AddForce(force.applicationPoint, force.vector);
        }

        // Residual torque for given force application point.
        public Vector3d TorqueAt(Vector3d origin)
        {
            return totalZeroOriginTorque - Vector3d.Cross(origin, totalForce);
        }

        // Total force vector.
        public Vector3d GetTotalForce()
        {
            return totalForce;
        }

        // Returns the minimum-residual-torque force application point that is closest to origin.
        // Note that TorqueAt(GetMinTorquePos()) is always parallel to totalForce.
        public Vector3d GetMinTorqueForceApplicationPoint(Vector3d origin)
        {
            double fmag = totalForce.sqrMagnitude;
            if (fmag <= 0)
            {
                return origin;
            }

            return origin + Vector3d.Cross(totalForce, TorqueAt(origin)) / fmag;
        }

        public Vector3d GetMinTorqueForceApplicationPoint()
        {
            return GetMinTorqueForceApplicationPoint(avgApplicationPoint.Get());
        }

        public void Reset()
        {
            totalForce = Vector3d.zero;
            totalZeroOriginTorque = Vector3d.zero;
            avgApplicationPoint.Reset();
        }
    }
}
