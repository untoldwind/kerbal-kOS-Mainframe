using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;

namespace kOSMainframe.VesselExtra {
    public class Simulation {
        private const double SECONDS_PER_DAY = 86400;
        private List<EngineSim> activeEngines;
        private List<EngineSim> allEngines;
        private List<RCSSim> allRCS;
        private List<PartSim> allFuelLines;
        private List<PartSim> allParts;
        private double atmosphere;
        private int currentStage;
        private double currentisp;
        private HashSet<PartSim> decoupledParts;
        private bool doingCurrent;
        private List<PartSim> dontStageParts;
        private List<List<PartSim>> dontStagePartsLists;
        private HashSet<PartSim> drainingParts;
        private HashSet<int> drainingResources;
        private double gravity;
        // A dictionary for fast lookup of Part->PartSim during the preparation phase
        private Dictionary<Part, PartSim> partSimLookup;
        private bool debug;

        private int lastStage;
        private List<Part> partList;
        private double simpleTotalThrust;
        private double stageStartMass;
        private Vector3d stageStartCom;
        private double stageTime;
        private double stepEndMass;
        private double stepStartMass;
        private double totalStageActualThrust;
        private double totalStageFlowRate;
        private double totalStageIspFlowRate;
        private double totalStageThrust;
        private ForceAccumulator totalStageThrustForce;
        private Vector3 vecActualThrust;
        private Vector3 vecStageDeltaV;
        private Vector3 vecThrust;
        private double mach;
        private float maxMach;
        public String vesselName;
        public VesselType vesselType;
        private WeightedVectorAverager vectorAverager;

        private double RCSIsp;
        private double RCSThrust;
        private double RCSDeltaV;
        private double RCSTWR;
        private double RCSBurnTime;

        public Simulation() {
            activeEngines = new List<EngineSim>();
            allEngines = new List<EngineSim>();
            allRCS = new List<RCSSim>();
            allFuelLines = new List<PartSim>();
            allParts = new List<PartSim>();
            decoupledParts = new HashSet<PartSim>();
            dontStagePartsLists = new List<List<PartSim>>();
            drainingParts = new HashSet<PartSim>();
            drainingResources = new HashSet<int>();
            partSimLookup = new Dictionary<Part, PartSim>();
            partList = new List<Part>();
            totalStageThrustForce = new ForceAccumulator();
            vectorAverager = new WeightedVectorAverager();
        }

        public bool PrepareSimulation(List<Part> parts, double theGravity, double theAtmosphere = 0, double theMach = 0, bool dumpTree = false, bool vectoredThrust = false, bool fullThrust = false, bool _debug = false) {
            debug = _debug;
            partList = parts;
            gravity = theGravity;
            atmosphere = theAtmosphere;
            mach = theMach;
            lastStage = StageManager.LastStage;
            maxMach = 1.0f;
            if (debug) Debug.Log("lastStage = " + lastStage);

            // Clear the lists for our simulation parts
            allParts.Clear();
            allFuelLines.Clear();
            drainingParts.Clear();
            allEngines.Clear();
            activeEngines.Clear();
            drainingResources.Clear();

            // A dictionary for fast lookup of Part->PartSim during the preparation phase
            partSimLookup.Clear();

            if (partList.Count > 0 && partList[0].vessel != null) {
                vesselName = partList[0].vessel.vesselName;
                vesselType = partList[0].vessel.vesselType;
            }
            // First we create a PartSim for each Part (giving each a unique id)
            int partId = 1;
            for (int i = 0; i < partList.Count; ++i) {
                Part part = partList[i];

                // If the part is already in the lookup dictionary then log it and skip to the next part
                if (partSimLookup.ContainsKey(part)) {
                    if (debug) Debug.Log("Part " + part.name + " appears in vessel list more than once");
                    continue;
                }

                // Create the PartSim
                PartSim partSim = PartSim.New(part, partId, debug);

                // Add it to the Part lookup dictionary and the necessary lists
                partSimLookup.Add(part, partSim);
                allParts.Add(partSim);

                if (partSim.isFuelLine) {
                    allFuelLines.Add(partSim);
                }

                if (partSim.isEngine) {
                    partSim.CreateEngineSims(allEngines, atmosphere, mach, vectoredThrust, fullThrust, debug);
                }

                if (partSim.isRCS) {
                    partSim.CreateRCSSims(allRCS, atmosphere, mach, vectoredThrust, fullThrust, debug);
                }

                partId++;
            }

            for (int i = 0; i < allEngines.Count; ++i) {
                maxMach = Mathf.Max(maxMach, allEngines[i].maxMach);
            }

            UpdateActiveEngines();

            // Now that all the PartSims have been created we can do any set up that needs access to other parts
            // First we set up all the parent links
            for (int i = 0; i < allParts.Count; i++) {
                PartSim partSim = allParts[i];
                partSim.SetupParent(partSimLookup, debug);
            }

            if (debug) Debug.Log("SetupAttachNodes and count stages");
            for (int i = 0; i < allParts.Count; ++i) {
                PartSim partSim = allParts[i];

                partSim.SetupAttachNodes(partSimLookup, debug);
                if (partSim.decoupledInStage >= lastStage) {
                    lastStage = partSim.decoupledInStage + 1;
                }
            }

            // And finally release the Part references from all the PartSims
            if (debug) Debug.Log("ReleaseParts");
            for (int i = 0; i < allParts.Count; ++i) {
                allParts[i].ReleasePart();
            }

            // And dereference the core's part list
            partList = null;

            return true;
        }

