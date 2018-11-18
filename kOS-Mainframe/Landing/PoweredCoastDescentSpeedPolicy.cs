using System;

namespace kOSMainframe.Landing
{
    class PoweredCoastDescentSpeedPolicy : IDescentSpeedPolicy
    {
        float terrainRadius;
        float g;
        float thrust;

        public PoweredCoastDescentSpeedPolicy(double terrainRadius, double g, double thrust)
        {
            this.terrainRadius = (float)terrainRadius;
            this.g = (float)g;
            this.thrust = (float)thrust;
        }

        public double MaxAllowedSpeed(Vector3d pos, Vector3d vel)
        {

            if (terrainRadius < pos.magnitude)
                return Double.MaxValue;

            double vSpeed = Vector3d.Dot(vel, pos.normalized);
            double ToF = (vSpeed + Math.Sqrt(vSpeed * vSpeed + 2 * g * (pos.magnitude - terrainRadius))) / g;

            //MechJebCore.print("ToF = " + ToF.ToString("F2"));
            return 0.8 * (thrust - g) * ToF;
        }
    }
}
