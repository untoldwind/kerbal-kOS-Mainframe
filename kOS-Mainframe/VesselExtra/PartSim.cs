using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace kOSMainframe.VesselExtra
{
    public class PartSim
    {
        private static readonly Pool<PartSim> pool = new Pool<PartSim>(Create, Reset);

        private readonly List<AttachNodeSim> attachNodes = new List<AttachNodeSim>();

        public double realMass;
        public double baseMass;
        public double baseMassForCoM;
        public Vector3d centerOfMass;
        public int decoupledInStage;
        public bool fuelCrossFeed;
        public List<PartSim> fuelTargets = new List<PartSim>();
        public List<PartSim> surfaceMountFuelTargets = new List<PartSim>();
        public bool hasModuleEngines;
        public bool hasMultiModeEngine;

        public bool hasVessel;
        public String initialVesselName;
        public int inverseStage;
        public int resPriorityOffset;
        public bool resPriorityUseParentInverseStage;
        public double resRequestRemainingThreshold;
        public bool isEngine;
        public bool isRCS;
        public bool isFuelLine;
        public bool isFuelTank;
        public bool isLanded;
        public bool isNoPhysics;
        public bool isSepratron;
        public float postStageMassAdjust;
        public int stageIndex;
        public String name;
        public String noCrossFeedNodeKey;
        public PartSim parent;
        public AttachModes parentAttach;
        public Part part; // This is only set while the data structures are being initialised
        public int partId = 0;
        public ResourceContainer resourceDrains = new ResourceContainer();
        public ResourceContainer resourceFlowStates = new ResourceContainer();
        public ResourceContainer resources = new ResourceContainer();
        public double startMass = 0d;
        public String vesselName;
        public VesselType vesselType;
        public bool isEnginePlate;

        private static PartSim Create()
        {
            return new PartSim();
        }

        private static void Reset(PartSim partSim)
        {
            for (int i = 0; i < partSim.attachNodes.Count; i++)
            {
                partSim.attachNodes[i].Release();
            }
            partSim.attachNodes.Clear();
            partSim.fuelTargets.Clear();
            partSim.surfaceMountFuelTargets.Clear();
            partSim.resourceDrains.Reset();
            partSim.resourceFlowStates.Reset();
            partSim.resources.Reset();
            partSim.parent = null;
            partSim.baseMass = 0d;
            partSim.baseMassForCoM = 0d;
            partSim.startMass = 0d;
        }

        public void Release()
        {
            pool.Release(this);
        }

        public static PartSim New(Part p, int id, bool debug)
        {
            PartSim partSim = pool.Borrow();

            partSim.part = p;
            partSim.centerOfMass = p.transform.TransformPoint(p.CoMOffset);
            partSim.partId = id;
            partSim.name = p.partInfo.name;

            if (debug) Debug.Log("Create PartSim for " + partSim.name);

            partSim.parent = null;
            partSim.parentAttach = p.attachMode;
            partSim.fuelCrossFeed = p.fuelCrossFeed;
            partSim.noCrossFeedNodeKey = p.NoCrossFeedNodeKey;
            partSim.isEnginePlate = IsEnginePlate(p);
            if (partSim.isEnginePlate)
                partSim.noCrossFeedNodeKey = "bottom"; //sadly this only works in one direction.
            partSim.decoupledInStage = partSim.DecoupledInStage(p);
            partSim.isFuelLine = p.IsFuelLine();
            partSim.isRCS = p.HasModule<ModuleRCS>() || p.HasModule<ModuleRCSFX>(); //I don't think it checks inheritance.
            partSim.isSepratron = partSim.IsSepratron();
            partSim.inverseStage = p.inverseStage;
            if (debug) Debug.Log("inverseStage = " + partSim.inverseStage);
            partSim.resPriorityOffset = p.resourcePriorityOffset;
            partSim.resPriorityUseParentInverseStage = p.resourcePriorityUseParentInverseStage;
            partSim.resRequestRemainingThreshold = p.resourceRequestRemainingThreshold;

            if (debug)
            {
                Debug.Log("Parent part = " + (p.parent == null ? "null" : p.parent.partInfo.name));
                Debug.Log("physicalSignificance = " + p.physicalSignificance);
                Debug.Log("PhysicsSignificance = " + p.PhysicsSignificance);
            }

            // Work out if the part should have no physical significance
            // The root part is never "no physics"
            partSim.isNoPhysics = p.physicalSignificance == Part.PhysicalSignificance.NONE ||
                                    p.PhysicsSignificance == 1;

            if (p.HasModule<LaunchClamp>())
            {
                partSim.realMass = 0d;
                if (debug) Debug.Log("Ignoring mass of launch clamp");
            }
            else
            {
                partSim.realMass = p.mass;

                if (debug) Debug.Log("Using part.mass of " + partSim.realMass);
            }

            partSim.postStageMassAdjust = 0f;
            if (debug) Debug.Log("Calculating postStageMassAdjust, prefabMass = " + p.prefabMass);
            int count = p.Modules.Count;
            for (int i = 0; i < count; i++)
            {
                if (debug) Debug.Log("Module: " + p.Modules[i].moduleName);
                IPartMassModifier partMassModifier = p.Modules[i] as IPartMassModifier;
                if (partMassModifier != null)
                {
                    if (debug) Debug.Log("ChangeWhen = " + partMassModifier.GetModuleMassChangeWhen());
                    if (partMassModifier.GetModuleMassChangeWhen() == ModifierChangeWhen.STAGED)
                    {
                        float preStage = partMassModifier.GetModuleMass(p.prefabMass, ModifierStagingSituation.UNSTAGED);
                        float postStage = partMassModifier.GetModuleMass(p.prefabMass, ModifierStagingSituation.STAGED);
                        if (debug) Debug.Log("preStage = " + preStage + "   postStage = " + postStage);
                        partSim.postStageMassAdjust += (postStage - preStage);
                    }
                }
            }

            if (debug) Debug.Log("postStageMassAdjust = " + partSim.postStageMassAdjust);

            for (int i = 0; i < p.Resources.Count; i++)
            {
                PartResource resource = p.Resources[i];

                // Make sure it isn't NaN as this messes up the part mass and hence most of the values
                // This can happen if a resource capacity is 0 and tweakable
                if (!Double.IsNaN(resource.amount))
                {
                    if (debug) Debug.Log(resource.resourceName + " = " + resource.amount);

                    partSim.resources.Add(resource.info.id, resource.amount);
                    partSim.resourceFlowStates.Add(resource.info.id, resource.flowState ? 1 : 0);
                }
                else
                {
                    if (debug) Debug.Log(resource.resourceName + " is NaN. Skipping.");
                }
            }

            partSim.hasVessel = (p.vessel != null);
            partSim.isLanded = partSim.hasVessel && p.vessel.Landed;
            if (partSim.hasVessel)
            {
                partSim.vesselName = p.vessel.vesselName;
                partSim.vesselType = p.vesselType;
            }
            partSim.initialVesselName = p.initialVesselName;

            partSim.hasMultiModeEngine = p.HasModule<MultiModeEngine>();
            partSim.hasModuleEngines = p.HasModule<ModuleEngines>();

            partSim.isEngine = partSim.hasMultiModeEngine || partSim.hasModuleEngines;

            if (debug) Debug.Log("Created " + partSim.name + ". Decoupled in stage " + partSim.decoupledInStage);

            return partSim;
        }

        public void ReleasePart()
        {
            this.part = null;
        }

        public void CreateEngineSims(List<EngineSim> allEngines, double atmosphere, double mach, bool vectoredThrust, bool fullThrust, bool debug)
        {
            if (debug) Debug.Log("CreateEngineSims for " + this.name);
            List<ModuleEngines> cacheModuleEngines = part.FindModulesImplementing<ModuleEngines>();

            try
            {
                if (cacheModuleEngines.Count > 0)
                {
                    //find first active engine, assuming that two are never active at the same time
                    foreach (ModuleEngines engine in cacheModuleEngines)
                    {
                        if (engine.isEnabled)
                        {
                            if (debug) Debug.Log("Module: " + engine.moduleName);
                            EngineSim engineSim = EngineSim.New(
                                this,
                                engine,
                                atmosphere,
                                (float)mach,
                                vectoredThrust,
                                fullThrust,
                                debug);
                            allEngines.Add(engineSim);
                        }
                    }
                }
            }
            catch
            {
                Debug.Log("[KER] Error Catch in CreateEngineSims");
            }
        }

        public void CreateRCSSims(List<RCSSim> allRCS, double atmosphere, double mach, bool vectoredThrust, bool fullThrust, bool debug)
        {
            if (debug) Debug.Log("CreateRCSSims for " + this.name);
            List<ModuleRCS> cacheModuleRCS = part.FindModulesImplementing<ModuleRCS>();

            try
            {
                if (cacheModuleRCS.Count > 0)
                {
                    //find first active engine, assuming that two are never active at the same time
                    foreach (ModuleRCS engine in cacheModuleRCS)
                    {
                        if (engine.isEnabled)
                        {
                            if (debug) Debug.Log("Module: " + engine.moduleName);
                            RCSSim engineSim = RCSSim.New(
                                this,
                                engine,
                                atmosphere,
                                (float)mach,
                                vectoredThrust,
                                fullThrust,
                                debug);
                            allRCS.Add(engineSim);
                        }
                    }
                }
            }
            catch
            {
                Debug.Log("[KER] Error Catch in CreateRCSSims");
            }
        }

        public void SetupAttachNodes(Dictionary<Part, PartSim> partSimLookup, bool debug)
        {
            if (debug) Debug.Log("SetupAttachNodes for " + name + ":" + partId);

            attachNodes.Clear();

            for (int i = 0; i < part.attachNodes.Count; ++i)
            {
                AttachNode attachNode = part.attachNodes[i];

                if (debug) Debug.Log("AttachNode " + attachNode.id + " = " + (attachNode.attachedPart != null ? attachNode.attachedPart.partInfo.name : "null"));

                if (attachNode.attachedPart != null && attachNode.id != "Strut")
                {
                    PartSim attachedSim;
                    if (partSimLookup.TryGetValue(attachNode.attachedPart, out attachedSim))
                    {
                        if (debug) Debug.Log("Adding attached node " + attachedSim.name + ":" + attachedSim.partId);

                        attachNodes.Add(AttachNodeSim.New(attachedSim, attachNode.id, attachNode.nodeType));
                    }
                    else
                    {
                        if (debug) Debug.Log("No PartSim for attached part (" + attachNode.attachedPart.partInfo.name + ")");
                    }
                }
            }

            for (int i = 0; i < part.fuelLookupTargets.Count; ++i)
            {
                Part p = part.fuelLookupTargets[i];

                if (p != null)
                {
                    PartSim targetSim;
                    if (partSimLookup.TryGetValue(p, out targetSim))
                    {
                        if (debug) Debug.Log("Fuel target: " + targetSim.name + ":" + targetSim.partId);

                        fuelTargets.Add(targetSim);
                    }
                    else
                    {
                        if (debug) Debug.Log("No PartSim for fuel target (" + p.name + ")");
                    }
                }
            }
        }

        public void SetupParent(Dictionary<Part, PartSim> partSimLookup, bool debug)
        {
            if (part.parent != null)
            {
                parent = null;
                if (partSimLookup.TryGetValue(part.parent, out parent))
                {
                    if (debug) Debug.Log("Parent part is " +  parent.name + ":"+ parent.partId);
                    if (part.attachMode == AttachModes.SRF_ATTACH && part.attachRules.srfAttach && part.fuelCrossFeed && part.parent.fuelCrossFeed)
                    {
                        if (debug)
                        {
                            Debug.Log("Added " + name + ":" + partId);
                            Debug.Log(", " + parent.name + ":" + parent.partId + " to surface mounted fuel targets.");
                        }
                        parent.surfaceMountFuelTargets.Add(this);
                        surfaceMountFuelTargets.Add(parent);
                    }
                }
                else
                {
                    if (debug) Debug.Log("No PartSim for parent part (" + part.parent.partInfo.name + ")");
                }
            }
        }

        private int DecoupledInStage(Part thePart)
        {
            int stage = -1;
            Part original = thePart;

            if (original.parent == null)
                return stage; //root part is always present. Fixes phantom stage if root is stageable.

            List<Part> chain = new List<Part>(); //prolly dont need a list, just the previous part but whatever.

            while (thePart != null)
            {

                chain.Add(thePart);

                if (thePart.inverseStage > stage)
                {

                    ModuleDecouple mdec = thePart.GetModule<ModuleDecouple>();
                    ModuleDockingNode mdock = thePart.GetModule<ModuleDockingNode>();
                    ModuleAnchoredDecoupler manch = thePart.GetModule<ModuleAnchoredDecoupler>();

                    if (mdec != null)
                    {
                        AttachNode att = thePart.FindAttachNode(mdec.explosiveNodeID);
                        if (mdec.isOmniDecoupler)
                            stage = thePart.inverseStage;
                        else
                        {
                            if (att != null)
                            {
                                if ((thePart.parent != null && att.attachedPart == thePart.parent) || chain.Contains(att.attachedPart))
                                    stage = thePart.inverseStage;
                            }
                            else stage = thePart.inverseStage;
                        }
                    }

                    if (manch != null) //radial decouplers (ALSO REENTRY PODS BECAUSE REASONS!)
                    {
                        AttachNode att = thePart.FindAttachNode(manch.explosiveNodeID); // these stupid fuckers don't initialize in the Editor scene.
                        if (att != null)
                        {
                            if ((thePart.parent != null && att.attachedPart == thePart.parent) || chain.Contains(att.attachedPart))
                                stage = thePart.inverseStage;
                        }
                        else stage = thePart.inverseStage; //radial decouplers it seems the attach node ('surface') comes back null.
                    }

                    if (mdock != null) //docking port
                    {
                        if (original == thePart)
                        {    //checking self, never leaves.

                        }
                        else stage = thePart.inverseStage;
                    }

                }

                thePart = thePart.parent;
            }

            return stage;
        }

        private static bool IsEnginePlate(Part thePart)
        {
            ModuleDecouple mdec = thePart.GetModule<ModuleDecouple>();
            if (mdec != null && mdec.IsStageable())
            {
                ModuleDynamicNodes mdyn = thePart.GetModule<ModuleDynamicNodes>();
                if (mdyn != null)
                    return true;
            }

            return false;
        }

        private bool IsSepratron()
        {
            if (!part.ActivatesEvenIfDisconnected)
            {
                return false;
            }

            IEnumerable<ModuleEngines> modList = part.Modules.OfType<ModuleEngines>();

            return modList.Any(module => module.throttleLocked);
        }
    }
}
