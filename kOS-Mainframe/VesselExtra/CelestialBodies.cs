using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace kOSMainframe.VesselExtra {
    public class CelestialBodies {
        static CelestialBodies() {
            try {
                SystemBody = new BodyInfo(PSystemManager.Instance.localBodies.Find(b => b.referenceBody == null || b.referenceBody == b));
                String homeCBName = Planetarium.fetch.Home.bodyName;
                if (!SetSelectedBody(homeCBName)) {
                    SelectedBody = SystemBody;
                    SelectedBody.SetSelected(true);
                }
            } catch (Exception ex) {
                Debug.Log(ex);
            }
        }

        public static BodyInfo SelectedBody {
            get;
            private set;
        }
        public static BodyInfo SystemBody {
            get;
            private set;
        }

        /// <summary>
        ///     Gets a body given a supplied body name.
        /// </summary>
        public static BodyInfo GetBodyInfo(string bodyName) {
            try {
                return SystemBody.GetBodyInfo(bodyName);
            } catch (Exception ex) {
                Debug.Log(ex);
            }
            return null;
        }

        /// <summary>
        ///     Sets the selected body to one matching the supplied body name.  Returns true if successful.
        /// </summary>
        public static bool SetSelectedBody(string bodyName) {
            try {
                BodyInfo body = GetBodyInfo(bodyName);
                if (body != null) {
                    if (SelectedBody != null) {
                        SelectedBody.SetSelected(false);
                    }
                    SelectedBody = body;
                    SelectedBody.SetSelected(true);
                    return true;
                }
            } catch (Exception ex) {
                Debug.Log(ex);
            }
            return false;
        }

        public class BodyInfo {
            public BodyInfo(CelestialBody body, BodyInfo parent = null) {
                try {
                    // Set the body information.
                    CelestialBody = body;
                    Name = body.bodyName;
                    Gravity = 9.81 * body.GeeASL;
                    Parent = parent;

                    // Set orbiting bodies information.
                    Children = new List<BodyInfo>();
                    foreach (CelestialBody orbitingBody in body.orbitingBodies) {
                        Children.Add(new BodyInfo(orbitingBody, this));
                    }

                    SelectedDepth = 0;
                } catch (Exception ex) {
                    Debug.Log(ex);
                }
            }

            public CelestialBody CelestialBody {
                get;
                private set;
            }
            public List<BodyInfo> Children {
                get;
                private set;
            }
            public double Gravity {
                get;
                private set;
            }
            public string Name {
                get;
                private set;
            }
            public BodyInfo Parent {
                get;
                private set;
            }
            public bool Selected {
                get;
                private set;
            }
            public int SelectedDepth {
                get;
                private set;
            }

            public BodyInfo GetBodyInfo(string bodyName) {
                try {
                    // This is the searched body.
                    if (String.Equals(Name, bodyName, StringComparison.CurrentCultureIgnoreCase)) {
                        return this;
                    }

                    // Check to see if any of this bodies children are the searched body.
                    foreach (BodyInfo child in Children) {
                        BodyInfo body = child.GetBodyInfo(bodyName);
                        if (body != null) {
                            return body;
                        }
                    }
                } catch (Exception ex) {
                    Debug.Log(ex);
                }

                // A body with the specified name was not found.
                return null;
            }

            public double GetDensity(double altitude) {
                return CelestialBody.GetDensity(GetPressure(altitude), GetTemperature(altitude));
            }

            public double GetPressure(double altitude) {
                return CelestialBody.GetPressure(altitude);
            }

            public double GetTemperature(double altitude) {
                return CelestialBody.GetTemperature(altitude);
            }

            public double GetAtmospheres(double altitude) {
                return GetPressure(altitude) * PhysicsGlobals.KpaToAtmospheres;
            }

            public void SetSelected(bool state, int depth = 0) {
                Selected = state;
                SelectedDepth = depth;
                if (Parent != null) {
                    Parent.SetSelected(state, depth + 1);
                }
            }

            public override string ToString() {
                string log = "\n" + Name +
                             "\n\tGravity: " + Gravity +
                             "\n\tSelected: " + Selected;

                return Children.Aggregate(log, (current, child) => current + "\n" + child);
            }
        }
    }
}