        // This function runs the simulation and returns a newly created array of Stage objects
        public StageInfo[] RunSimulation() {
            if (debug) Debug.Log("RunSimulation started");

            // Start with the last stage to simulate
            // (this is in a member variable so it can be accessed by AllowedToStage and ActivateStage)
            currentStage = lastStage;
            // Work out which engines would be active if just doing the staging and if this is different to the
            // currently active engines then generate an extra stage
            // Loop through all the engines
            bool anyActive = false;
            for (int i = 0; i < allEngines.Count; ++i) {
                EngineSim engine = allEngines[i];

                if (debug) Debug.Log("Testing engine mod of " + engine.partSim.name + ":" + engine.partSim.partId);

                bool bActive = engine.isActive;
                bool bStage = (engine.partSim.inverseStage >= currentStage);
                if (debug) Debug.Log("bActive = " + bActive + "   bStage = " + bStage);
                if (HighLogic.LoadedSceneIsFlight) {
                    if (bActive) {
                        anyActive = true;
                    }
                    if (bActive != bStage) {
                        // If the active state is different to the state due to staging
                        if (debug) Debug.Log("Need to do current active engines first");
                        doingCurrent = true;
                    }
                } else {
                    if (bStage) {
                        if (debug) Debug.Log("Marking as active");
                        engine.isActive = true;
                    }
                }
            }

            // If we need to do current because of difference in engine activation and there actually are active engines
            // then we do the extra stage otherwise activate the next stage and don't treat it as current
            if (doingCurrent && anyActive) {
                currentStage++;
            } else {
                ActivateStage();
                doingCurrent = false;
            }

            // Create a list of lists of PartSims that prevent decoupling
            BuildDontStageLists();

            // Create the array of stages that will be returned
            StageInfo[] stages = new StageInfo[currentStage + 1];

            int startStage = currentStage;

            // Loop through the stages
            while (currentStage >= 0) {
                if (debug) {
                    Debug.Log("Simulating stage " + currentStage);
                }

                // Update active engines and resource drains
                UpdateResourceDrains();

                // Update the masses of the parts to correctly handle "no physics" parts
                stageStartMass = UpdatePartMasses();

                if (debug)
                    allParts[0].DumpPartToLog("", allParts);

                // Create the Stage object for this stage
                StageInfo stage = new StageInfo();

                stageTime = 0d;
                vecStageDeltaV = Vector3.zero;

                stageStartCom = ShipCom;

                stepStartMass = stageStartMass;
                stepEndMass = 0;

                CalculateThrustAndISP();


                // Store various things in the Stage object
                stage.thrust = totalStageThrust;
                stage.thrustToWeight = totalStageThrust / (stageStartMass * gravity);
                stage.maxThrustToWeight = stage.thrustToWeight;
                stage.actualThrust = totalStageActualThrust;
                stage.actualThrustToWeight = totalStageActualThrust / (stageStartMass * gravity);

                CalculateRCS(gravity, false);

                stage.RCSIsp = RCSIsp;
                stage.RCSThrust = RCSThrust;
                stage.RCSdeltaVStart = RCSDeltaV;
                stage.RCSTWRStart = RCSTWR;
                stage.RCSBurnTime = RCSBurnTime;

                if (debug) {
                    Debug.Log("stage.thrust = " + stage.thrust);
                    Debug.Log("StageMass = " + stageStartMass);
                    Debug.Log("Initial maxTWR = " + stage.maxThrustToWeight);
                }

                // calculate torque and associates
                stage.maxThrustTorque = totalStageThrustForce.TorqueAt(stageStartCom).magnitude;

                // torque divided by thrust. imagine that all engines are at the end of a lever that tries to turn the ship.
                // this numerical value, in meters, would represent the length of that lever.
                double torqueLeverArmLength = (stage.thrust <= 0) ? 0 : stage.maxThrustTorque / stage.thrust;

                // how far away are the engines from the CoM, actually?
                double thrustDistance = (stageStartCom - totalStageThrustForce.GetAverageForceApplicationPoint()).magnitude;

                // the combination of the above two values gives an approximation of the offset angle.
                double sinThrustOffsetAngle = 0;
                if (thrustDistance > 1e-7) {
                    sinThrustOffsetAngle = torqueLeverArmLength / thrustDistance;
                    if (sinThrustOffsetAngle > 1) {
                        sinThrustOffsetAngle = 1;
                    }
                }

                stage.thrustOffsetAngle = Math.Asin(sinThrustOffsetAngle) * 180 / Math.PI;

                // The total mass is simply the mass at the start of the stage
                stage.totalMass = stageStartMass;

                // If we have done a previous stage
                if (currentStage < startStage) {
                    // Calculate what the previous stage's mass and cost were by subtraction
                    StageInfo prev = stages[currentStage + 1];
                    prev.mass = prev.totalMass - stage.totalMass;
                }

                // The above code will never run for the last stage so set those directly
                if (currentStage == 0) {
                    stage.mass = stage.totalMass;
                }

                dontStageParts = dontStagePartsLists[currentStage];

                if (debug) {
                    if (dontStageParts.Count > 0) {
                        Debug.Log("Parts preventing staging:");
                        for (int i = 0; i < dontStageParts.Count; i++) {
                            PartSim partSim = dontStageParts[i];
                            partSim.DumpPartToLog("");
                        }
                    } else {
                        Debug.Log("No parts preventing staging");
                    }
                }


                // Now we will loop until we are allowed to stage
                int loopCounter = 0;
                while (!AllowedToStage()) {
                    loopCounter++;
                    //if (log != null) log.AppendLine("loop = ", loopCounter);
                    // Calculate how long each draining tank will take to drain and run for the minimum time
                    double resourceDrainTime = double.MaxValue;
                    PartSim partMinDrain = null;

                    foreach (PartSim partSim in drainingParts) {
                        double time = partSim.TimeToDrainResource();
                        if (time < resourceDrainTime) {
                            resourceDrainTime = time;
                            partMinDrain = partSim;
                        }
                    }

                    if (debug) Debug.Log("Drain time = " + resourceDrainTime + " (" + partMinDrain.name + ":" + partMinDrain.partId + ")");

                    foreach (PartSim partSim in drainingParts) {
                        partSim.DrainResources(resourceDrainTime);
                    }

                    // Get the mass after draining
                    stepEndMass = ShipMass;
                    stageTime += resourceDrainTime;

                    double stepEndTWR = totalStageThrust / (stepEndMass * gravity);
                    /*if (log != null)
                    {
                        log.AppendLine("After drain mass = ", stepEndMass);
                        log.AppendLine("currentThrust = ", totalStageThrust);
                        log.AppendLine("currentTWR = ", stepEndTWR);
                    }*/
                    if (stepEndTWR > stage.maxThrustToWeight) {
                        stage.maxThrustToWeight = stepEndTWR;
                    }

                    //if (log != null) log.AppendLine("newMaxTWR = ", stage.maxThrustToWeight);

                    // If we have drained anything and the masses make sense then add this step's deltaV to the stage total
                    if (resourceDrainTime > 0d && stepStartMass > stepEndMass && stepStartMass > 0d && stepEndMass > 0d) {
                        vecStageDeltaV += vecThrust * (float)((currentisp * Units.GRAVITY * Math.Log(stepStartMass / stepEndMass)) / simpleTotalThrust);
                    }

                    // Update the active engines and resource drains for the next step
                    UpdateResourceDrains();

                    // Recalculate the current thrust and isp for the next step
                    CalculateThrustAndISP();

                    // Check if we actually changed anything
                    if (stepStartMass == stepEndMass) {
                        //MonoBehaviour.print("No change in mass");
                        break;
                    }

                    // Check to stop rampant looping
                    if (loopCounter == 1000) {
                        if (debug) {
                            Debug.Log("exceeded loop count");
                            Debug.Log("stageStartMass = " + stageStartMass);
                            Debug.Log("stepStartMass = " + stepStartMass);
                            Debug.Log("StepEndMass   = " + stepEndMass);
                        }
                        break;
                    }

                    // The next step starts at the mass this one ended at
                    stepStartMass = stepEndMass;
                }

                // Store more values in the Stage object and stick it in the array

                // Store the magnitude of the deltaV vector
                stage.deltaV = vecStageDeltaV.magnitude;
                stage.resourceMass = stageStartMass - stepEndMass;

                if (HighLogic.LoadedSceneIsEditor) //this is only needed in the VAB.
                    CalculateRCS(gravity, true);

                stage.RCSdeltaVEnd = RCSDeltaV;
                stage.RCSTWREnd = RCSTWR;

                // Recalculate effective stage isp from the stage deltaV (flip the standard deltaV calculation around)
                // Note: If the mass doesn't change then this is a divide by zero
                if (stageStartMass != stepStartMass) {
                    stage.isp = stage.deltaV / (Units.GRAVITY * Math.Log(stageStartMass / stepStartMass));
                } else {
                    stage.isp = 0;
                }

                // Zero stage time if more than a day (this should be moved into the window code)
                stage.time = (stageTime < SECONDS_PER_DAY) ? stageTime : 0d;
                stage.number = doingCurrent ? -1 : currentStage; // Set the stage number to -1 if doing current engines
                stage.totalPartCount = allParts.Count;
                stage.maxMach = maxMach;
                stages[currentStage] = stage;

                // Now activate the next stage
                currentStage--;
                doingCurrent = false;

                // Activate the next stage
                ActivateStage();
            }

            // Now we add up the various total fields in the stages
            for (int i = 0; i < stages.Length; i++) {
                // For each stage we total up the cost, mass, deltaV and time for this stage and all the stages above
                for (int j = i; j >= 0; j--) {
                    stages[i].totalDeltaV += stages[j].deltaV;
                    stages[i].totalTime += stages[j].time;
                    stages[i].partCount = i > 0 ? stages[i].totalPartCount - stages[i - 1].totalPartCount : stages[i].totalPartCount;
                }
                // We also total up the deltaV for stage and all stages below
                for (int j = i; j < stages.Length; j++) {
                    stages[i].inverseTotalDeltaV += stages[j].deltaV;
                }

                // Zero the total time if the value will be huge (24 hours?) to avoid the display going weird
                // (this should be moved into the window code)
                if (stages[i].totalTime > SECONDS_PER_DAY) {
                    stages[i].totalTime = 0d;
                }
            }

            FreePooledObject();

            return stages;
        }

