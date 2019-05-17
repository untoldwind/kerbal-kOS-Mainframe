using System;
namespace kOSMainframe.Orbital {
    public interface IBody {
        double GravParameter {
            get;
        }

        double SOIRadius {
            get;
        }
    }

    public class BodyWrapper : IBody {
        private readonly CelestialBody body;

        public BodyWrapper(CelestialBody body) {
            this.body = body;
        }

        public double GravParameter => body.gravParameter;

        public double SOIRadius => body.sphereOfInfluence;
    }
}
