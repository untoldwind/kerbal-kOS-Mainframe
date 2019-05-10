using System;
namespace kOSMainframe.Orbital {
    public interface IBody {
        double gravParameter {
            get;
        }
    }

    public class BodyWrapper : IBody {
        private readonly CelestialBody body;

        public BodyWrapper(CelestialBody body) {
            this.body = body;
        }

        public double gravParameter => body.gravParameter;
    }
}
