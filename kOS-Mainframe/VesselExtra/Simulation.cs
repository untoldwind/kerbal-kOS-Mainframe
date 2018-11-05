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
            return false;
        }
    }
}