        // This function activates the next stage
        // currentStage must be updated before calling this function
        private void ActivateStage() {
            // Build a set of all the parts that will be decoupled
            decoupledParts.Clear();
            for (int i = 0; i < allParts.Count; ++i) {
                PartSim partSim = allParts[i];

                if (partSim.decoupledInStage >= currentStage) {
                    decoupledParts.Add(partSim);
                }
            }

            foreach (PartSim partSim in decoupledParts) {
                // Remove it from the all parts list
                allParts.Remove(partSim);
                partSim.Release();

                if (partSim.isEngine) {
                    // If it is an engine then loop through all the engine modules and remove all the ones from this engine part
                    for (int i = allEngines.Count - 1; i >= 0; i--) {
                        EngineSim engine = allEngines[i];
                        if (engine.partSim == partSim) {
                            allEngines.RemoveAt(i);
                            engine.Release();
                        }
                    }
                }

                if (partSim.isRCS) {
                    // If it is an engine then loop through all the engine modules and remove all the ones from this engine part
                    for (int i = allRCS.Count - 1; i >= 0; i--) {
                        RCSSim engine = allRCS[i];
                        if (engine.partSim == partSim) {
                            allRCS.RemoveAt(i);
                            engine.Release();
                        }
                    }
                }

                // If it is a fuel line then remove it from the list of all fuel lines
                if (partSim.isFuelLine) {
                    allFuelLines.Remove(partSim);
                }
            }

            // Loop through all the (remaining) parts
            for (int i = 0; i < allParts.Count; ++i) {
                // Ask the part to remove all the parts that are decoupled
                allParts[i].RemoveAttachedParts(decoupledParts);
            }

            // Now we loop through all the engines and activate those that are ignited in this stage
            for (int i = 0; i < allEngines.Count; ++i) {
                EngineSim engine = allEngines[i];
                if (engine.partSim.inverseStage == currentStage) {
                    engine.isActive = true;
                }
            }
        }

