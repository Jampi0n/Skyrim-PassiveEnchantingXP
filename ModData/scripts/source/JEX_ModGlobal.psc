Scriptname JEX_ModGlobal extends activemagiceffect  

GlobalVariable Property GlobalToMod Auto

Float mag = -1.0

Event OnEffectStart(Actor akTarget, Actor akCaster)
	mag = GetMagnitude()
	GlobalToMod.Mod(mag)
EndEvent

Event OnEffectFinish(Actor akTarget, Actor akCaster)
	GlobalToMod.Mod(-mag)
EndEvent
