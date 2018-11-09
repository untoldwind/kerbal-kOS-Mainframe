using System;
namespace kOSMainframe.VesselExtra
{
    public class StageInfo
    {
        public double actualThrust = 0.0;
        public double actualThrustToWeight = 0.0;
        public double cost = 0.0;
        public double deltaV = 0.0;
        public double inverseTotalDeltaV = 0.0;
        public double isp = 0.0;
        public double mass = 0.0;
        public double rcsMass = 0.0;
        public double maxThrustToWeight = 0.0;
        public int number = 0;
        public double thrust = 0.0;
        public double thrustToWeight = 0.0;
        public double time = 0.0;
        public double totalDeltaV = 0.0;
        public double totalMass = 0.0;
        public double totalTime = 0.0;
        public int totalPartCount = 0;
        public int partCount = 0;
        public double resourceMass = 0.0;
        public double maxThrustTorque = 0.0;
        public double thrustOffsetAngle = 0.0;
        public float maxMach = 0.0f;

        //RCS
        public double RCSIsp = 0.0;
        public double RCSThrust = 0.0;
        public double RCSdeltaVStart = 0.0;
        public double RCSTWRStart = 0.0;
        public double RCSdeltaVEnd = 0.0;
        public double RCSTWREnd = 0.0;
        public double RCSBurnTime = 0.0;
    }
}