        // This function works out if it is time to stage
        private bool AllowedToStage() {
            if (debug) Debug.Log("AllowedToStage: currentStage = " + currentStage);

            if (activeEngines.Count > 0) {
                for (int i = 0; i < dontStageParts.Count; ++i) {
                    PartSim partSim = dontStageParts[i];

                    if (debug) partSim.DumpPartToLog("Testing: ");
                    //if (log != null) log.AppendLine("isSepratron = ", partSim.isSepratron ? "true" : "false");

                    if (!partSim.isSepratron && !partSim.EmptyOf(drainingResources)) {
                        if (debug) partSim.DumpPartToLog("Decoupled part not empty => false: ");
                        return false;
                    }

                    if (partSim.isEngine) {
                        for (int j = 0; j < activeEngines.Count; ++j) {
                            EngineSim engine = activeEngines[j];

                            if (engine.dontDecoupleActive && engine.partSim == partSim) {
                                if (debug) partSim.DumpPartToLog( "Decoupled part is active engine => false: ");
                                return false;
                            }
                        }
                    }
                }
            }

            if (currentStage == 0 && doingCurrent) {
                if (debug) Debug.Log("Current stage == 0 && doingCurrent => false");
                return false;
            }

            if (debug) Debug.Log("Returning true");
            return true;
        }

