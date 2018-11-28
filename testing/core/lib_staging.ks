
GLOBAL all_engines IS List().

LIST ENGINES IN all_engines.

function stagingCheck {
	LOCAL staging IS false.
	FOR eng IN all_engines {
		IF eng:IGNITION and eng:FLAMEOUT {
			SET staging TO true.
		}
	}
	IF staging {
		WAIT UNTIL STAGE:READY.
		STAGE.
		WAIT 0.5.
		LIST ENGINES IN all_engines.
	}

}

// calculate burn time for maneuver needing provided deltaV
function stagingBurnTimeForDv {
	parameter dv.
	// For now a rough estimate will sufice
	return SHIP:MASS * dv / SHIP:AVAILABLETHRUST.
}
