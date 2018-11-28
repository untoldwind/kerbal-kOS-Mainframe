// Execute a maneuver node, warping if necessary to save time.

RUNONCEPATH("/core/lib_util").
RUNONCEPATH("/core/lib_warp").
RUNONCEPATH("/core/lib_staging").
RUNONCEPATH("/core/lib_ui").

function mainframeExecNode {
    uiConsole("MAINFRAME", "Exec Node").

	// Configuration constants; these are pre-set for automated missions; if you
	// have a ship that turns poorly, you may need to decrease these and perform
	// manual corrections.
	if not (defined node_bestFacing) {
		GLOBAL node_bestFacing is 5.   // ~5  degrees error (10 degree cone)
	}
	if not (defined node_okFacing) {
		GLOBAL node_okFacing   is 20.  // ~20 degrees error (40 degree cone)
	}

	if not hasNode {
		uiFatal("Node", "No node to execute").
	}

	LOCAL nn IS NEXTNODE.

	rcs off.
	sas off.
	LOCK steerDir to LOOKDIRUP(nn:DELTAV, POSITIONAT(SHIP, TIME:SECONDS + nn:ETA) - BODY:POSITION).
	LOCK STEERING to steerDir.

	LOCAL burnTime IS addons:mainframe:info:burn_time(nn:DELTAV:MAG, 1).
	LOCAL dt IS burnTime:HALF:SECONDS.

	// If have time, wait to ship almost align with maneuver node.
	// If have little time, wait at least to ship face in general direction of node
	// This prevents backwards burns, but still allows steering via engine thrust.
	// If ship is not rotating for some reason, will proceed anyway. (Maybe only torque source is engine gimbal?)
	LOCAL warped IS false.
	UNTIL utilIsShipFacing(steerDir, node_bestFacing, 0.5) or
		nn:ETA <= dt and utilIsShipFacing(steerDir, node_okFacing, 5)
	{
		IF SHIP:angularvel:mag < 0.01 RCS on.
		IF not warped { set warped to true. physWarp(1). }
		WAIT 0.
	}
	if warped resetWarp().

	warpSeconds(nn:eta - dt - 10).

	LOCAL dv0 is nn:DELTAV.
	LOCAL dvMin is dv0:mag.
	LOCAL minThrottle is 0.
	LOCAL maxThrottle is 0.
	LOCK THROTTLE to MIN(maxThrottle,MAX(minThrottle,MIN(dvMin,nn:DELTAV:mag) * SHIP:MASS/MAX(1,SHIP:AVAILABLETHRUST))).
	LOCK steerDir to LOOKDIRUP(nn:DELTAV, SHIP:POSITION - BODY:POSITION).

	LOCAL almostThere to 0.
	LOCAL choked to 0.
	LOCAL warned to false.

	if nn:ETA - dt > 5 {
		physWarp(1).
		WAIT UNTIL nn:ETA - dt <= 2.
		resetWarp().
	}
	WAIT UNTIL nn:ETA - dt <= 1.

	UNTIL dvMin < 0.05
	{
		IF stagingCheck() uiWarning("Node", "Stage " + stage:number + " separation during burn").
		wait 0. //Let a physics tick run each loop.

		local dv is nn:DELTAV:mag.
		IF dv < dvMin set dvMin to dv.

		IF SHIP:AVAILABLETHRUST > 0 {
			IF utilIsShipFacing(steerDir,node_okFacing,2) {
				set minThrottle to 0.01.
				set maxThrottle to 1.
			} else {
				// we are not facing correctly! cut back thrust to 10% so gimbaled
				// engine will push us back on course
				set minThrottle to 0.1.
				set maxThrottle to 0.1.
				rcs on.
			}
			IF vdot(dv0, nn:DELTAV) < 0 break.	// overshot (node delta vee is pointing opposite from initial)
			IF dv > dvMin + 0.1 break.			// burn DV increases (off target due to wobbles)
			IF dv <= 0.2 {						// burn DV gets too small for main engines to cope with
				IF almostThere = 0 set almostThere to time:seconds.
				IF time:seconds-almostThere > 5 break.
				IF dv <= 0.05 break.
			}
			set choked to 0.
		} else {
			IF choked = 0 set choked to time:seconds.
			IF not warned and time:seconds-choked > 3 {
				set warned to true.
				uiWarn("Node", "No acceleration").
			}
			IF time:seconds-choked > 30
				uiFatal("Node", "No acceleration").
		}
	}

	UNLOCK all.
	SET ship:control:pilotMainThrottle to 0.

	REMOVE nn.
}