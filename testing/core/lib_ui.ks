GLOBAL ui_debug is true. 
GLOBAL logconsole is false.

global ui_debugNode is true. // Explain node planning
global ui_debugAxes is false. // Explain 3-axis navigation e.g. docking

global ui_DebugStb is vecdraw(v(0,0,0), v(0,0,0), GREEN, "Stb", 1, false).
global ui_DebugUp is vecdraw(v(0,0,0), v(0,0,0), BLUE, "Up", 1, false).
global ui_DebugFwd is vecdraw(v(0,0,0), v(0,0,0), RED, "Fwd", 1, false).

global ui_myPort is vecdraw(v(0,0,0), v(0,0,0), YELLOW, "Ship", 1, false).
global ui_hisPort is vecdraw(v(0,0,0), v(0,0,0), PURPLE, "Dock", 1, false).

function uiConsole {
  parameter prefix.
  parameter msg.

  LOCAL logtext is "T+" + round(time:seconds) + " " + prefix + ": " + msg.
  PRINT logtext.

  if logconsole {
    LOG logtext to "log.txt".
    IF HOMECONNECTION:ISCONNECTED {
      COPYPATH("log.txt","0:/logs/"+SHIP:NAME+".txt").
    }
  }
}

function uiWarning {
  parameter prefix.
  parameter msg.

  uiConsole(prefix, msg).
  HUDTEXT(msg, 10, 4, 36, YELLOW, false).
  uiAlarm().
}

function uiError {
  parameter prefix.
  parameter msg.

  uiConsole(prefix, msg).
  HUDTEXT(msg, 10, 4, 36, RED, false).
  uiAlarm().
}

function uiFatal {
  parameter prefix.
  parameter message.

  uiError(prefix, message + " - RESUME CONTROL").
  wait 3.
  reboot.
}

FUNCTION uiAlarm {
    local vAlarm TO GetVoice(0).
    set vAlarm:wave to "TRIANGLE".
    set vAlarm:volume to 0.5.
      vAlarm:PLAY(
          LIST(
              NOTE("A#4", 0.2,  0.25), 
              NOTE("A4",  0.2,  0.25), 
              NOTE("A#4", 0.2,  0.25), 
              NOTE("A4",  0.2,  0.25),
              NOTE("R",   0.2,  0.25),
              NOTE("A#4", 0.2,  0.25), 
              NOTE("A4",  0.2,  0.25), 
              NOTE("A#4", 0.2,  0.25), 
              NOTE("A4",  0.2,  0.25)
          )
      ).
}

function uiDebug {
  parameter msg.

  if ui_debug {
    uiConsole("Debug", msg).
    hudtext(msg, 3, 3, 24, WHITE, false).
  }
}

function uiShowPorts {
  parameter myPort.
  parameter hisPort.
  parameter dist.
  parameter ready.

  if myPort <> 0 {
    set ui_myPort:start to myPort:position.
    set ui_myPort:vec to myPort:portfacing:vector*dist.
    if ready {
      set ui_myPort:color to GREEN.
    } else {
      set ui_myPort:color to RED.
    }
    set ui_myPort:show to true.
  } else {
    set ui_myPort:show to false.
  }

  if hisPort <> 0 {
    set ui_hisPort:start to hisPort:position.
    set ui_hisPort:vec to hisPort:portfacing:vector*dist.
    set ui_hisPort:show to true.
  } else {
    set ui_hisPort:show to false.
  }
}

function uiDebugAxes {
  parameter origin.
  parameter dir.
  parameter length.

  if ui_debugAxes = true {
    if length:x <> 0 {
      set ui_DebugStb:start to origin.
      set ui_DebugStb:vec to dir:starvector*length:x.
      set ui_DebugStb:show to true.
    } else {
      set ui_DebugStb:show to false.
    }

    if length:y <> 0 {
      set ui_DebugUp:start to origin.
      set ui_DebugUp:vec to dir:upvector*length:y.
      set ui_DebugUp:show to true.
    } else {
      set ui_DebugUp:show to false.
    }

    if length:z <> 0 {
      set ui_DebugFwd:start to origin.
      set ui_DebugFwd:vec to dir:vector*length:z.
      set ui_DebugFwd:show to true.
    } else {
      set ui_DebugFwd:show to false.
    }
  }
}
