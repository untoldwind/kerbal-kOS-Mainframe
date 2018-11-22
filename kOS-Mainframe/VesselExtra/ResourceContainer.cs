using System;
using System.Collections.Generic;
using UnityEngine;

namespace kOSMainframe.VesselExtra {
    public class ResourceContainer {
        private Dictionary<int, double> resources = new Dictionary<int, double>();
        private List<int> types = new List<int>();

        public double this[int type] {
            get {
                double value;
                if (this.resources.TryGetValue(type, out value)) {
                    return value;
                }

                return 0d;
            }
            set {
                if (this.resources.ContainsKey(type)) {
                    this.resources[type] = value;
                } else {
                    this.resources.Add(type, value);
                    this.types.Add(type);
                }
            }
        }

        public List<int> Types {
            get {
                return types;
            }
        }

        public double Mass {
            get {
                double mass = 0d;

                foreach (double resource in this.resources.Values) {
                    mass += resource;
                }

                return mass;
            }
        }

        public bool Empty {
            get {
                foreach (int type in this.resources.Keys) {
                    if (this.resources[type] > SimManager.RESOURCE_MIN) {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool HasType(int type) {
            return this.resources.ContainsKey(type);
        }

        public bool EmptyOf(HashSet<int> types) {
            foreach (int type in types) {
                if (this.HasType(type) && this.resources[type] > SimManager.RESOURCE_MIN) {
                    return false;
                }
            }

            return true;
        }

        public void Add(int type, double amount) {
            if (this.resources.ContainsKey(type)) {
                this.resources[type] = this.resources[type] + amount;
            } else {
                this.resources.Add(type, amount);
                this.types.Add(type);
            }
        }

        public void Reset() {
            this.resources.Clear();
            this.types.Clear();
        }

        public void LogDebug() {
            foreach (int key in this.resources.Keys) {
                Debug.Log(" -> " + GetResourceName(key) + " = " + this.resources[key]);
            }
        }

        public double GetResourceMass(int type) {
            double density = GetResourceDensity(type);
            return density == 0d ? 0d : this.resources[type] * density;
        }

        public static ResourceFlowMode GetResourceFlowMode(int type) {
            return PartResourceLibrary.Instance.GetDefinition(type).resourceFlowMode;
        }

        public static ResourceTransferMode GetResourceTransferMode(int type) {
            return PartResourceLibrary.Instance.GetDefinition(type).resourceTransferMode;
        }

        public static float GetResourceDensity(int type) {
            return PartResourceLibrary.Instance.GetDefinition(type).density;
        }

        public static string GetResourceName(int type) {
            return PartResourceLibrary.Instance.GetDefinition(type).name;
        }

        public static double GetResourceUnitCost(int type) {
            return PartResourceLibrary.Instance.GetDefinition(type).unitCost;
        }
    }
}
