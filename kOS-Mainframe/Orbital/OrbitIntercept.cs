using UnityEngine;
using System;
using kOSMainframe.ExtraMath;

namespace kOSMainframe.Orbital {
    public static class OrbitIntercept {
        public static Vector3d DeltaVToInterceptAtTime(Orbit o, double UT, Orbit target, double interceptUT, double offsetDistance = 0, bool shortway = true) {
            Vector3d finalVelocity;
            return DeltaVToInterceptAtTime(o, UT, target, interceptUT, out finalVelocity, offsetDistance, shortway);
        }

        // Computes the delta-V of a burn at a given time that will put an object with a given orbit on a
        // course to intercept a target at a specific interceptUT.
        //
        // offsetDistance: this is used by the Rendezvous Autopilot and is only going to be valid over very short distances
        // shortway: the shortway parameter to feed into the Lambert solver
        //
        public static Vector3d DeltaVToInterceptAtTime(Orbit o, double initialUT, Orbit target, double finalUT, out Vector3d secondDV, double offsetDistance = 0, bool shortway = true) {
            Vector3d initialRelPos = o.SwappedRelativePositionAtUT(initialUT);
            Vector3d finalRelPos = target.SwappedRelativePositionAtUT(finalUT);

            Vector3d initialVelocity = o.SwappedOrbitalVelocityAtUT(initialUT);
            Vector3d finalVelocity = target.SwappedOrbitalVelocityAtUT(finalUT);

            Vector3d transferVi, transferVf;

            LambertSolver.Solve(initialRelPos, finalRelPos, finalUT - initialUT, o.referenceBody.gravParameter, shortway, out transferVi, out transferVf);
            // GoodingSolver.Solve(initialRelPos, initialVelocity, finalRelPos, finalVelocity, finalUT - initialUT, o.referenceBody, 0, out transferVi, out transferVf);

            if (offsetDistance != 0) {
                finalRelPos += offsetDistance * Vector3d.Cross(finalVelocity, finalRelPos).normalized;
                LambertSolver.Solve(initialRelPos, finalRelPos, finalUT - initialUT, o.referenceBody.gravParameter, shortway, out transferVi, out transferVf);
                //GoodingSolver.Solve(initialRelPos, initialVelocity, finalRelPos, finalVelocity, finalUT - initialUT, o.referenceBody, 0, out transferVi, out transferVf);
            }

            secondDV = finalVelocity - transferVf;

            return transferVi - initialVelocity;
        }

        //First finds the time of closest approach to the target during the next orbit after the
        //time UT. Then returns the delta-V of a burn at UT that will change the separation at
        //that closest approach time to zero.
        //This will likely only return sensible results when the given orbit is already an
        //approximate intercept trajectory.
        public static NodeParameters CourseCorrection(Orbit o, double UT, Orbit target) {
            double closestApproachTime = o.NextClosestApproachTime(target, UT + 1); //+1 so that closestApproachTime is definitely > UT
            Vector3d dV = DeltaVToInterceptAtTime(o, UT, target, closestApproachTime);
            return o.DeltaVToNode(UT, dV);
        }

        public static NodeParameters CheapestCourseCorrection(Orbit o, double UT, Orbit target) {
            double closestApproachTime = o.NextClosestApproachTime(target, UT + 2); //+2 so that closestApproachTime is definitely > UT

            double burnUT = UT;
            Vector3d dV = DeltaVToInterceptAtTime(o, burnUT, target, closestApproachTime);

            const int fineness = 20;
            for (double step = 0.5; step < fineness; step += 1.0) {
                double testUT = UT + (closestApproachTime - UT) * step / fineness;
                Vector3d testDV = DeltaVToInterceptAtTime(o, testUT, target, closestApproachTime);

                if (testDV.magnitude < dV.magnitude) {
                    dV = testDV;
                    burnUT = testUT;
                }
            }

            return o.DeltaVToNode(burnUT, dV);
        }

