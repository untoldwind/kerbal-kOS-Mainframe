// Check if ship is facing a specific direction or vector. Useful for a wait until while turning.
// 
// Returns true if:
// Ship is facing the FaceVec whiting a tolerance of maxDeviationDegrees and
// with a Angular velocity less than maxAngularVelocity.
// 
// Paramters:
//   Direction or Vector to Check
//   Maximum deviation in degrees (default: 8)
//   Maximum angular velocity (default: 0.01) 
function utilIsShipFacing { 
  parameter face.
  parameter maxDeviationDegrees is 8.
  parameter maxAngularVelocity is 0.01.

  if face:ISTYPE("direction") { // Ensure that face is a vector
      set face to face:VECTOR.
  }

  return face:NORMALIZED * SHIP:FACING:FOREVECTOR:NORMALIZED >= COS(maxDeviationDegrees) and
         SHIP:ANGULARVEL:MAG < maxAngularVelocity. 
}

// remove all nodes and wait one tick if there was any
function utilRemoveNodes {
	IF not HASNODE return.
	FOR n IN ALLNODES REMOVE n.
	WAIT 0.
}

// convert any angle to range [0, 360)
function utilAngleTo360 {
	parameter a.
	set a to mod(a, 360).
	if a < 0 set a to a + 360.
	return a.
}

// convert from true to mean anomaly
function utilMeanFromTrue {
	parameter a.
	parameter obt is ORBIT.
	set e to obt:eccentricity.
	if e < 0.001 return a. //circular, no need for conversion
	if e >= 1 { print "ERROR: meanFromTrue("+round(a,2)+") with e=" + round(e,5). return a. }
	set a to a*0.5.
	set a to 2*arctan2(sqrt(1-e)*sin(a),sqrt(1+e)*cos(a)).
//	https://en.wikipedia.org/wiki/Eccentric_anomaly
//	https://en.wikipedia.org/wiki/Mean_anomaly
	return a - e * sin(a) * 180/constant:pi.
}

// eta to true anomaly (angle from periapsis in the direction of movement)
// note: this is the ultimate ETA function which is in KSP API known as GetDTforTrueAnomaly
function utilDtTrue {
	parameter a.
	parameter obt is ORBIT.
	return utilAngleTo360(utilMeanFromTrue(a) - utilMeanFromTrue(obt:trueAnomaly)) / 360 * obt:period.
}

// Determine the time of ship1's closest approach to ship2.
function utilClosestApproach {
  parameter ship1.
  parameter ship2.

  local Tmin is time:seconds.
  local Tmax is Tmin + 2 * max(ship1:obt:period, ship2:obt:period).
  local Rbest is (ship1:position - ship2:position):mag.
  local Tbest is 0.

  until Tmax - Tmin < 5 {
    local dt2 is (Tmax - Tmin) / 2.
    local Rl is utilCloseApproach(ship1, ship2, Tmin, Tmin + dt2).
    local Rh is utilCloseApproach(ship1, ship2, Tmin + dt2, Tmax).
    if Rl < Rh {
      set Tmax to Tmin + dt2.
    } else {
      set Tmin to Tmin + dt2.
    }
  }

  return (Tmax+Tmin) / 2.
}

// Given that ship1 "passes" ship2 during time span, find the APPROXIMATE
// distance of closest approach, but not precise! Use this iteratively to find
// the true closest approach.
function utilCloseApproach {
  parameter ship1.
  parameter ship2.
  parameter Tmin.
  parameter Tmax.

  local Rbest is (ship1:position - ship2:position):mag.
  local Tbest is 0.
  local dt is (Tmax - Tmin) / 32.

  local T is Tmin.
  until T >= Tmax {
    local X is (POSITIONAT(ship1, T)) - (POSITIONAT(ship2, T)).
    if X:mag < Rbest {
      set Rbest to X:mag.
    }
    set T to T + dt.
  }

  return Rbest.
}
