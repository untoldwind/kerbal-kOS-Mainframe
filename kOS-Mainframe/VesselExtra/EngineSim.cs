using System;
using System.Collections.Generic;
using UnityEngine;

namespace kOSMainframe.VesselExtra
{
    public class EngineSim
    {
        private static readonly Pool<EngineSim> pool = new Pool<EngineSim>(Create, Reset);

        private readonly ResourceContainer resourceConsumptions = new ResourceContainer();
        private readonly ResourceContainer resourceFlowModes = new ResourceContainer();

        public double actualThrust = 0;
        public bool isActive = false;
        public double isp = 0;
        public PartSim partSim;
        public List<AppliedForce> appliedForces = new List<AppliedForce>();
        public float maxMach;
        public bool isFlamedOut;
        public bool dontDecoupleActive = true;

        // A dictionary to hold a set of parts for each resource
        Dictionary<int, HashSet<PartSim>> sourcePartSets = new Dictionary<int, HashSet<PartSim>>();
        Dictionary<int, HashSet<PartSim>> stagePartSets = new Dictionary<int, HashSet<PartSim>>();

        HashSet<PartSim> visited = new HashSet<PartSim>();

        public double thrust = 0;

        // Add thrust vector to account for directional losses
        public Vector3 thrustVec;

        private static EngineSim Create()
        {
            return new EngineSim();
        }

        private static void Reset(EngineSim engineSim)
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

        public static EngineSim New(PartSim theEngine,
                                    ModuleEngines engineMod,
                                    double atmosphere,
                                    float machNumber,
                                    bool vectoredThrust,
                                    bool fullThrust,
                                    bool debug)
        {
            float maxFuelFlow = engineMod.maxFuelFlow;
            float minFuelFlow = engineMod.minFuelFlow;
            float thrustPercentage = engineMod.thrustPercentage;
            List<Transform> thrustTransforms = engineMod.thrustTransforms;
            List<float> thrustTransformMultipliers = engineMod.thrustTransformMultipliers;
            Vector3 vecThrust = CalculateThrustVector(vectoredThrust ? thrustTransforms : null,
                                                        vectoredThrust ? thrustTransformMultipliers : null,
                                                        debug);
            FloatCurve atmosphereCurve = engineMod.atmosphereCurve;
            bool atmChangeFlow = engineMod.atmChangeFlow;
            FloatCurve atmCurve = engineMod.useAtmCurve ? engineMod.atmCurve : null;
            FloatCurve velCurve = engineMod.useVelCurve ? engineMod.velCurve : null;
            FloatCurve thrustCurve = engineMod.useThrustCurve ? engineMod.thrustCurve : null;
            float currentThrottle = engineMod.currentThrottle;
            float IspG = engineMod.g;
            bool throttleLocked = engineMod.throttleLocked || fullThrust;
            List<Propellant> propellants = engineMod.propellants;
            float thrustCurveRatio = engineMod.thrustCurveRatio;
            bool active = engineMod.isOperational;

            //I do not know if this matters. RF and stock always have finalThrust. But stock uses resultingThrust in the mass flow calculations, so keep it.
            float resultingThrust = engineMod.resultingThrust;

            bool isFlamedOut = engineMod.flameout;

            EngineSim engineSim = pool.Borrow();

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

                foreach (Propellant p in propellants)
                {
                    if (p.ignoreForThrustCurve) continue;
                    double ratio = p.totalResourceAvailable / p.totalResourceCapacity;
                    if (ratio < thrustCurveRatio)
                        thrustCurveRatio = (float)ratio;
                }

                float flowModifier = GetFlowModifier(atmChangeFlow, atmCurve, engineSim.partSim.part.atmDensity, velCurve, machNumber, thrustCurve, thrustCurveRatio, ref engineSim.maxMach);
                engineSim.isp = atmosphereCurve.Evaluate((float)atmosphere);
                engineSim.thrust = GetThrust(Mathf.Lerp(minFuelFlow, maxFuelFlow, GetThrustPercent(thrustPercentage)) * flowModifier, engineSim.isp);
                engineSim.actualThrust = engineSim.isActive ? resultingThrust : 0.0;
                if (debug)
                {
                    Debug.Log("flowMod = " + flowModifier);
                    Debug.Log("isp     = " + engineSim.isp);
                    Debug.Log("thrust  = " + engineSim.thrust);
                    Debug.Log("actual  = " + engineSim.actualThrust);
                    Debug.Log("final  = " + engineMod.finalThrust);
                    Debug.Log("resulting  = " + engineMod.resultingThrust);
                }

                if (throttleLocked)
                {
                    if (debug) Debug.Log("throttleLocked is true, using thrust for flowRate");
                    flowRate = GetFlowRate(engineSim.thrust, engineSim.isp);
                }
                else
                {
                    if (currentThrottle > 0.0f && engineSim.partSim.isLanded == false)
                    {
                        if (debug) Debug.Log("throttled up and not landed, using actualThrust for flowRate");
                        flowRate = GetFlowRate(engineSim.actualThrust, engineSim.isp);
                    }
                    else
                    {
                        if (debug) Debug.Log("throttled down or landed, using thrust for flowRate");
                        flowRate = GetFlowRate(engineSim.thrust, engineSim.isp);
                    }
                }
            }
            else
            {
                if (debug) Debug.Log("hasVessel is false");
                float flowModifier = GetFlowModifier(atmChangeFlow, atmCurve, CelestialBodies.SelectedBody.GetDensity(0), velCurve, machNumber, thrustCurve, thrustCurveRatio, ref engineSim.maxMach);
                engineSim.isp = atmosphereCurve.Evaluate((float)atmosphere);
                engineSim.thrust = GetThrust(Mathf.Lerp(minFuelFlow, maxFuelFlow, GetThrustPercent(thrustPercentage)) * flowModifier, engineSim.isp);
                engineSim.actualThrust = 0d;
                if (debug)
                {
                    Debug.Log("flowMod = " + flowModifier);
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
                        "Add consumption " +
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

                AppliedForce appliedForce = AppliedForce.New(direction * engineSim.thrust * thrustTransformMultipliers[i], position);
                engineSim.appliedForces.Add(appliedForce);
            }

            return engineSim;
        }