        public static NodeParameters CheapestCourseCorrection(Orbit o, double UT, Orbit target, CelestialBody targetBody, double finalPeR) {
            NodeParameters collisionParams = CheapestCourseCorrection(o, UT, target);
            Orbit collisionOrbit = o.PerturbedOrbit(collisionParams);
            double collisionUT = collisionOrbit.NextClosestApproachTime(target, collisionParams.time);
            Vector3d collisionPosition = target.SwappedAbsolutePositionAtUT(collisionUT);
            Vector3d collisionRelVel = collisionOrbit.SwappedOrbitalVelocityAtUT(collisionUT) - target.SwappedOrbitalVelocityAtUT(collisionUT);

            double soiEnterUT = collisionUT - targetBody.sphereOfInfluence / collisionRelVel.magnitude;
            Vector3d soiEnterRelVel = collisionOrbit.SwappedOrbitalVelocityAtUT(soiEnterUT) - target.SwappedOrbitalVelocityAtUT(soiEnterUT);

            double E = 0.5 * soiEnterRelVel.sqrMagnitude - targetBody.gravParameter / targetBody.sphereOfInfluence; //total orbital energy on SoI enter
            double finalPeSpeed = Math.Sqrt(2 * (E + targetBody.gravParameter / finalPeR)); //conservation of energy gives the orbital speed at finalPeR.
            double desiredImpactParameter = finalPeR * finalPeSpeed / soiEnterRelVel.magnitude; //conservation of angular momentum gives the required impact parameter

            Vector3d displacementDir = Vector3d.Cross(collisionRelVel, o.SwappedOrbitNormal()).normalized;
            Vector3d interceptTarget = collisionPosition + desiredImpactParameter * displacementDir;

            Vector3d velAfterBurn;
            Vector3d arrivalVel;
            LambertSolver.Solve(o.SwappedRelativePositionAtUT(collisionParams.time), interceptTarget - o.referenceBody.position, collisionUT - collisionParams.time, o.referenceBody.gravParameter, true, out velAfterBurn, out arrivalVel);

            Vector3d deltaV = velAfterBurn - o.SwappedOrbitalVelocityAtUT(collisionParams.time);
            return o.DeltaVToNode(collisionParams.time, deltaV);
        }

        public static NodeParameters CheapestCourseCorrection(Orbit o, double UT, Orbit target, double caDistance) {
            NodeParameters collisionParams = CheapestCourseCorrection(o, UT, target);
            Orbit collisionOrbit = o.PerturbedOrbit(collisionParams);
            double collisionUT = collisionOrbit.NextClosestApproachTime(target, collisionParams.time);
            Vector3d position = o.SwappedAbsolutePositionAtUT(collisionUT);
            Vector3d targetPos = target.SwappedAbsolutePositionAtUT(collisionUT);
            Vector3d direction = targetPos - position;

            Vector3d interceptTarget = targetPos + target.NormalPlus(collisionUT) * caDistance;

            Vector3d velAfterBurn;
            Vector3d arrivalVel;
            LambertSolver.Solve(o.SwappedRelativePositionAtUT(collisionParams.time), interceptTarget - o.referenceBody.position, collisionUT - collisionParams.time, o.referenceBody.gravParameter, true, out velAfterBurn, out arrivalVel);

            Vector3d deltaV = velAfterBurn - o.SwappedOrbitalVelocityAtUT(collisionParams.time);
            return o.DeltaVToNode(collisionParams.time, deltaV);
        }

