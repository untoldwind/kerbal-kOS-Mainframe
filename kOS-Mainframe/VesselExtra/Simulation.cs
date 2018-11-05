using System;
using System.Collections.Generic;
using UnityEngine;
using CompoundParts;
using KSP.UI.Screens;

namespace kOSMainframe.VesselExtra
{
    public class Simulation
    {
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

        public Simulation()
        {
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

        public bool PrepareSimulation(List<Part> parts, double theGravity, double theAtmosphere = 0, double theMach = 0, bool dumpTree = false, bool vectoredThrust = false, bool fullThrust = false, bool _debug = false)
        {
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

            if (partList.Count > 0 && partList[0].vessel != null)
            {
                vesselName = partList[0].vessel.vesselName;
                vesselType = partList[0].vessel.vesselType;
            }
            // First we create a PartSim for each Part (giving each a unique id)
            int partId = 1;
            for (int i = 0; i < partList.Count; ++i)
            {
                Part part = partList[i];

                // If the part is already in the lookup dictionary then log it and skip to the next part
                if (partSimLookup.ContainsKey(part))
                {
                    if (debug) Debug.Log("Part " + part.name + " appears in vessel list more than once");
                    continue;
                }

                // Create the PartSim
                PartSim partSim = PartSim.New(part, partId, debug);

                // Add it to the Part lookup dictionary and the necessary lists
                partSimLookup.Add(part, partSim);
                allParts.Add(partSim);

                if (partSim.isFuelLine)
                {
                    allFuelLines.Add(partSim);
                }

                if (partSim.isEngine)
                {
                    partSim.CreateEngineSims(allEngines, atmosphere, mach, vectoredThrust, fullThrust, debug);
                }

                if (partSim.isRCS)
                {
                    partSim.CreateRCSSims(allRCS, atmosphere, mach, vectoredThrust, fullThrust, debug);
                }

                partId++;
            }

            for (int i = 0; i < allEngines.Count; ++i)
            {
                maxMach = Mathf.Max(maxMach, allEngines[i].maxMach);
            }

            UpdateActiveEngines();

            // Now that all the PartSims have been created we can do any set up that needs access to other parts
            // First we set up all the parent links
            for (int i = 0; i < allParts.Count; i++)
            {
                PartSim partSim = allParts[i];
                partSim.SetupParent(partSimLookup, debug);
            }

            if (debug) Debug.Log("SetupAttachNodes and count stages");
            for (int i = 0; i < allParts.Count; ++i)
            {
                PartSim partSim = allParts[i];

                partSim.SetupAttachNodes(partSimLookup, debug);
                if (partSim.decoupledInStage >= lastStage)
                {
                    lastStage = partSim.decoupledInStage + 1;
                }
            }

            // And finally release the Part references from all the PartSims
            if (debug) Debug.Log("ReleaseParts");
            for (int i = 0; i < allParts.Count; ++i)
            {
                allParts[i].ReleasePart();
            }

            // And dereference the core's part list
            partList = null;

            return true;
        }

        // This function simply rebuilds the active engines by testing the isActive flag of all the engines
        private void UpdateActiveEngines()
        {
            activeEngines.Clear();
            for (int i = 0; i < allEngines.Count; ++i)
            {
                EngineSim engine = allEngines[i];
                if (engine.isActive && engine.isFlamedOut == false)
                {
                    activeEngines.Add(engine);
                }
            }
        }
    }
}
