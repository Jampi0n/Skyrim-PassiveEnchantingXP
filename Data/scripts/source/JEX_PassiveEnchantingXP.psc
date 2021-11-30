Scriptname JEX_PassiveEnchantingXP extends activemagiceffect  

Actor Property PlayerRef Auto
GlobalVariable Property TotalEnchMag Auto
GlobalVariable Property XPFactor Auto
Float Property UpdateRate Auto
String skillAV = "Enchanting"
Int maxPerBattle = 6
Int currentPerBattle = 0
String fileName = "../../../PassiveEnchantingXP.json"

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
	float rate = JsonUtil.GetFloatValue(fileName, "max_xp_per_minute", 0.2)
	if !rate || rate <= 0.0 ; fallback
		rate = 0.2
	endif
	return rate * ratio / 6
EndFunction

Event OnEffectStart(Actor akTarget, Actor akCaster)
	currentPerBattle = 0
	RegisterForSingleUpdate(UpdateRate)
EndEvent

Event OnUpdate()
	If currentPerBattle <= maxPerBattle
		currentPerBattle += 1
		float xp = AVtoXP(TotalEnchMag.GetValue()) * 5
		if xp > 0
			Game.AdvanceSkill(skillAV, xp)
		endif
		RegisterForSingleUpdate(UpdateRate)
	EndIf
EndEvent
