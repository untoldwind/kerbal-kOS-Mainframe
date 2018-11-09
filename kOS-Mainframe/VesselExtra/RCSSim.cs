using System;
using System.Collections.Generic;
using UnityEngine;
using Smooth.Pools;

namespace kOSMainframe.VesselExtra
{
    public class RCSSim
    {
        private static readonly Pool<RCSSim> pool = new Pool<RCSSim>(Create, Reset);

        public readonly ResourceContainer resourceConsumptions = new ResourceContainer();
        public readonly ResourceContainer resourceFlowModes = new ResourceContainer();

        public double actualThrust = 0;
        public bool isActive = false;
        public double isp = 0;
        public PartSim partSim;
        public List<AppliedForce> appliedForces = new List<AppliedForce>();
        public float maxMach;
        public bool isFlamedOut;
        public bool dontDecoupleActive = true;

        public double thrust = 0;

        // Add thrust vector to account for directional losses
        public Vector3 thrustVec;

        private static RCSSim Create()
        {
            return new RCSSim();
        }

        private static void Reset(RCSSim engineSim)
        {
            engineSim.resourceConsumptions.Reset();
            engineSim.resourceFlowModes.Reset();
            engineSim.partSim = null;
            engineSim.actualThrust = 0;
            engineSim.isActive = false;
            engineSim.isp = 0;
            for (int i = 0; i < engineSim.appliedForces.Count; i++)
            {
                engineSim.appliedForces[i].Release();
            }
            engineSim.appliedForces.Clear();
            engineSim.thrust = 0;
            engineSim.maxMach = 0f;
            engineSim.isFlamedOut = false;
        }

        public void Release()
        {
            pool.Release(this);
        }

        public static RCSSim New(PartSim theEngine,
                            ModuleRCS engineMod,
                            double atmosphere,
                            float machNumber,
                            bool vectoredThrust,
                            bool fullThrust,
                            bool debug)
        {
            double maxFuelFlow = engineMod.maxFuelFlow;
            // double minFuelFlow = engineMod.minFuelFlow;
            float thrustPercentage = engineMod.thrustPercentage;
            List<Transform> thrustTransforms = engineMod.thrusterTransforms;
            //   List<float> thrustTransformMultipliers = engineMod.th
            Vector3 vecThrust = CalculateThrustVector(vectoredThrust ? thrustTransforms : null, debug);
            FloatCurve atmosphereCurve = engineMod.atmosphereCurve;
            //   bool atmChangeFlow = engineMod.at
            //   FloatCurve atmCurve = engineMod.useAtmCurve ? engineMod.atmCurve : null;
            //   FloatCurve velCurve = engineMod.useVelCurve ? engineMod.velCurve : null;
            //    float currentThrottle = engineMod.currentThrottle;
            double IspG = engineMod.G;
            //    bool throttleLocked = engineMod.throttleLocked || fullThrust;
            List<Propellant> propellants = engineMod.propellants;
            bool active = engineMod.moduleIsEnabled;
            //    float resultingThrust = engineMod.resultingThrust;
            bool isFlamedOut = engineMod.flameout;

            RCSSim engineSim = pool.Borrow();

            engineSim.isp = 0.0;
            engineSim.maxMach = 0.0f;
            engineSim.actualThrust = 0.0;
            engineSim.partSim = theEngine;
            engineSim.isActive = active;
            engineSim.thrustVec = vecThrust;
            engineSim.isFlamedOut = isFlamedOut;
            engineSim.resourceConsumptions.Reset();
            engineSim.resourceFlowModes.Reset();
            engineSim.appliedForces.Clear();

            double flowRate = 0.0;
            if (engineSim.partSim.hasVessel)
            {
                if (debug) Debug.Log("hasVessel is true");

                engineSim.isp = atmosphereCurve.Evaluate((float)atmosphere);
                engineSim.thrust = GetThrust(maxFuelFlow, engineSim.isp);
                engineSim.actualThrust = engineSim.isActive ? engineSim.thrust : 0.0;

                if (debug)
                {
                    Debug.Log("isp     = " + engineSim.isp);
                    Debug.Log("thrust  = " + engineSim.thrust);
                    Debug.Log("actual  = " + engineSim.actualThrust);
                }

                if (debug) Debug.Log("throttleLocked is true, using thrust for flowRate");
                flowRate = GetFlowRate(engineSim.thrust, engineSim.isp);
            }
            else
            {
                if (debug) Debug.Log("hasVessel is false");
                engineSim.isp = atmosphereCurve.Evaluate((float)atmosphere);
                engineSim.thrust = GetThrust(maxFuelFlow, engineSim.isp);
                engineSim.actualThrust = 0d;
                if (debug)
                {
                    Debug.Log("isp     = " + engineSim.isp);
                    Debug.Log("thrust  = " + engineSim.thrust);
                    Debug.Log("actual  = " + engineSim.actualThrust);
                    Debug.Log("no vessel, using thrust for flowRate");
                }

                flowRate = GetFlowRate(engineSim.thrust, engineSim.isp);
            }

            if (debug) Debug.Log("flowRate = " + flowRate);

            float flowMass = 0f;
            for (int i = 0; i < propellants.Count; ++i)
            {
                Propellant propellant = propellants[i];
                if (!propellant.ignoreForIsp)
                    flowMass += propellant.ratio * ResourceContainer.GetResourceDensity(propellant.id);
            }

            if (debug) Debug.Log("flowMass = " + flowMass);

            for (int i = 0; i < propellants.Count; ++i)
            {
                Propellant propellant = propellants[i];

                if (propellant.ignoreForIsp || propellant.name == "ElectricCharge" || propellant.name == "IntakeAir")
                {
                    continue;
                }

                double consumptionRate = propellant.ratio * flowRate / flowMass;
                if (debug) Debug.Log(
                        "Add consumption({0}, {1}:{2:d}) = {3:g6}\n" + "," +
                        ResourceContainer.GetResourceName(propellant.id) + "," +
                        theEngine.name + "," +
                        theEngine.partId + "," +
                        consumptionRate);
                engineSim.resourceConsumptions.Add(propellant.id, consumptionRate);
                engineSim.resourceFlowModes.Add(propellant.id, (double)propellant.GetFlowMode());
            }

            for (int i = 0; i < thrustTransforms.Count; i++)
            {
                Transform thrustTransform = thrustTransforms[i];
                Vector3d direction = thrustTransform.forward.normalized;
                Vector3d position = thrustTransform.position;

                AppliedForce appliedForce = AppliedForce.New(direction * engineSim.thrust, position);
                engineSim.appliedForces.Add(appliedForce);
            }

            return engineSim;
        }

