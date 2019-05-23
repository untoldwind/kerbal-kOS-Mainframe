using System;
using UnityEngine;

namespace kOSMainframe.Numerics {
    public static class AnnealingOptimizer {
        /// <summary>
        /// Simulate annealing of particles settling down in the global minima
        /// of a potential when it gets cooler.
        /// This is a suprisingly robust stochastic optimization to find the
        /// global minima of a function. For best results this should be combined
        /// with a local optimizer.
        /// The main issue with this method is to find the right starting temperature
        /// for your problem. If the starting temperature is too high the
        /// particles will just erratically jump around, if it is too low chances
        /// are that they just settle down in some local minima.
        /// </summary>
        public static Vector2d[] Optimize(Func2 func, Vector2d min, Vector2d max, double maxTemp, out Vector2d best, int iters = 5000, int numParticles = 10, double coolingRate = 0.003) {
            System.Random random = new System.Random();
            double temp = maxTemp;
            Vector2d range = max - min;
            Vector2d window = new Vector2d();
            double[] particlesF = new double[numParticles];
            Vector2d[] particles = new Vector2d[numParticles];

            for(int i = 0; i < numParticles; i++) {
                particles[i] = i == 0 ? (range / 2.0) : new Vector2d(random.NextDouble() * range.x + min.x, random.NextDouble() * range.y + min.y);
                particlesF[i] = func(particles[i].x, particles[i].y);
            }

            for(int n = iters - 1; n >= 0; n-- ) {
                window.x = (0.5 * n) / iters * range.x;
                window.y = (0.5 * n) / iters * range.y;

                // Randomly choose a particle
                int i = random.Next() % numParticles;
                // ... and perdurb its position a bit
                double x = ExtraMath.Clamp(random.NextGaussian(particles[i].x, window.x), min.x, max.x);
                double y = ExtraMath.Clamp(random.NextGaussian(particles[i].y, window.y), min.y, max.y);
                double f = func(x, y);

                // Now find a particle that wants to jump to this position
                double P = random.NextDouble();
                // See if this particle wants to jump there
                if (f < particlesF[i] || Math.Exp((particlesF[i] - f) / temp) > P) {
                    particles[i].x = x;
                    particles[i].y = y;
                    particlesF[i] = f;
                } else {
                    // See if a random another particle want to jump there
                    i = random.Next(1) % numParticles;
                    if (f < particlesF[i] || Math.Exp((particlesF[i] - f) / temp) > P) {
                        particles[i].x = x;
                        particles[i].y = y;
                        particlesF[i] = f;
                    }
                }

                // And cool it down a bit
                temp *= 1 - coolingRate;
            }

            double bestF = particlesF[0];
            best = particles[0];
            for(int i = 1; i < numParticles; i++ ) {
                if(particlesF[i] < bestF) {
                    bestF = particlesF[i];
                    best = particles[i];
                }
            }
            return particles;
        }
    }
}
