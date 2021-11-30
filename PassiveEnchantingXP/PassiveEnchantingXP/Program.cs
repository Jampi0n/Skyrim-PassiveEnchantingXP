using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Threading.Tasks;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;

namespace PassiveEnchantingXP {
    public class Program {
        const string PREFIX = "JEX_";
        const float MAGNITUDE = 1000.0f;
        public static async Task<int> Main(string[] args) {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "PassiveEnchantingXP.esp")
                .Run(args);
        }

        public static Quest createPlayerAlias(ISkyrimMod mod, string name) {
            var q = mod.Quests.AddNew();
            q.EditorID = name;
            q.Name = name;

            q.Aliases.Add(new QuestAlias {
                ID = 0,
                Name = name + "PlayerAlias"
            });
            q.Aliases.First().ForcedReference.SetTo(Constants.Player.FormKey);
            return q;
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) {
            var totalEnchMag = state.PatchMod.Globals.AddNewFloat();
            totalEnchMag.EditorID = PREFIX + "TotalEnchMag";
            var updateEffect = state.PatchMod.MagicEffects.AddNew(PREFIX + "UpdateEffect");
            updateEffect.Name = updateEffect.EditorID;
            updateEffect.BaseCost = 0;
            updateEffect.Flags =
                MagicEffect.Flag.Recover |
                MagicEffect.Flag.NoHitEvent |
                MagicEffect.Flag.NoDuration |
                MagicEffect.Flag.NoMagnitude |
                MagicEffect.Flag.NoArea |
                MagicEffect.Flag.HideInUI |
                MagicEffect.Flag.NoHitEffect;
            updateEffect.Archetype.ActorValue = ActorValue.None;
            updateEffect.Archetype.Type = MagicEffectArchetype.TypeEnum.Script;
            updateEffect.CastType = CastType.ConstantEffect;
            updateEffect.TargetType = TargetType.Self;

            updateEffect.Conditions.Add(new ConditionFloat {
                CompareOperator = CompareOperator.EqualTo,
                ComparisonValue = 1.0f,
                Data = new FunctionConditionData {
                    RunOnType = Condition.RunOnType.Subject,
                    Function = Condition.Function.GetIsID,
                    ParameterOneRecord = Skyrim.Npc.Player
                }
            })
            ;

            updateEffect.VirtualMachineAdapter = new VirtualMachineAdapter();
            var script = new ScriptEntry {
                Name = PREFIX + "PassiveEnchantingXP",
                Flags = ScriptEntry.Flag.Local,
            };
            script.Properties.Add(new ScriptObjectProperty {
                Name = "TotalEnchMag",
                Object = totalEnchMag.AsLink()
            });
            script.Properties.Add(new ScriptObjectProperty {
                Name = "PlayerRef",
                Object = Constants.Player.AsSetter()
            });
            script.Properties.Add(new ScriptFloatProperty {
                Name = "UpdateRate",
                Data = 10.0f
            });

            updateEffect.VirtualMachineAdapter.Scripts.Add(script);


            var enchEffect = state.PatchMod.MagicEffects.AddNew(PREFIX + "EnchEffect");
            enchEffect.Name = enchEffect.EditorID;
            enchEffect.BaseCost = 0;
            enchEffect.Flags =
                MagicEffect.Flag.Recover |
                MagicEffect.Flag.NoHitEvent |
                MagicEffect.Flag.NoDuration |
                MagicEffect.Flag.NoArea |
                MagicEffect.Flag.HideInUI |
                MagicEffect.Flag.NoHitEffect |
                MagicEffect.Flag.PowerAffectsMagnitude;
            enchEffect.Archetype.ActorValue = ActorValue.None;
            enchEffect.Archetype.Type = MagicEffectArchetype.TypeEnum.Script;
            enchEffect.CastType = CastType.ConstantEffect;
            enchEffect.TargetType = TargetType.Self;

            enchEffect.Conditions.Add(new ConditionFloat {
                CompareOperator = CompareOperator.EqualTo,
                ComparisonValue = 1.0f,
                Data = new FunctionConditionData {
                    RunOnType = Condition.RunOnType.Subject,
                    Function = Condition.Function.GetIsID,
                    ParameterOneRecord = Skyrim.Npc.Player
                },
            });

            enchEffect.VirtualMachineAdapter = new VirtualMachineAdapter();
            script = new ScriptEntry {
                Name = PREFIX + "ModGlobal",
                Flags = ScriptEntry.Flag.Local,
            };
            script.Properties.Add(new ScriptObjectProperty {
                Name = "GlobalToMod",
                Object = totalEnchMag.AsLink()
            });
            enchEffect.VirtualMachineAdapter.Scripts.Add(script);

            var passiveUpdateAbility = state.PatchMod.Spells.AddNew(PREFIX + "UpdateAbility");
            passiveUpdateAbility.Name = passiveUpdateAbility.EditorID;
            passiveUpdateAbility.Flags = SpellDataFlag.IgnoreResistance | SpellDataFlag.ManualCostCalc | SpellDataFlag.NoAbsorbOrReflect;
            passiveUpdateAbility.Type = SpellType.Ability;
            passiveUpdateAbility.CastType = CastType.ConstantEffect;
            passiveUpdateAbility.TargetType = TargetType.Self;
            passiveUpdateAbility.EquipmentType = Skyrim.EquipType.EitherHand.AsNullable();
            passiveUpdateAbility.MenuDisplayObject.SetTo(FormLink<IStaticGetter>.Null);
            var effect = new Effect();
            effect.BaseEffect.SetTo(updateEffect);

            effect.Conditions.Add(new ConditionFloat {
                CompareOperator = CompareOperator.EqualTo,
                ComparisonValue = 1.0f,
                Data = new FunctionConditionData {
                    RunOnType = Condition.RunOnType.Subject,
                    Function = Condition.Function.IsInCombat
                }
            });
            passiveUpdateAbility.Effects.Add(effect);

            var playerAliasQuest = createPlayerAlias(state.PatchMod, PREFIX + "Quest");
            playerAliasQuest.Aliases.First().Spells.Add(passiveUpdateAbility);
            playerAliasQuest.Flags = Quest.Flag.RunOnce | Quest.Flag.StartGameEnabled;

            var baseEnchantments = new HashSet<IObjectEffectGetter>();
            foreach(var armorGetter in state.LoadOrder.PriorityOrder.Armor().WinningOverrides()) {
                if(armorGetter.ObjectEffect.TryResolve(state.LinkCache, out var enchGetter)) {
                    if(enchGetter != null) {
                        var ench = (enchGetter as IObjectEffectGetter);
                        if(ench != null && (armorGetter.Keywords == null || !armorGetter.Keywords.Contains(Skyrim.Keyword.MagicDisallowEnchanting))) {
                            if(ench.BaseEnchantment.TryResolve(state.LinkCache, out var baseEnch)) {
                                if(baseEnch != null) {
                                    if(baseEnch.CastType == CastType.ConstantEffect && baseEnch.TargetType == TargetType.Self) {
                                        if(baseEnchantments.Add(baseEnch)) {
                                            Console.WriteLine("Patching base enchantment: " + baseEnch.EditorID);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach(var baseEnchGetter in baseEnchantments) {
                var baseEnch = state.PatchMod.ObjectEffects.GetOrAddAsOverride(baseEnchGetter);
                effect = new Effect {
                    Data = new EffectData {
                        Magnitude = MAGNITUDE,
                        Duration = 0,
                        Area = 0
                    }
                };
                effect.BaseEffect.SetTo(enchEffect);
                baseEnch.Effects.Add(effect);
            }
        }
    }
}