        private static Vector3 CalculateThrustVector(List<Transform> thrustTransforms, bool debug)
        {
            if (thrustTransforms == null)
            {
                return Vector3.forward;
            }

            Vector3 thrustvec = Vector3.zero;
            for (int i = 0; i < thrustTransforms.Count; ++i)
            {
                Transform trans = thrustTransforms[i];

                if (debug) Debug.Log("Transform = " + trans.forward.x + "," + trans.forward.y + "," + trans.forward.z + "," + trans.forward.magnitude);

                thrustvec -= (trans.forward);
            }

            if (debug) Debug.Log("ThrustVec  = " + thrustvec.x + "," + thrustvec.y + "," + thrustvec.z + "," + thrustvec.magnitude);

            thrustvec.Normalize();

            if (debug) Debug.Log("ThrustVecN = " + thrustvec.x + "," + thrustvec.y + "," + thrustvec.z + "," + thrustvec.magnitude);

            return thrustvec;
        }

        public static double GetExhaustVelocity(double isp)
        {
            return isp * Units.GRAVITY;
        }

        public static float GetFlowModifier(bool atmChangeFlow, FloatCurve atmCurve, double atmDensity, FloatCurve velCurve, float machNumber, ref float maxMach)
        {
            float flowModifier = 1.0f;
            if (atmChangeFlow)
            {
                flowModifier = (float)(atmDensity / 1.225);
                if (atmCurve != null)
                {
                    flowModifier = atmCurve.Evaluate(flowModifier);
                }
            }
            if (velCurve != null)
            {
                flowModifier = flowModifier * velCurve.Evaluate(machNumber);
                maxMach = velCurve.maxTime;
            }
            if (flowModifier < float.Epsilon)
            {
                flowModifier = float.Epsilon;
            }
            return flowModifier;
        }

        public static double GetFlowRate(double thrust, double isp)
        {
            return thrust / GetExhaustVelocity(isp);
        }

        public static float GetThrottlePercent(float currentThrottle, float thrustPercentage)
        {
            return currentThrottle * GetThrustPercent(thrustPercentage);
        }

        public static double GetThrust(double flowRate, double isp)
        {
            return flowRate * GetExhaustVelocity(isp);
        }

        public static float GetThrustPercent(float thrustPercentage)
        {
            return thrustPercentage * 0.01f;
        }

        public void DumpEngineToLog()
        {
            Debug.Log("[thrust = " + thrust + ", actual = " + actualThrust + ", isp = " + isp);
        }
    }
}
