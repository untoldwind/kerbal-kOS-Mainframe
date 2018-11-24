using System;
using UnityEngine;
using kOSMainframe.UnityToolbag;

namespace kOSMainframe.VesselExtra {
    public class AttachNodeSim {
        private static readonly Pool<AttachNodeSim> pool = new Pool<AttachNodeSim>(Create, Reset);

        public PartSim attachedPartSim;
        public String id;
        public AttachNode.NodeType nodeType;

        private static AttachNodeSim Create() {
            return new AttachNodeSim();
        }

        public static AttachNodeSim New(PartSim partSim, String newId, AttachNode.NodeType newNodeType) {
            AttachNodeSim nodeSim = pool.Borrow();

            nodeSim.attachedPartSim = partSim;
            nodeSim.nodeType = newNodeType;
            nodeSim.id = newId;

            return nodeSim;
        }

        static private void Reset(AttachNodeSim attachNodeSim) {
            attachNodeSim.attachedPartSim = null;
        }


        public void Release() {
            pool.Release(this);
        }

        public void DumpToLog() {
            if (attachedPartSim == null) {
                Debug.Log("<staged>:<n>");
            } else {
                Debug.Log(attachedPartSim.name + ":" + attachedPartSim.partId);
            }
            Debug.Log("#" + nodeType + ":" + id);
        }
    }
}
