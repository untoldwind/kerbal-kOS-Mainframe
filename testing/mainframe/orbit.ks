RUNONCEPATH("/core/lib_util").
RUNONCEPATH("/mainframe/exec_node").

function mainframeCircularize {
    uiConsole("MAINFRAME", "Circularize").
    utilRemoveNodes().
    ADD ADDONS:MainFrame:MANEUVERS:CIRCULARIZE.
    WAIT 0.

    mainframeExecNode().
}

function mainframeMatchPlanes {
    uiConsole("MAINFRAME", "Match planes").
    if not HASTARGET {
        uiError("MAINFRAME", "No target").
        return.
    }
    utilRemoveNodes().

    ADD ADDONS:MainFrame:MANEUVERS:MATCH_PLANES(TARGET:ORBIT).
    WAIT 0.

    mainframeExecNode().
}

function mainframeHohmann {
    uiConsole("MAINFRAME", "Hohmann").
    if not HASTARGET {
        uiError("MAINFRAME", "No target").
        return.
    }
    utilRemoveNodes().

    ADD ADDONS:MainFrame:MANEUVERS:HOHMANN_LAMBERT(TARGET:ORBIT, 0).
    WAIT 0.

    mainframeExecNode().
}

function mainframeMatchVelocities {
    uiConsole("MAINFRAME", "Match velocities").
    if not HASTARGET {
        uiError("MAINFRAME", "No target").
        return.
    }
    utilRemoveNodes().

    ADD ADDONS:MainFrame:MANEUVERS:MATCH_VELOCITIES(TARGET:ORBIT).
    WAIT 0.

    mainframeExecNode().
}

function mainframeCorrectTargetPeriapsis {
    parameter targetPeriapsis.

    uiConsole("MAINFRAME", "Correct Target Periapsis").
    if not HASTARGET or TARGET:TYPENAME <> "Body" {
        uiError("MAINFRAME", "No target body").
        return.
    }
    utilRemoveNodes().

    ADD ADDONS:MainFrame:MANEUVERS:CHEAPEST_CORRECTION_BODY(TARGET, targetPeriapsis + TARGET:RADIUS).
    WAIT 0.

    mainframeExecNode().
}

function mainframeChangeApoapsis {
    parameter targetApoapsis.

    utilRemoveNodes().

    ADD ADDONS:MainFrame:MANEUVERS:CHANGE_APOAPSIS(TIME + 20, BODY:RADIUS + targetApoapsis).
    WAIT 0.

    mainframeExecNode().
}

function mainframeChangePeriapsis {
    parameter targetPeriapsis.

    utilRemoveNodes().

    ADD ADDONS:MainFrame:MANEUVERS:CHANGE_PERIAPSIS(TIME + 20, BODY:RADIUS + targetPeriapsis).
    WAIT 0.

    mainframeExecNode().
}

function mainframeReturnFromMoon {
    parameter targetPeriapsis.

    utilRemoveNodes().

    ADD ADDONS:MainFrame:MANEUVERS:RETURN_FROM_MOON(targetPeriapsis).
    WAIT 0.

    mainframeExecNode().
}