        // This function simply rebuilds the active engines by testing the isActive flag of all the engines
        private void UpdateActiveEngines() {
            activeEngines.Clear();
            for (int i = 0; i < allEngines.Count; ++i) {
                EngineSim engine = allEngines[i];
                if (engine.isActive && engine.isFlamedOut == false) {
                    activeEngines.Add(engine);
                }
            }
        }

        // This function does all the hard work of working out which engines are burning, which tanks are being drained
        // and setting the drain rates
        private void UpdateResourceDrains() {
            // Update the active engines
            UpdateActiveEngines();

            // Empty the draining resources set
            drainingResources.Clear();

            // Reset the resource drains of all draining parts
            foreach (PartSim partSim in drainingParts) {
                partSim.resourceDrains.Reset();
            }

            // Empty the draining parts set
            drainingParts.Clear();

            // Loop through all the active engine modules
            for (int i = 0; i < activeEngines.Count; ++i) {
                EngineSim engine = activeEngines[i];

                // Set the resource drains for this engine
                if (engine.SetResourceDrains(allParts, allFuelLines, drainingParts, debug)) {
                    // If it is active then add the consumed resource types to the set
                    for (int j = 0; j < engine.ResourceConsumptions.Types.Count; ++j) {
                        drainingResources.Add(engine.ResourceConsumptions.Types[j]);
                    }
                }
            }

            // Update the active engines again to remove any engines that have no fuel supply
            UpdateActiveEngines();

            if (debug) {
                Debug.Log("Active engines = " + activeEngines.Count);
                int i = 0;
                for (int j = 0; j < activeEngines.Count; j++) {
                    EngineSim engine = activeEngines[j];
                    Debug.Log("Engine " + (i++) + ":");
                    engine.DumpEngineToLog();
                }
            }
        }

