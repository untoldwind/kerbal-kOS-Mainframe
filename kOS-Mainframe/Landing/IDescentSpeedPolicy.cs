using System;
namespace kOSMainframe.Landing {
    //An IDescentSpeedPolicy describes a strategy for doing the braking burn.
    //while landing. The function MaxAllowedSpeed is supposed to compute the maximum allowed speed
    //as a function of body-relative position and rotating frame, surface-relative velocity.
    //This lets the ReentrySimulator simulate the descent of a vessel following this policy.
    //
    //Note: the IDescentSpeedPolicy object passed into the simulation will be used in the separate simulation
    //thread. It must not point to mutable objects that may change over the course of the simulation. Similarly,
    //you must be sure not to modify the IDescentSpeedPolicy object itself after passing it to the simulation.
    public interface IDescentSpeedPolicy {
        double MaxAllowedSpeed(Vector3d pos, Vector3d surfaceVel);
    }
}
