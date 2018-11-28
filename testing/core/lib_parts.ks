// events are preferred because there are no restrictions
function partsDoEvent {
	parameter module.
	parameter event.
	parameter tag is "".
	
	set event to "^"+event+"\b". // match first word
	local success is false.
	for p in ship:partsTagged(tag) {
		if p:modules:contains(module) {
			local m is p:getModule(module).
			for e in m:allEventNames() {
				if e:matchesPattern(event) {
					m:doEvent(e).
					set success to true.
				}
			}
		}
	}
	return success.
}

function partsExtendSolarPanels {
	parameter tag is "".
	if not partsDoEvent("ModuleDeployableSolarPanel", "extend", tag) {
		return partsDoEvent("KopernicusSolarPanel", "extend", tag).
	}
	return false.
}

function partsRetractSolarPanels {
	parameter tag is "".
	if not partsDoEvent("ModuleDeployableSolarPanel", "retract", tag) {
		return partsDoEvent("KopernicusSolarPanel", "retract", tag).
	}
	return false.
}

function partsExtendAntennas {
	parameter tag is "".
	return partsDoEvent("ModuleDeployableAntenna", "extend", tag).
}

function partsRetractAntennas {
	parameter tag is "".
	return partsDoEvent("ModuleDeployableAntenna", "Retract", tag).
}

function partsControlFromDockingPort {
	parameter cPart. //The docking port you want to control from.
	local success is false.

	// Try to control from the port
	if cPart:modules:contains("ModuleDockingNode") {
			local m is cPart:getModule("ModuleDockingNode").
			for Event in m:allEventNames() {
					if Event:contains("Control") { m:DOEVENT(Event). success on. }
			}.
	}

	// Try to open/deploy the port
	if cPart:modules:contains("ModuleAnimateGeneric") {
			local m is cPart:getModule("ModuleAnimateGeneric").
			for Event in m:allEventNames() {
					if Event:contains("open") or Event:contains("deploy") or Event:contains("extend") { m:DOEVENT(Event). }
			}.
	}

	Return success.
}

