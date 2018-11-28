RUNONCEPATH("/core/lib_util").
RUNONCEPATH("/core/lib_ui").
RUNONCEPATH("/mainframe/exec_node").


FUNCTION landTimeToLong {
    PARAMETER lng.

    LOCAL SDAY IS SHIP:BODY:ROTATIONPERIOD. // Duration of Body day in seconds
    LOCAL KAngS IS 360/SDAY. // Rotation angular speed.
    LOCAL P IS SHIP:ORBIT:PERIOD.
    LOCAL SAngS IS (360/P) - KAngS. // Ship angular speed acounted for Body rotation.
    LOCAL TgtLong IS utilAngleTo360(lng).
    LOCAL ShipLong is utilAngleTo360(SHIP:LONGITUDE). 
    LOCAL DLong IS TgtLong - ShipLong. 
    IF DLong < 0 {
        RETURN (DLong + 360) / SAngS. 
    }
    ELSE {
        RETURN DLong / SAngS.
    }
}

FUNCTION landDeorbitDeltaV {
    parameter alt.
    // From node_apo.ks
    local mu is body:mu.
    local br is body:radius.

    // present orbit properties
    local vom is ship:obt:velocity:orbit:mag.      // actual velocity
    local r is br + altitude.                      // actual distance to body
    local ra is r.                                 // radius at burn apsis
    //local v1 is sqrt( vom^2 + 2*mu*(1/ra - 1/r) ). // velocity at burn apsis
    local v1 is vom.
    // true story: if you name this "a" and call it from circ_alt, its value is 100,000 less than it should be!
    local sma1 is obt:semimajoraxis.

    // future orbit properties
    local r2 is br + periapsis.                    // distance after burn at periapsis
    local sma2 is (alt + 2*br + periapsis)/2. // semi major axis target orbit
    local v2 is sqrt( vom^2 + (mu * (2/r2 - 2/r + 1/sma1 - 1/sma2 ) ) ).

    // create node
    local deltav is v2 - v1.
    return deltav.
}

function vacLandPrepareDeorbit {
    parameter LandingSite.

    LOCAL DeorbitRad is ship:body:radius - 1000.

    LOCAL r1 is ship:orbit:semimajoraxis.                               //Orbit now
    LOCAL r2 is DeorbitRad .                                            // Target orbit
    LOCAL pt is 0.5 * ((r1+r2) / (2*r2))^1.5.                           // How many orbits of a target in the target (deorbit) orbit will do.
    LOCAL sp is SQRT( ( 4 * constant:pi^2 * r2^3 ) / body:mu ).         // Period of the target orbit.
    LOCAL DeorbitTravelTime is pt*sp.                                   // Transit time 
    LOCAL phi is (DeorbitTravelTime/ship:body:rotationperiod) * 360.    // Phi in this case is not the angle between two orbits, but the angle the body rotates during the transit time
    LOCAL IncTravelTime is SHIP:ORBIT:period / 4. // Travel time between change of inclinationa and lower perigee
    LOCAL phiIncManeuver is (IncTravelTime/ship:body:rotationperiod) * 360.

    // Deorbit and plane change longitudes
    LOCAL Deorbit_Long to utilAngleTo360(LandingSite:LNG - 90).
    LOCAl PlaneChangeLong to utilAngleTo360(LandingSite:LNG - 180).

    IF SHIP:ORBIT:INCLINATION < -90 OR SHIP:ORBIT:INCLINATION > 90 {
        SET Deorbit_Long to utilAngleTo360(LandingSite:LNG + 90).
    }

    // Plane change for landing site
    LOCAL vel is velocityat(ship, landTimeToLong(PlaneChangeLong)):orbit.
    LOCAL inc is LandingSite:lat.
    LOCAL TotIncDV is 2 * vel:mag * sin(inc / 2).
    LOCAL nDv is vel:mag * sin(inc).
    LOCAL pDV is vel:mag * (cos(inc) - 1 ).

    if TotIncDV > 0.1 { // Only burn if it matters.
        uiDebug("Deorbit: Burning dV of " + round(TotIncDV,1) + " m/s @ anti-normal to change plane.").
        utilRemoveNodes().
        LOCAL nd IS NODE(time:seconds + landTimeToLong(PlaneChangeLong+phiIncManeuver), 0, -nDv, pDv).
        add nd.
        WAIT 0. 
        mainframeExecNode().
    }

    // Lower orbit over landing site
    local Deorbit_dV is landDeorbitDeltaV(DeorbitRad-body:radius).
    uiDebug("Deorbit: Burning dV of " + round(Deorbit_dV,1) + " m/s retrograde to deorbit.").
    LOCAL nd IS NODE(time:seconds + landTimeToLong(Deorbit_Long+phi) , 0, 0, Deorbit_dV).
    add nd. 
    mainframeExecNode(). 
    WAIT 0. 
    uiDebug("Deorbit: Deorbit burn done"). 
}

function vacLand {
    parameter LandLat is 0.
    parameter LandLng is 0.

    LOCAL LandingSite is LATLNG(LandLat,LandLng).

    addons:mainframe:landing:prediction_start(LandingSite).

    if ship:status = "ORBITING" {
        vacLandPrepareDeorbit(LandingSite).
    }
}