        //Computes the dV of a Hohmann transfer burn at time UT that will put the apoapsis or periapsis
        //of the transfer orbit on top of the target orbit.
        //The output value apsisPhaseAngle is the phase angle between the transferring vessel and the
        //target object as the transferring vessel crosses the target orbit at the apoapsis or periapsis
        //of the transfer orbit.
        //Actually, it's not exactly the phase angle. It's a sort of mean anomaly phase angle. The
        //difference is not important for how this function is used by DeltaVAndTimeForHohmannTransfer.
        private static Vector3d DeltaVAndApsisPhaseAngleOfHohmannTransfer(Orbit o, Orbit target, double UT, out double apsisPhaseAngle) {
            Vector3d apsisDirection = -o.SwappedRelativePositionAtUT(UT);
            double desiredApsis = target.RadiusAtTrueAnomaly(UtilMath.Deg2Rad * target.TrueAnomalyFromVector(apsisDirection));

            Vector3d dV;
            if (desiredApsis > o.ApR) {
                dV = OrbitChange.ChangeApoapsis(o, UT, desiredApsis).deltaV;
                Orbit transferOrbit = o.PerturbedOrbit(UT, dV);
                double transferApTime = transferOrbit.NextApoapsisTime(UT);
                Vector3d transferApDirection = transferOrbit.SwappedRelativePositionAtApoapsis();  // getRelativePositionAtUT was returning NaNs! :(((((
                double targetTrueAnomaly = target.TrueAnomalyFromVector(transferApDirection);
                double meanAnomalyOffset = 360 * (target.TimeOfTrueAnomaly(targetTrueAnomaly, UT) - transferApTime) / target.period;
                apsisPhaseAngle = meanAnomalyOffset;
            } else {
                dV = OrbitChange.ChangePeriapsis(o, UT, desiredApsis).deltaV;
                Orbit transferOrbit = o.PerturbedOrbit(UT, dV);
                double transferPeTime = transferOrbit.NextPeriapsisTime(UT);
                Vector3d transferPeDirection = transferOrbit.SwappedRelativePositionAtPeriapsis();  // getRelativePositionAtUT was returning NaNs! :(((((
                double targetTrueAnomaly = target.TrueAnomalyFromVector(transferPeDirection);
                double meanAnomalyOffset = 360 * (target.TimeOfTrueAnomaly(targetTrueAnomaly, UT) - transferPeTime) / target.period;
                apsisPhaseAngle = meanAnomalyOffset;
            }

            apsisPhaseAngle = Functions.ClampDegrees180(apsisPhaseAngle);

            return dV;
        }

        //Computes the time and dV of a Hohmann transfer injection burn such that at apoapsis the transfer
        //orbit passes as close as possible to the target.
        //The output burnUT will be the first transfer window found after the given UT.
        //Assumes o and target are in approximately the same plane, and orbiting in the same direction.
        //Also assumes that o is a perfectly circular orbit (though result should be OK for small eccentricity).
        public static NodeParameters HohmannTransfer(Orbit o, Orbit target, double UT) {
            //We do a binary search for the burn time that zeros out the phase angle between the
            //transferring vessel and the target at the apsis of the transfer orbit.
            double synodicPeriod = o.SynodicPeriod(target);

            double lastApsisPhaseAngle;
            Vector3d immediateBurnDV = DeltaVAndApsisPhaseAngleOfHohmannTransfer(o, target, UT, out lastApsisPhaseAngle);

            double minTime = UT;
            double maxTime = UT + 1.5 * synodicPeriod;

            //first find roughly where the zero point is
            const int numDivisions = 30;
            double dt = (maxTime - minTime) / numDivisions;
            for (int i = 1; i <= numDivisions; i++) {
                double t = minTime + dt * i;

                double apsisPhaseAngle;
                DeltaVAndApsisPhaseAngleOfHohmannTransfer(o, target, t, out apsisPhaseAngle);

                if ((Math.Abs(apsisPhaseAngle) < 90) && (Math.Sign(lastApsisPhaseAngle) != Math.Sign(apsisPhaseAngle))) {
                    minTime = t - dt;
                    maxTime = t;
                    break;
                }

                if ((i == 1) && (Math.Abs(lastApsisPhaseAngle) < 0.5) && (Math.Sign(lastApsisPhaseAngle) == Math.Sign(apsisPhaseAngle))) {
                    //In this case we are JUST passed the center of the transfer window, but probably we
                    //can still do the transfer just fine. Don't do a search, just return an immediate burn
                    return o.DeltaVToNode(UT, immediateBurnDV);
                }

                lastApsisPhaseAngle = apsisPhaseAngle;

                if (i == numDivisions) {
                    throw new ArgumentException("OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer: couldn't find the transfer window!!");
                }
            }

            int minTimeApsisPhaseAngleSign = Math.Sign(lastApsisPhaseAngle);

            //then do a binary search
            while (maxTime - minTime > 0.01) {
                double testTime = (maxTime + minTime) / 2;

                double testApsisPhaseAngle;
                DeltaVAndApsisPhaseAngleOfHohmannTransfer(o, target, testTime, out testApsisPhaseAngle);

                if (Math.Sign(testApsisPhaseAngle) == minTimeApsisPhaseAngleSign) {
                    minTime = testTime;
                } else {
                    maxTime = testTime;
                }
            }

            double burnUT = (minTime + maxTime) / 2;
            double finalApsisPhaseAngle;
            Vector3d burnDV = DeltaVAndApsisPhaseAngleOfHohmannTransfer(o, target, burnUT, out finalApsisPhaseAngle);

            return o.DeltaVToNode(burnUT, burnDV);
        }

