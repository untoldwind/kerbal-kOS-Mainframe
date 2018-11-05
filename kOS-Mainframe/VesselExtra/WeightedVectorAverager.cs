using System;
namespace kOSMainframe.VesselExtra
{
    public class WeightedVectorAverager
    {
        private Vector3d sum = Vector3d.zero;
        private double totalweight = 0;

        public void Add(Vector3d v, double weight)
        {
            sum += v * weight;
            totalweight += weight;
        }

        public Vector3d Get()
        {
            if (totalweight > 0)
            {
                return sum / totalweight;
            }
            else
            {
                return Vector3d.zero;
            }
        }

        public double GetTotalWeight()
        {
            return totalweight;
        }

        public void Reset()
        {
            sum = Vector3d.zero;
            totalweight = 0.0;
        }
    }
}
