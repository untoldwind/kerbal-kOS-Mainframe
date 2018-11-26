using System;
using kOSMainframe.ExtraMath;
using UnityEngine;

namespace kOSMainframeTest {
    public class AmoebaOptimizerTest {
        class TestFunc3 : Function3 {
            public double Evaluate(Vector3d p) {
                return 0.5 - Bessel.J0((p.x - 1.0) * (p.x - 1.0) + (p.y - 2.0) * (p.y - 2.0) + (p.z - 3.0) * (p.z - 3.0));
            }
        }

    }
}