        public double UpdatePartMasses() {
            for (int i = 0; i < allParts.Count; i++) {
                allParts[i].baseMass = allParts[i].realMass;
                allParts[i].baseMassForCoM = allParts[i].realMass;
            }

            for (int i = 0; i < allParts.Count; i++) {
                PartSim part = allParts[i];

                // Check if part should pass it's mass onto its parent.
                if (part.isNoPhysics && part.parent != null) {
                    PartSim partParent = part.parent;

                    // Loop through all parents until a physically significant parent is found.
                    while (partParent != null) {
                        // Check if parent is physically significant.
                        if (partParent.isNoPhysics == false) {
                            // Apply the mass to the parent and remove it from the originating part.
                            partParent.baseMassForCoM += part.baseMassForCoM;
                            part.baseMassForCoM = 0.0;

                            // Break out of the recursive loop.
                            break;
                        }

                        // Recursively loop through the parent parts.
                        partParent = partParent.parent;
                    }
                }
            }

            double totalMass = 0d;
            for (int i = 0; i < allParts.Count; i++) {
                totalMass += allParts[i].startMass = allParts[i].GetMass(currentStage);
            }

            return totalMass;
        }

        private void CalculateThrustAndISP() {
            // Reset all the values
            vecThrust = Vector3.zero;
            vecActualThrust = Vector3.zero;
            simpleTotalThrust = 0d;
            totalStageThrust = 0d;
            totalStageActualThrust = 0d;
            totalStageFlowRate = 0d;
            totalStageIspFlowRate = 0d;
            totalStageThrustForce.Reset();

            // Loop through all the active engines totalling the thrust, actual thrust and mass flow rates
            // The thrust is totalled as vectors

            for (int i = 0; i < activeEngines.Count; ++i) {
                EngineSim engine = activeEngines[i];

                simpleTotalThrust += engine.thrust;
                vecThrust += ((float)engine.thrust * engine.thrustVec);
                vecActualThrust += ((float)engine.actualThrust * engine.thrustVec);

                totalStageFlowRate += engine.ResourceConsumptions.Mass;
                totalStageIspFlowRate += engine.ResourceConsumptions.Mass * engine.isp;

                for (int j = 0; j < engine.appliedForces.Count; ++j) {
                    totalStageThrustForce.AddForce(engine.appliedForces[j]);
                }
            }

            if (debug) Debug.Log("vecThrust = " + vecThrust.ToString() + "   magnitude = " + vecThrust.magnitude);
            totalStageThrust = vecThrust.magnitude;
            totalStageActualThrust = vecActualThrust.magnitude;

            // Calculate the effective isp at this point
            if (totalStageFlowRate > 0d && totalStageIspFlowRate > 0d) {
                currentisp = totalStageIspFlowRate / totalStageFlowRate;
            } else {
                currentisp = 0;
            }
        }

