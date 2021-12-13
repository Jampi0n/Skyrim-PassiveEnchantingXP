Scriptname JEX_PassiveEnchantingXP extends activemagiceffect  

Actor Property PlayerRef Auto
GlobalVariable Property TotalEnchMag Auto
GlobalVariable Property XPFactor Auto
GlobalVariable Property JEX_MaxXpPerMinute Auto
GlobalVariable Property JEX_UpdateTimer Auto
GlobalVariable Property JEX_MaxPerBattle Auto
String skillAV = "Enchanting"
Int currentPerBattle = 0

Float Function AVtoXP(Float av)
	; xp should always be similar regardless of level, so xp gain is similar to xp gain from enchanting
	; stronger enchantments should give more xp to incentivize making useful enchantments with expensive soul gems over just using small gems for training only
	; however the xp gain should be capped so if you get powerful enchantments you don't level to quickly (e.g. custom enchantment from before making skill legendary)
	; introduce level dependent max av
	; max av: roughly 1000 - 15000
	int level = PlayerRef.GetActorValue(skillAV) as int
	float max = level * level * 1.5
	float ratio = av / max
	if ratio < 0
		ratio = 0
	elseif ratio > 1
		ratio = 1
	endif
	float rate = JEX_MaxXpPerMinute.GetValue()
	return rate * ratio * 60.0 / JEX_UpdateTimer.GetValue()
EndFunction

Event OnEffectStart(Actor akTarget, Actor akCaster)
	currentPerBattle = 0
	RegisterForSingleUpdate(JEX_UpdateTimer.GetValue())
EndEvent

Event OnUpdate()
	If currentPerBattle <= JEX_MaxPerBattle.GetValueInt()
		currentPerBattle += 1
		float xp = AVtoXP(TotalEnchMag.GetValue()) * 5
		if xp > 0
			Game.AdvanceSkill(skillAV, xp)
		endif
		RegisterForSingleUpdate(JEX_UpdateTimer.GetValue())
	EndIf
EndEvent
