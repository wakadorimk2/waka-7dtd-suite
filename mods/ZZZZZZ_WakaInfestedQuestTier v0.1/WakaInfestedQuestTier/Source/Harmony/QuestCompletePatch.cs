using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WakaInfestedQuestTier
{
    /// <summary>
    /// Hooks vanilla Quest completion. When a quest_scourge_infestation_tN
    /// finishes, fires POI Scourge's `psc_addtokens &lt;tier*2&gt;` to grant
    /// Scourge Tokens (Exchange Station currency) proportional to POI tier.
    ///
    /// Scope (intentional):
    ///   - Scourge Tokens grant: yes
    ///   - Vanilla XP / item rewards: handled by quests.xml itself
    ///   - Vanilla trader tier counter (`add_to_tier_complete=true` pipeline):
    ///     NOT touched. Beacon-started quests have QuestGiverID=-1 because
    ///     Beacon bypasses the trader-accept pipeline that the counter is
    ///     gated on. The counter's storage location (Dict<int,byte> living
    ///     somewhere on the player/QuestJournal/PlayerData hierarchy) is not
    ///     part of TFP's public modding surface; reaching into it requires
    ///     dnSpy decompilation rather than blind reflection. Out of scope.
    ///
    ///   The XML attribute `add_to_tier_complete="true"` is kept on each
    ///   tier-N quest so that, if a future redesign routes Beacon through
    ///   the trader-accept path, the counter integration becomes automatic
    ///   without C# changes here.
    /// </summary>
    public static class QuestCompletePatch
    {
        const string TierIdPrefix = "quest_scourge_infestation_t";

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
                    Log.Warning("[WakaInfestedQuestTier] vanilla Quest type not found; tokens hook disabled");
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
                    Log.Warning("[WakaInfestedQuestTier] Required Quest member not found; tokens hook disabled");
                    return;
                }

                Ready = true;
                Log.Out($"[WakaInfestedQuestTier] Tokens hook armed on Quest.{TargetMethod.Name}() (id via {(QuestIdField != null ? "field " + QuestIdField.Name : "property " + QuestIdProperty.Name)})");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaInfestedQuestTier] QuestCompletePatch.Initialize failed: {e}");
            }
        }

        static string ReadId(object questInstance)
        {
            string viaProperty = null, viaField = null;
            try { if (QuestIdProperty != null) viaProperty = QuestIdProperty.GetValue(questInstance) as string; } catch { }
            try { if (QuestIdField != null) viaField = QuestIdField.GetValue(questInstance) as string; } catch { }
            if (!string.IsNullOrEmpty(viaProperty) && viaProperty.StartsWith(TierIdPrefix, StringComparison.Ordinal)) return viaProperty;
            if (!string.IsNullOrEmpty(viaField) && viaField.StartsWith(TierIdPrefix, StringComparison.Ordinal)) return viaField;
            return viaProperty ?? viaField;
        }

        public static void OnQuestEnded(object __instance, object[] __args)
        {
            if (!Ready || __instance == null) return;
            try
            {
                var stateArg = (__args != null && __args.Length >= 1 && __args[0] != null) ? __args[0].ToString() : "<null>";
                if (!string.Equals(stateArg, "Completed", StringComparison.OrdinalIgnoreCase)) return;

                var id = ReadId(__instance);
                if (string.IsNullOrEmpty(id) || !id.StartsWith(TierIdPrefix, StringComparison.Ordinal)) return;

                var tail = id.Substring(TierIdPrefix.Length);
                if (!int.TryParse(tail, out int tier)) return;
                if (tier < 1 || tier > 6) return;

                int tokens = tier * 2;
                var console = SdtdConsole.Instance;
                if (console != null)
                {
                    Log.Out($"[WakaInfestedQuestTier] Quest '{id}' completed -> psc_addtokens {tokens}");
                    console.ExecuteSync($"psc_addtokens {tokens}", null);
                }
                else
                {
                    Log.Warning("[WakaInfestedQuestTier] SdtdConsole.Instance null; cannot award tokens");
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaInfestedQuestTier] OnQuestEnded failed: {e.Message}");
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