        public struct LambertProblem {
            public Orbit o;
            public Orbit target;
            public bool shortway;
            public bool intercept_only;  // omit the second burn from the cost
            public double zeroUT;
        }

        // x[0] is the burn time before/after zeroUT
        // x[1] is the time of the transfer
        //
        // f[1] is the cost of the burn
        //
        // prob.shortway is which lambert solution to find
        // prob.intercept_only omits adding the second burn to the cost
        //
        public static void LambertCost(double[] x, double[] f, object obj) {
            LambertProblem prob = (LambertProblem)obj;
            double UT1 = x[0] + prob.zeroUT;
            double UT2 = UT1 + x[1];
            Vector3d finalVelocity;

            try {
                f[0] = DeltaVToInterceptAtTime(prob.o, UT1, prob.target, UT2, out finalVelocity, 0, prob.shortway).magnitude;
                if (!prob.intercept_only) {
                    f[0] += finalVelocity.magnitude;
                }
            } catch (Exception) {
                // need Sqrt of MaxValue so least-squares can square it without an infinity
                f[0] = Math.Sqrt(Double.MaxValue);
            }
            if (!f[0].IsFinite())
                f[0] = Math.Sqrt(Double.MaxValue);
        }

        // Levenburg-Marquardt local optimization of a two burn transfer.  This is used to refine the results of the simulated annealing global
        // optimization search.
        //
        // NOTE TO SELF: all UT times here are non-zero centered.
        public static NodeParameters BiImpulsiveTransfer(Orbit o, Orbit target, double UT, double TT, double minUT = Double.NegativeInfinity, double maxUT = Double.PositiveInfinity, double maxTT = Double.PositiveInfinity, double maxUTplusT = Double.PositiveInfinity, bool intercept_only = false, double eps = 1e-9, int maxIter = 10000, bool shortway = false) {
            double[] x = { 0, TT };
            double[] scale = { o.period / 2, TT };

            // absolute final time constraint: x[0] + x[1] <= maxUTplusT
            double[,] C = { { 1, 1, maxUTplusT } };
            int[] CT = { -1 };

            double[] bndl = { minUT - UT, 0 };
            double[] bndu = { maxUT - UT, maxTT };

            alglib.minlmstate state;
            alglib.minlmreport rep = new alglib.minlmreport();
            alglib.minlmcreatev(1, x, 0.000001, out state);
            alglib.minlmsetscale(state, scale);
            alglib.minlmsetbc(state, bndl, bndu);
            if (maxUTplusT != Double.PositiveInfinity)
                alglib.minlmsetlc(state, C, CT);
            alglib.minlmsetcond(state, eps, maxIter);

            LambertProblem prob = new LambertProblem();
            prob.o = o;
            prob.target = target;
            prob.shortway = shortway;
            prob.zeroUT = UT;
            prob.intercept_only = intercept_only;

            // solve it the long way
            alglib.minlmoptimize(state, LambertCost, null, prob);
            alglib.minlmresultsbuf(state, ref x, rep);
            if (rep.terminationtype != 2)
                Logging.Debug("MechJeb Lambert Transfer minlmoptimize termination code: " + rep.terminationtype);

            Logging.Debug("DeltaVAndTimeForBiImpulsiveTransfer: x[0] = " + x[0] + " x[1] = " + x[1]);

            double burnUT = UT + x[0];
            Vector3d dV = DeltaVToInterceptAtTime(o, burnUT, target, burnUT + x[1], 0, shortway);

            return o.DeltaVToNode(burnUT, dV);
        }

        public static double acceptanceProbabilityForBiImpulsive(double currentCost, double newCost, double temp) {
            if (newCost < currentCost)
                return 1.0;
            return Math.Exp((currentCost - newCost) / temp);
        }

