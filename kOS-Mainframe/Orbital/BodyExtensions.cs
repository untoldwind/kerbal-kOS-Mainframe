namespace kOSMainframe.Orbital {
    public static class BodyExtensions {
        public static IBody wrap(this CelestialBody body) {
            return new BodyWrapper(body);
        }
    }
}