        //by jrbudda.
        private void CalculateRCS(double localGravity, bool final) {
            // Reset all the values
            var vecThrust = Vector3.zero;
            var vecActualThrust = Vector3.zero;
            var simpleTotalThrust = 0d;
            var totalStageFlowRate = 0d;
            var totalStageIspFlowRate = 0d;

            var FlowRateByFuel = new Dictionary<int, double>();
            var IspFlowRateByFuel = new Dictionary<int, double>();
            var FuelSources = new Dictionary<int, double>();
            var thrustByFuel = new Dictionary<int, double>();

            var fueltypes = new HashSet<int>();

            //populate
            foreach (RCSSim engine in allRCS) {

                engine.DumpEngineToLog();

                foreach (int type in engine.resourceConsumptions.Types) {
                    if (!fueltypes.Contains(type)) {
                        fueltypes.Add(type);
                        FlowRateByFuel.Add(type, 0);
                        IspFlowRateByFuel.Add(type, 0);
                        thrustByFuel.Add(type, 0);
                        FuelSources.Add(type, 0);
                    }
                }
            }
            foreach (int fuel in fueltypes) {
                foreach (PartSim p in allParts) {
                    if (p.resources.HasType(fuel) && p.resourceFlowStates[fuel] > 0.0) {
                        FuelSources[fuel] += p.resources[fuel];
                    }
                }
            }

            foreach (RCSSim engine in allRCS) {
                bool active = engine.isActive;

                foreach (int type in engine.resourceConsumptions.Types) {
                    if (FuelSources[type] < SimManager.RESOURCE_MIN)
                        active = false;
                }

                if (active) {
                    simpleTotalThrust += engine.thrust;
                    vecThrust += ((float)engine.thrust * engine.thrustVec);
                    totalStageFlowRate += engine.resourceConsumptions.Mass;
                    totalStageIspFlowRate += engine.resourceConsumptions.Mass * engine.isp;
                }
            }


            var preMAss = ShipMass;
            var mass = preMAss;

            double deltav = 0;
            RCSBurnTime = 0;
            int loopcount = 0;


            if (debug) {
                Debug.Log("**RCS PRE ");
                Debug.Log("   StartingMss = " + mass);
                foreach (var type in fueltypes) {
                    Debug.Log("   **Fuel " + type);
                    Debug.Log("      FuelMass = " + FuelSources[type]);
                }
            }

            //simulate
            while (true) {

                //so... many... loops....

                //reset
                foreach (int fuel in fueltypes) {
                    thrustByFuel[fuel] = 0;
                    FlowRateByFuel[fuel] = 0;
                    IspFlowRateByFuel[fuel] = 0;
                }

                var combinedFlowRate = 0d;
                var combinedIspFlowRate = 0d;

                //check for flamed out engines.
                foreach (RCSSim engine in allRCS) {
                    bool active = engine.isActive;

                    foreach (int type in engine.resourceConsumptions.Types) {
                        if (FuelSources[type] < SimManager.RESOURCE_MIN)
                            active = false;
                    }

                    if (active) {
                        foreach (int type in engine.resourceConsumptions.Types) {
                            thrustByFuel[type] += engine.thrust;
                            FlowRateByFuel[type] += engine.resourceConsumptions[type];
                            IspFlowRateByFuel[type] += engine.resourceConsumptions[type] * engine.isp;

                            combinedFlowRate += engine.resourceConsumptions[type];
                            combinedIspFlowRate += engine.resourceConsumptions[type] * engine.isp;

                        }
                    }
                }


                double startmass = mass;

                double burnTime = double.MaxValue;

                foreach (int fuel in fueltypes) {
                    //min time til something goes empty.
                    if (FuelSources[fuel] > SimManager.RESOURCE_MIN) {
                        if (FlowRateByFuel[fuel] > 0) {
                            var time = FuelSources[fuel] / FlowRateByFuel[fuel];
                            if (time < burnTime)
                                burnTime = time;
                        }
                    }
                }

                if (burnTime > 0 && burnTime != double.MaxValue) {
                    //subtract masses.
                    foreach (var type in fueltypes) {
                        if (FuelSources[type] > SimManager.RESOURCE_MIN) {
                            var sub = burnTime * FlowRateByFuel[type];
                            mass -= sub * PartResourceLibrary.Instance.GetDefinition(type).density;
                            FuelSources[type] -= sub;
                        }
                    }
                }

                if (debug) {
                    Debug.Log("**RCS STEP " + loopcount);
                    Debug.Log("   burnTime = " + burnTime);
                    Debug.Log("   StartMass = " + startmass);
                    Debug.Log("   StepEndMass = " + mass);
                    foreach (var type in fueltypes) {
                        Debug.Log("   **Fuel " + type);
                        Debug.Log("      FuelMass = " + FuelSources[type]);
                        Debug.Log("      FlowRateByFuel = " + FlowRateByFuel[type]);
                        Debug.Log("      IspFlowRateByFuel = " + IspFlowRateByFuel[type]);
                        Debug.Log("      thrustByFuel = " + thrustByFuel[type]);
                        Debug.Log("      fuelDensity = " + PartResourceLibrary.Instance.GetDefinition(type).density);
                    }
                }

                if (startmass == mass) //we done here.
                    break;

                var isp = combinedIspFlowRate > 0 && combinedFlowRate > 0 ? combinedIspFlowRate / combinedFlowRate : 0;
                deltav += (float)((isp * Units.GRAVITY * Math.Log(startmass / mass)));

                RCSBurnTime += burnTime;
                loopcount++;

                if (loopcount == 1000) {
                    Debug.Log("RCS exceeded loop count");
                    Debug.Log("stageStartMass = " + preMAss);
                    Debug.Log("burnTime = " + RCSBurnTime);
                    Debug.Log("StepEndMass   = " + mass);
                    break;
                }

            }

            RCSThrust = simpleTotalThrust;
            RCSDeltaV = deltav;

            if (final) //calculate TWR assuming all RCS has been consumed as well.
                RCSTWR = RCSThrust / mass / localGravity;
            else //calculate twr using full mass.
                RCSTWR = RCSThrust / ShipMass / localGravity;


            if (totalStageFlowRate > 0d && totalStageIspFlowRate > 0d) {
                RCSIsp = totalStageIspFlowRate / totalStageFlowRate;
            } else {
                RCSIsp = 0;
            }

        }