        // Simulated Annealing global search for a two burn transfer.
        //
        // FIXME: there's some very confusing nomenclature between DeltaVAndTimeForBiImpulsiveTransfer and this
        //        the minUT/maxUT values here are zero-centered on this methods UT.  the minUT/maxUT parameters to
        //        the other method are proper UT times and not zero centered at all.
        public static NodeParameters BiImpulsiveAnnealed(Orbit o, Orbit target, double UT, double minUT = 0.0, double maxUT = Double.PositiveInfinity, bool intercept_only = false, bool fixed_ut = false) {
            double MAXTEMP = 10000;
            double temp = MAXTEMP;
            double coolingRate = 0.003;
            double[] f = new double[1];
            double[] x = new double[2];
            double bestUT = 0;
            double bestTT = 0;
            double bestCost = Double.MaxValue;
            double currentCost = Double.MaxValue;
            bool bestshortway = false;
            System.Random random = new System.Random();

            LambertProblem prob = new LambertProblem();
            prob.o = o;
            prob.target = target;
            prob.zeroUT = UT;
            prob.intercept_only = intercept_only;

            double maxUTplusT = Double.PositiveInfinity;

            // min transfer time must be > 0 (no teleportation)
            double minTT = 1e-15;

            if (maxUT == Double.PositiveInfinity)
                maxUT = 1.5 * o.SynodicPeriod(target);

            // figure the max transfer time of a Hohmann orbit using the SMAs of the two orbits instead of the radius (as a guess), multiplied by 2
            double a = (Math.Abs(o.semiMajorAxis) + Math.Abs(target.semiMajorAxis)) / 2;
            double maxTT = Math.PI * Math.Sqrt(a * a * a / o.referenceBody.gravParameter);   // FIXME: allow tweaking

            Debug.Log("[MechJeb] DeltaVAndTimeForBiImpulsiveAnnealed Check1: minUT = " + minUT + " maxUT = " + maxUT + " maxTT = " + maxTT + " maxUTplusT = " + maxUTplusT);

            if (target.patchEndTransition != Orbit.PatchTransitionType.FINAL && target.patchEndTransition != Orbit.PatchTransitionType.INITIAL) {
                Debug.Log("[MechJeb] DeltaVAndTimeForBiImpulsiveAnnealed target.patchEndTransition = " + target.patchEndTransition);
                // reset the guess to search for start times out to the end of the target orbit
                maxUT = target.EndUT - UT;
                // longest possible transfer time would leave now and arrive at the target patch end
                maxTT = Math.Min(maxTT, target.EndUT - UT);
                // constraint on UT + TT <= maxUTplusT to arrive before the target orbit ends
                maxUTplusT = Math.Min(maxUTplusT, target.EndUT - UT);
            }

            // if our orbit ends, search for start times all the way to the end, but don't violate maxUTplusT if its set
            if (o.patchEndTransition != Orbit.PatchTransitionType.FINAL && o.patchEndTransition != Orbit.PatchTransitionType.INITIAL) {
                Debug.Log("[MechJeb] DeltaVAndTimeForBiImpulsiveAnnealed o.patchEndTransition = " + o.patchEndTransition);
                maxUT = Math.Min(o.EndUT - UT, maxUTplusT);
            }

            // user requested a burn at a specific time
            if (fixed_ut) {
                maxUT = 0;
                minUT = 0;
            }

            Debug.Log("[MechJeb] DeltaVAndTimeForBiImpulsiveAnnealed Check2: minUT = " + minUT + " maxUT = " + maxUT + " maxTT = " + maxTT + " maxUTplusT = " + maxUTplusT);

            double currentUT = maxUT / 2;
            double currentTT = maxTT / 2;

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

            int n = 0;

            stopwatch.Start();
            while (temp > 1) {
                // shrink the neighborhood based on temp
                double windowUT = temp / MAXTEMP * maxUT / 4;
                double windowTT = temp / MAXTEMP * maxTT / 4;

                // compute the neighbor
                x[0] = Functions.Clamp(random.NextGaussian(currentUT, windowUT), minUT, maxUT);
                x[1] = Functions.Clamp(random.NextGaussian(currentTT, windowTT), minTT, maxTT);
                x[1] = Math.Min(x[1], maxUTplusT - x[0]);

                // just randomize the shortway
                prob.shortway = random.NextDouble() > 0.5;

                LambertCost(x, f, prob);

                if (f[0] < bestCost) {
                    bestUT = x[0];
                    bestTT = x[1];
                    bestshortway = prob.shortway;
                    bestCost = f[0];
                    currentUT = bestUT;
                    currentTT = bestTT;
                    currentCost = bestCost;
                } else if (acceptanceProbabilityForBiImpulsive(currentCost, f[0], temp) > random.NextDouble()) {
                    currentUT = x[0];
                    currentTT = x[1];
                    currentCost = f[0];
                }

                temp *= 1 - coolingRate;

                n++;
            }
            stopwatch.Stop();

            Logging.Debug("DeltaVAndTimeForBiImpulsiveAnnealed N = " + n + " time = " + stopwatch.Elapsed);

            double burnUT = UT + bestUT;

            Logging.Debug("Annealing results burnUT = " + burnUT + " zero'd burnUT = " + bestUT + " TT = " + bestTT + " Cost = " + bestCost);

            //return DeltaVToInterceptAtTime(o, UT + bestUT, target, UT + bestUT + bestTT, shortway: bestshortway);

            // FIXME: srsly this minUT = UT + minUT / maxUT = UT + maxUT / maxUTplusT = maxUTplusT - bestUT rosetta stone here needs to go
            // and some consistency needs to happen.  this is just breeding bugs.
            return BiImpulsiveTransfer(o, target, UT + bestUT, bestTT, minUT: UT + minUT, maxUT: UT + maxUT, maxTT: maxTT, maxUTplusT: maxUTplusT - bestUT, intercept_only: intercept_only, shortway: bestshortway);
        }