        private static Vector3 CalculateThrustVector(List<Transform> thrustTransforms, List<float> thrustTransformMultipliers, bool debug)
        {
            if (thrustTransforms == null)
            {
                return Vector3.forward;
            }

            Vector3 thrustvec = Vector3.zero;
            for (int i = 0; i < thrustTransforms.Count; ++i)
            {
                Transform trans = thrustTransforms[i];

                if (debug) Debug.Log("Transform = " + trans.forward.x + "," +  trans.forward.y + "," + trans.forward.z + "," + trans.forward.magnitude);

                thrustvec -= (trans.forward * thrustTransformMultipliers[i]);
            }

            if (debug) Debug.Log("ThrustVec  = " + thrustvec.x + "," + thrustvec.y + "," + thrustvec.z + "," + thrustvec.magnitude);

            thrustvec.Normalize();

            if (debug) Debug.Log("ThrustVecN = " + thrustvec.x + "," + thrustvec.y + "," + thrustvec.z + "," + thrustvec.magnitude);

            return thrustvec;
        }

        public static float GetFlowModifier(bool atmChangeFlow, FloatCurve atmCurve, double atmDensity, FloatCurve velCurve, float machNumber, FloatCurve thrustCurve, float thrustCurveRatio, ref float maxMach)
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
            if (thrustCurve != null)
            {
                flowModifier = flowModifier * thrustCurve.Evaluate(thrustCurveRatio);
            }
            if (flowModifier < float.Epsilon)
            {
                flowModifier = float.Epsilon;
            }
            return flowModifier;
        }

        public ResourceContainer ResourceConsumptions
        {
            get
            {
                return resourceConsumptions;
            }
        }