        private void BuildDontStageLists() {
            if (debug) Debug.Log("Creating list with capacity of " + (currentStage + 1));

            dontStagePartsLists.Clear();
            for (int i = 0; i <= currentStage; i++) {
                if (i < dontStagePartsLists.Count) {
                    dontStagePartsLists[i].Clear();
                } else {
                    dontStagePartsLists.Add(new List<PartSim>());
                }
            }

            for (int i = 0; i < allParts.Count; ++i) {
                PartSim partSim = allParts[i];

                if (partSim.isEngine || !partSim.resources.Empty) {
                    if (debug) Debug.Log(partSim.name + ":" + partSim.partId + " is engine or tank, decoupled = " + partSim.decoupledInStage);

                    if (partSim.decoupledInStage < -1 || partSim.decoupledInStage > currentStage - 1) {
                        if (debug) Debug.Log("decoupledInStage out of range");
                    } else {
                        dontStagePartsLists[partSim.decoupledInStage + 1].Add(partSim);
                    }
                }
            }

            for (int i = 1; i <= lastStage; i++) {
                if (dontStagePartsLists[i].Count == 0) {
                    dontStagePartsLists[i] = dontStagePartsLists[i - 1];
                }
            }
        }

        // Make sure we free them all, even if they should all be free already at this point
        public void FreePooledObject() {
            //MonoBehaviour.print("FreePooledObject pool size before = " + PartSim.pool.Count() + " for " + allParts.Count + " parts");
            foreach (PartSim part in allParts) {
                part.Release();
            }
            //MonoBehaviour.print("FreePooledObject pool size after = " + PartSim.pool.Count());

            //MonoBehaviour.print("FreePooledObject pool size before = " + EngineSim.pool.Count() + " for " + allEngines.Count + " engines");
            foreach (EngineSim engine in allEngines) {
                engine.Release();
            }
            //MonoBehaviour.print("FreePooledObject pool size after = " + EngineSim.pool.Count());

            foreach (RCSSim engine in allRCS) {
                engine.Release();
            }
        }

        private double ShipMass {
            get {
                double mass = 0d;

                for (int i = 0; i < allParts.Count; ++i) {
                    mass += allParts[i].GetMass(currentStage);
                }

                return mass;
            }
        }

        private Vector3d ShipCom {
            get {
                vectorAverager.Reset();

                for (int i = 0; i < allParts.Count; ++i) {
                    PartSim partSim = allParts[i];
                    vectorAverager.Add(partSim.centerOfMass, partSim.GetMass(currentStage, true));
                }

                return vectorAverager.Get();
            }
        }
    }
}
