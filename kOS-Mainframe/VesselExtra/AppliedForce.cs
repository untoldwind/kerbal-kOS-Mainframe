using System;
namespace kOSMainframe.VesselExtra
{
    public class AppliedForce
    {
        private static readonly Pool<AppliedForce> pool = new Pool<AppliedForce>(Create, Reset);

        public Vector3d vector;
        public Vector3d applicationPoint;

        static private AppliedForce Create()
        {
            return new AppliedForce();
        }

        static private void Reset(AppliedForce appliedForce) { }

        static public AppliedForce New(Vector3d vector, Vector3d applicationPoint)
        {
            AppliedForce force = pool.Borrow();
            force.vector = vector;
            force.applicationPoint = applicationPoint;
            return force;
        }

        public void Release()
        {
            pool.Release(this);
        }
    }
}
