// Transfer to target

RUNONCEPATH("/core/lib_warp").

function mainframeTransfer {
  LOCAL currentSituation IS ORBIT:TRANSITION.
  until ORBIT:TRANSITION <> currentSituation {
    warpSeconds(ETA:TRANSITION + 1).
  }
}