        //Like DeltaVAndTimeForHohmannTransfer, but adds an additional step that uses the Lambert
        //solver to adjust the initial burn to produce an exact intercept instead of an approximate
        public static NodeParameters HohmannLambertTransfer(Orbit o, Orbit target, double UT, double subtractProgradeDV = 0) {
            NodeParameters hohmannParams = HohmannTransfer(o, target, UT);
            Vector3d subtractedProgradeDV = subtractProgradeDV * hohmannParams.deltaV.normalized;

            Orbit hohmannOrbit = o.PerturbedOrbit(hohmannParams);
            double apsisTime; //approximate target  intercept time
            if (hohmannOrbit.semiMajorAxis > o.semiMajorAxis) apsisTime = hohmannOrbit.NextApoapsisTime(hohmannParams.time);
            else apsisTime = hohmannOrbit.NextPeriapsisTime(hohmannParams.time);

            Debug.Log("hohmann = " + hohmannParams + ", apsisTime = " + apsisTime);

            Vector3d dV = Vector3d.zero;
            double minCost = 999999;

            double minInterceptTime = apsisTime - hohmannOrbit.period / 4;
            double maxInterceptTime = apsisTime + hohmannOrbit.period / 4;
            const int subdivisions = 30;
            for (int i = 0; i < subdivisions; i++) {
                double interceptUT = minInterceptTime + i * (maxInterceptTime - minInterceptTime) / subdivisions;

                Debug.Log("i + " + i + ", trying for intercept at UT = " + interceptUT);

                //Try both short and long way
                Vector3d interceptBurn = DeltaVToInterceptAtTime(o, hohmannParams.time, target, interceptUT, 0, true);
                double cost = (interceptBurn - subtractedProgradeDV).magnitude;
                Debug.Log("short way dV = " + interceptBurn.magnitude + "; subtracted cost = " + cost);
                if (cost < minCost) {
                    dV = interceptBurn;
                    minCost = cost;
                }

                interceptBurn = DeltaVToInterceptAtTime(o, hohmannParams.time, target, interceptUT, 0, false);
                cost = (interceptBurn - subtractedProgradeDV).magnitude;
                Debug.Log("long way dV = " + interceptBurn.magnitude + "; subtracted cost = " + cost);
                if (cost < minCost) {
                    dV = interceptBurn;
                    minCost = cost;
                }
            }

            return o.DeltaVToNode(hohmannParams.time, dV);
        }
    }
}