        public static double GetExhaustVelocity(double isp)
        {
            return isp * Units.GRAVITY;
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

        public bool SetResourceDrains(List<PartSim> allParts, List<PartSim> allFuelLines, HashSet<PartSim> drainingParts, bool debug)
        {
            //DumpSourcePartSets(log, "before clear");
            foreach (HashSet<PartSim> sourcePartSet in sourcePartSets.Values)
            {
                sourcePartSet.Clear();
            }
            //DumpSourcePartSets(log, "after clear");

            for (int index = 0; index < this.resourceConsumptions.Types.Count; index++)
            {
                int type = this.resourceConsumptions.Types[index];

                HashSet<PartSim> sourcePartSet;
                if (!sourcePartSets.TryGetValue(type, out sourcePartSet))
                {
                    sourcePartSet = new HashSet<PartSim>();
                    sourcePartSets.Add(type, sourcePartSet);
                }

                switch ((ResourceFlowMode)this.resourceFlowModes[type])
                {
                    case ResourceFlowMode.NO_FLOW:
                        if (partSim.resources[type] > SimManager.RESOURCE_MIN && partSim.resourceFlowStates[type] != 0)
                        {
                            sourcePartSet.Add(partSim);
                        }
                        break;

                    case ResourceFlowMode.ALL_VESSEL:
                    case ResourceFlowMode.ALL_VESSEL_BALANCE:
                        for (int i = 0; i < allParts.Count; i++)
                        {
                            PartSim aPartSim = allParts[i];
                            if (aPartSim.resources[type] > SimManager.RESOURCE_MIN && aPartSim.resourceFlowStates[type] != 0)
                            {
                                sourcePartSet.Add(aPartSim);
                            }
                        }
                        break;

                    case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                    case ResourceFlowMode.STAGE_PRIORITY_FLOW_BALANCE:

                        if (debug) Debug.Log("Find " + ResourceContainer.GetResourceName(type) + " sources for " + partSim.name + ":" + partSim.partId);
                        foreach (HashSet<PartSim> stagePartSet in stagePartSets.Values)
                        {
                            stagePartSet.Clear();
                        }
                        var maxStage = -1;

                        for (int i = 0; i < allParts.Count; i++)
                        {
                            var aPartSim = allParts[i];
                            //if (log != null) log.Append(aPartSim.name, ":" + aPartSim.partId, " contains ", aPartSim.resources[type])
                            //                  .AppendLine((aPartSim.resourceFlowStates[type] == 0) ? " (disabled)" : "");
                            if (aPartSim.resources[type] <= SimManager.RESOURCE_MIN || aPartSim.resourceFlowStates[type] == 0)
                            {
                                continue;
                            }

                            int stage = aPartSim.inverseStage;
                            if (stage > maxStage)
                            {
                                maxStage = stage;
                            }

                            HashSet<PartSim> tempPartSet;
                            if (!stagePartSets.TryGetValue(stage, out tempPartSet))
                            {
                                tempPartSet = new HashSet<PartSim>();
                                stagePartSets.Add(stage, tempPartSet);
                            }
                            tempPartSet.Add(aPartSim);
                        }

                        for (int j = maxStage; j >= -1; j--)
                        {
                            //if (log != null) log.AppendLine("Testing stage ", j);
                            HashSet<PartSim> stagePartSet;
                            if (stagePartSets.TryGetValue(j, out stagePartSet) && stagePartSet.Count > 0)
                            {
                                //if (log != null) log.AppendLine("Not empty");
                                // We have to copy the contents of the set here rather than copying the set reference or 
                                // bad things (tm) happen
                                foreach (PartSim aPartSim in stagePartSet)
                                {
                                    sourcePartSet.Add(aPartSim);
                                }
                                break;
                            }
                        }
                        break;

                    case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                    case ResourceFlowMode.STAGE_STACK_FLOW:
                    case ResourceFlowMode.STAGE_STACK_FLOW_BALANCE:
                        visited.Clear();

                        if (debug) Debug.Log("Find " + ResourceContainer.GetResourceName(type) + " sources for " + partSim.name + ":" + partSim.partId);

                        partSim.GetSourceSet(type, allParts, visited, sourcePartSet, debug, "");
                        break;

                    default:
                        if (debug) Debug.Log("SetResourceDrains(" + partSim.name + ":" + partSim.partId + ") Unexpected flow type for " + ResourceContainer.GetResourceName(type) + ")");
                        break;
                }

                if (debug && sourcePartSet.Count > 0)
                {
                    Debug.Log("Source parts for " + ResourceContainer.GetResourceName(type) + ":");
                    foreach (PartSim partSim in sourcePartSet)
                    {
                        Debug.Log(partSim.name + ":" + partSim.partId);
                    }
                }

                //DumpSourcePartSets(log, "after " + ResourceContainer.GetResourceName(type));
            }

            // If we don't have sources for all the needed resources then return false without setting up any drains
            for (int i = 0; i < this.resourceConsumptions.Types.Count; i++)
            {
                int type = this.resourceConsumptions.Types[i];
                HashSet<PartSim> sourcePartSet;
                if (!sourcePartSets.TryGetValue(type, out sourcePartSet) || sourcePartSet.Count == 0)
                {
                    if (debug) Debug.Log("No source of " + ResourceContainer.GetResourceName(type));
                    isActive = false;
                    return false;
                }
            }

            // Now we set the drains on the members of the sets and update the draining parts set
            for (int i = 0; i < this.resourceConsumptions.Types.Count; i++)
            {
                int type = this.resourceConsumptions.Types[i];
                HashSet<PartSim> sourcePartSet = sourcePartSets[type];
                ResourceFlowMode mode = (ResourceFlowMode)resourceFlowModes[type];
                double consumption = resourceConsumptions[type];
                double amount = 0d;
                double total = 0d;
                if (mode == ResourceFlowMode.ALL_VESSEL_BALANCE ||
                    mode == ResourceFlowMode.STAGE_PRIORITY_FLOW_BALANCE ||
                    mode == ResourceFlowMode.STAGE_STACK_FLOW_BALANCE ||
                    mode == ResourceFlowMode.STACK_PRIORITY_SEARCH)
                {
                    foreach (PartSim partSim in sourcePartSet)
                        total += partSim.resources[type];
                }
                else
                    amount = consumption / sourcePartSet.Count;

                // Loop through the members of the set 
                foreach (PartSim partSim in sourcePartSet)
                {
                    if (total != 0d)
                        amount = consumption * partSim.resources[type] / total;

                    if (debug) Debug.Log("Adding drain of " + amount + " " + ResourceContainer.GetResourceName(type) + " to " + partSim.name + ":" + partSim.partId);

                    partSim.resourceDrains.Add(type, amount);
                    drainingParts.Add(partSim);
                }
            }
            return true;
        }

        public void DumpEngineToLog()
        {
            Debug.Log("[thrust = " + thrust + ", actual = " + actualThrust + ", isp = " + isp);
        }
    }
}
