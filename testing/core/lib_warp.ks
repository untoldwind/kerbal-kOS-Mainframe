// Warp helpers

// Reset warp to 0 (i.e. no warping)
function resetWarp {
	kUniverse:TIMEWARP:CANCELWARP().
	SET WARP to 0.
	wait 0.
	wait until kUniverse:TIMEWARP:ISSETTLED.
	SET WARPMODE to "RAILS".
	wait until kUniverse:TIMEWARP:ISSETTLED.
}

// Set rails warp factor.
//
// Parameters: Warp factor
function railsWarp {
	parameter w.
	IF WARPMODE <> "RAILS"
		resetWarp().
	SET WARP to w.
}

// Set physics warp factor.
//
// Parameters: Warp factor
function physWarp {
	parameter w.
	IF WARPMODE <> "PHYSICS" {
		kUniverse:TIMEWARP:CANCELWARP().
		wait until kUniverse:TIMEWARP:ISSETTLED.
		SET WARPMODE to "PHYSICS".
	}
	SET WARP to w.
}

// Warp a number of seconds.
//
// Parameters: Number of seconds to warp.
function warpSeconds {
	parameter seconds.
	IF seconds <= 1 return 0.
	local t1 is TIME:SECONDS + seconds.
	until TIME:SECONDS >= t1-1 {
		resetWarp().
		IF TIME:SECONDS < t1-10 {
			kUniverse:TIMEWARP:WARPTO(t1).
			wait 1.
			wait until TIME:SECONDS >= t1-1 or (WARP = 0 and kUniverse:TIMEWARP:ISSETTLED).
		} else
		{// warpTo will not WARP 10 seconds and less
			IF TIME:SECONDS < t1-3 {
				physWarp(4).
				wait until TIME:SECONDS >= t1-3.
			}
			IF TIME:SECONDS < t1-2 {
				physWarp(3).
				wait until TIME:SECONDS >= t1-2.
			}
			IF TIME:SECONDS < t1-1 {
				physWarp(2).
				wait until TIME:SECONDS >= t1-1.
			}
			resetWarp().
			break.
		}
	}
	resetWarp().
	wait until TIME:SECONDS >= t1.
	return seconds.
}
