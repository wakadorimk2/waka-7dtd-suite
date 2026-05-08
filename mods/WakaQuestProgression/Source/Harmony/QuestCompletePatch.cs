using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WakaQuestProgression
{
    /// <summary>
    /// Hooks vanilla Quest completion. Two responsibilities:
    ///
    ///  (1) Scourge Beacon tier-aware quests (quest_scourge_infestation_tN, N=1..6):
    ///      fire POI Scourge's `psc_addtokens &lt;tier*2&gt;` console command on
    ///      Completed end state to grant Scourge Tokens proportional to POI tier.
    ///
    ///  (2) Quest Revamp super/ultra/nightmare quests (tier?_clear_superinfested,
    ///      tier?_clear_ultrainfested, tier?_clear_nightmare): apply a bonus
    ///      QuestFactionPoint via the public method
    ///      QuestJournal.AddQuestFactionPoint(faction, +alpha), in addition to
    ///      vanilla's `+DifficultyTier` baseline. This boosts trader-tier
    ///      progression for the harder variants without touching DifficultyTier
    ///      itself (so trader-tier gating in EntityTrader is untouched).
    ///
    ///        super       -> +1 alpha
    ///        ultra       -> +2 alpha
    ///        nightmare   -> +2 alpha
    ///
    /// Vanilla trader tier counter (`add_to_tier_complete=true` pipeline) flows
    /// through QuestJournal.AddQuestFactionPoint which is public, so we call it
    /// directly via Quest.OwnerJournal — no reflection needed.
    /// </summary>
    public static class QuestCompletePatch
    {
        const string ScourgeTierIdPrefix = "quest_scourge_infestation_t";
        const string SuperSuffix = "_clear_superinfested";
        const string UltraSuffix = "_clear_ultrainfested";
        const string NightmareSuffix = "_clear_nightmare";

        const int SuperBonus = 1;
        const int UltraBonus = 2;
        const int NightmareBonus = 2;

        public static Type QuestType;
        public static FieldInfo QuestIdField;
        public static PropertyInfo QuestIdProperty;
        public static MethodInfo TargetMethod;
        public static bool Ready;

        public static void Initialize()
        {
            try
            {
                QuestType = AccessTools.TypeByName("Quest");
                if (QuestType == null)
                {
                    Log.Warning("[WakaQuestProgression] vanilla Quest type not found; QuestCompletePatch disabled");
                    return;
                }

                QuestIdField = AccessTools.Field(QuestType, "ID")
                              ?? AccessTools.Field(QuestType, "id")
                              ?? AccessTools.Field(QuestType, "QuestCode");
                QuestIdProperty = AccessTools.Property(QuestType, "ID")
                                  ?? AccessTools.Property(QuestType, "id")
                                  ?? AccessTools.Property(QuestType, "QuestCode");

                TargetMethod = AccessTools.Method(QuestType, "CloseQuest")
                              ?? AccessTools.Method(QuestType, "HandleEnd")
                              ?? AccessTools.Method(QuestType, "OnComplete")
                              ?? AccessTools.Method(QuestType, "OnFinished")
                              ?? AccessTools.Method(QuestType, "TurnIn")
                              ?? AccessTools.Method(QuestType, "Complete");

                if (TargetMethod == null || (QuestIdField == null && QuestIdProperty == null))
                {
                    Log.Warning("[WakaQuestProgression] Required Quest member not found; QuestCompletePatch disabled");
                    return;
                }

                Ready = true;
                Log.Out($"[WakaQuestProgression] QuestCompletePatch armed on Quest.{TargetMethod.Name}() (id via {(QuestIdField != null ? "field " + QuestIdField.Name : "property " + QuestIdProperty.Name)})");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaQuestProgression] QuestCompletePatch.Initialize failed: {e}");
            }
        }

        static string ReadId(object questInstance)
        {
            string viaProperty = null, viaField = null;
            try { if (QuestIdProperty != null) viaProperty = QuestIdProperty.GetValue(questInstance) as string; } catch { }
            try { if (QuestIdField != null) viaField = QuestIdField.GetValue(questInstance) as string; } catch { }
            if (!string.IsNullOrEmpty(viaProperty)) return viaProperty;
            return viaField;
        }

        public static void OnQuestEnded(object __instance, object[] __args)
        {
            if (!Ready || __instance == null) return;
            try
            {
                var stateArg = (__args != null && __args.Length >= 1 && __args[0] != null) ? __args[0].ToString() : "<null>";
                if (!string.Equals(stateArg, "Completed", StringComparison.OrdinalIgnoreCase)) return;

                var id = ReadId(__instance);
                if (string.IsNullOrEmpty(id)) return;

                // (1) Scourge Beacon tier-aware quests -> psc_addtokens
                if (id.StartsWith(ScourgeTierIdPrefix, StringComparison.Ordinal))
                {
                    var tail = id.Substring(ScourgeTierIdPrefix.Length);
                    if (int.TryParse(tail, out int tier) && tier >= 1 && tier <= 6)
                    {
                        int tokens = tier * 2;
                        var console = SdtdConsole.Instance;
                        if (console != null)
                        {
                            Log.Out($"[WakaQuestProgression] '{id}' completed -> psc_addtokens {tokens}");
                            console.ExecuteSync($"psc_addtokens {tokens}", null);
                        }
                        else
                        {
                            Log.Warning("[WakaQuestProgression] SdtdConsole.Instance null; cannot award tokens");
                        }
                    }
                    return;
                }

                // (2) Quest Revamp super/ultra/nightmare -> AddQuestFactionPoint bonus
                int bonus = 0;
                string label = null;
                if (id.EndsWith(SuperSuffix, StringComparison.Ordinal)) { bonus = SuperBonus; label = "super"; }
                else if (id.EndsWith(UltraSuffix, StringComparison.Ordinal)) { bonus = UltraBonus; label = "ultra"; }
                else if (id.EndsWith(NightmareSuffix, StringComparison.Ordinal)) { bonus = NightmareBonus; label = "nightmare"; }

                if (bonus > 0 && __instance is Quest q)
                {
                    var journal = q.OwnerJournal;
                    if (journal != null)
                    {
                        journal.AddQuestFactionPoint(q.QuestFaction, bonus);
                        Log.Out($"[WakaQuestProgression] '{id}' ({label}) bonus +{bonus} faction points (faction={q.QuestFaction})");
                    }
                    else
                    {
                        Log.Warning($"[WakaQuestProgression] '{id}' OwnerJournal null; cannot apply bonus");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaQuestProgression] OnQuestEnded failed: {e.Message}");
            }
        }
    }

    [HarmonyPatch]
    public static class QuestCompletePatchHarness
    {
        public static bool Prepare(MethodBase _) => QuestCompletePatch.Ready;

        public static IEnumerable<MethodBase> TargetMethods()
        {
            if (!QuestCompletePatch.Ready) yield break;
            yield return QuestCompletePatch.TargetMethod;
        }

        public static void Postfix(object __instance, object[] __args)
            => QuestCompletePatch.OnQuestEnded(__instance, __args);
    }
}
