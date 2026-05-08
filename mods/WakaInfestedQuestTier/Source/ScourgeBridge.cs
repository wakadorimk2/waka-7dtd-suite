using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace WakaInfestedQuestTier
{
    /// <summary>
    /// Resolves POI Scourge's ItemActionStartScourgeQuest type and the location
    /// where the QuestID string is held. POI Scourge does not declare its own
    /// QuestID field; it stores it in ItemAction.Properties (a DynamicProperties
    /// instance) and reads it on each execute. We therefore write through a
    /// reflection-built accessor over DynamicProperties.
    /// </summary>
    public static class ScourgeBridge
    {
        const string TargetTypeName = "POIScourge.ItemActionStartScourgeQuest";
        const string PropertiesFieldName = "Properties";
        const string QuestIDKey = "QuestID";

        public static Type ItemActionType;
        public static FieldInfo PropertiesField;     // ItemAction.Properties
        public static Type DynamicPropertiesType;

        // The DynamicProperties internal storage. Populated by reflection probing.
        // Most likely shape: Dictionary<string, DynamicProperties.Property>
        // or              : Dictionary<string, string>
        public static FieldInfo ValuesField;
        public static MethodInfo ValuesIndexerGet;
        public static MethodInfo ValuesIndexerSet;
        public static MethodInfo ValuesContainsKey;
        public static Type ValueEntryType;
        public static FieldInfo ValueEntryStringField;     // if entry is a struct/class wrapping a string
        public static PropertyInfo ValueEntryStringProperty;

        // Optional helper methods on DynamicProperties (e.g. GetString/SetString)
        public static MethodInfo DpGetStringMethod;       // string GetString(string)
        public static MethodInfo DpSetStringMethod;       // void Set(string, string) or similar

        public static bool Ready;

        public static void Initialize()
        {
            try
            {
                ItemActionType = AccessTools.TypeByName(TargetTypeName);
                if (ItemActionType == null)
                {
                    Log.Warning($"[WakaInfestedQuestTier] {TargetTypeName} not found; tier redirect disabled");
                    return;
                }

                DumpTypeMembers(ItemActionType, "Target");

                PropertiesField = FindFieldByName(ItemActionType, PropertiesFieldName);
                if (PropertiesField == null)
                {
                    Log.Warning($"[WakaInfestedQuestTier] {ItemActionType.FullName}.Properties field not found; redirect disabled");
                    return;
                }

                DynamicPropertiesType = PropertiesField.FieldType;
                Log.Out($"[WakaInfestedQuestTier] Properties field resolved: type={DynamicPropertiesType.FullName}");
                DumpTypeMembers(DynamicPropertiesType, "DynamicProperties");

                // Try GetString / Set string-based helpers first
                DpGetStringMethod = FindMethod(DynamicPropertiesType, "GetString", new[] { typeof(string) }, typeof(string));
                if (DpGetStringMethod != null)
                    Log.Out($"[WakaInfestedQuestTier] Found {DynamicPropertiesType.FullName}.GetString(string)->string");

                // Common writer signatures (7DTD's DynamicProperties uses SetValue)
                DpSetStringMethod = FindMethod(DynamicPropertiesType, "SetValue", new[] { typeof(string), typeof(string) }, typeof(void))
                    ?? FindMethod(DynamicPropertiesType, "Set", new[] { typeof(string), typeof(string) }, typeof(void))
                    ?? FindMethod(DynamicPropertiesType, "SetString", new[] { typeof(string), typeof(string) }, typeof(void))
                    ?? FindMethod(DynamicPropertiesType, "Add", new[] { typeof(string), typeof(string) }, typeof(void));
                if (DpSetStringMethod != null)
                    Log.Out($"[WakaInfestedQuestTier] Found writer: {DynamicPropertiesType.FullName}.{DpSetStringMethod.Name}(string,string)->void");

                // Probe for a Values dictionary
                ValuesField = FindDictionaryField(DynamicPropertiesType, typeof(string));
                if (ValuesField != null)
                {
                    var dt = ValuesField.FieldType;
                    var args = dt.GetGenericArguments();
                    Log.Out($"[WakaInfestedQuestTier] Found dictionary {DynamicPropertiesType.FullName}.{ValuesField.Name} ({dt.FullName})");
                    ValuesIndexerGet = AccessTools.PropertyGetter(dt, "Item");
                    ValuesIndexerSet = AccessTools.PropertySetter(dt, "Item");
                    ValuesContainsKey = AccessTools.Method(dt, "ContainsKey", new[] { typeof(string) });
                    ValueEntryType = args[1];
                    if (ValueEntryType != typeof(string))
                    {
                        Log.Out($"[WakaInfestedQuestTier] Dictionary value type is {ValueEntryType.FullName}; probing for string member");
                        DumpTypeMembers(ValueEntryType, "DictValue");
                        ValueEntryStringField = FindFieldByType(ValueEntryType, typeof(string)) ?? FindFieldByName(ValueEntryType, "Value");
                        ValueEntryStringProperty = FindStringPropertyByName(ValueEntryType, "Value")
                                                   ?? FindStringPropertyAny(ValueEntryType);
                        if (ValueEntryStringField != null)
                            Log.Out($"[WakaInfestedQuestTier] Entry string field: {ValueEntryStringField.Name}");
                        if (ValueEntryStringProperty != null)
                            Log.Out($"[WakaInfestedQuestTier] Entry string property: {ValueEntryStringProperty.Name}");
                    }
                }

                // Decide if we have at least one viable read+write path
                bool canRead = DpGetStringMethod != null || ValuesField != null;
                bool canWrite = DpSetStringMethod != null
                                || (ValuesField != null && ValuesIndexerSet != null
                                    && (ValueEntryType == typeof(string)
                                        || ValueEntryStringField != null
                                        || (ValueEntryStringProperty != null && ValueEntryStringProperty.CanWrite)));
                if (canRead && canWrite)
                {
                    Ready = true;
                    Log.Out("[WakaInfestedQuestTier] Tier redirect armed");
                }
                else
                {
                    Log.Warning($"[WakaInfestedQuestTier] Could not assemble read+write path on DynamicProperties (canRead={canRead}, canWrite={canWrite}); redirect disabled");
                }
            }
            catch (Exception e)
            {
                Log.Error($"[WakaInfestedQuestTier] Initialize failed: {e}");
            }
        }

        public static string ReadQuestId(object actionInstance)
        {
            try
            {
                var props = PropertiesField?.GetValue(actionInstance);
                if (props == null) return null;
                if (DpGetStringMethod != null)
                {
                    return DpGetStringMethod.Invoke(props, new object[] { QuestIDKey }) as string;
                }
                if (ValuesField != null)
                {
                    var dict = ValuesField.GetValue(props);
                    if (dict == null) return null;
                    if (ValuesContainsKey != null && !(bool)ValuesContainsKey.Invoke(dict, new object[] { QuestIDKey })) return null;
                    var entry = ValuesIndexerGet.Invoke(dict, new object[] { QuestIDKey });
                    if (entry == null) return null;
                    if (ValueEntryType == typeof(string)) return entry as string;
                    if (ValueEntryStringField != null) return ValueEntryStringField.GetValue(entry) as string;
                    if (ValueEntryStringProperty != null) return ValueEntryStringProperty.GetValue(entry) as string;
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaInfestedQuestTier] ReadQuestId failed: {e.Message}");
            }
            return null;
        }

        public static bool WriteQuestId(object actionInstance, string value)
        {
            try
            {
                var props = PropertiesField?.GetValue(actionInstance);
                if (props == null) return false;
                if (DpSetStringMethod != null)
                {
                    DpSetStringMethod.Invoke(props, new object[] { QuestIDKey, value });
                    return true;
                }
                if (ValuesField != null && ValuesIndexerSet != null)
                {
                    var dict = ValuesField.GetValue(props);
                    if (dict == null) return false;
                    if (ValueEntryType == typeof(string))
                    {
                        ValuesIndexerSet.Invoke(dict, new object[] { QuestIDKey, value });
                        return true;
                    }
                    // Need to mutate entry's string slot
                    var entry = ValuesIndexerGet?.Invoke(dict, new object[] { QuestIDKey });
                    if (entry == null) return false;
                    if (ValueEntryStringField != null)
                    {
                        ValueEntryStringField.SetValue(entry, value);
                        // For value-types, the modified copy must be written back into the dictionary.
                        if (ValueEntryType.IsValueType)
                            ValuesIndexerSet.Invoke(dict, new object[] { QuestIDKey, entry });
                        return true;
                    }
                    if (ValueEntryStringProperty != null && ValueEntryStringProperty.CanWrite)
                    {
                        ValueEntryStringProperty.SetValue(entry, value);
                        if (ValueEntryType.IsValueType)
                            ValuesIndexerSet.Invoke(dict, new object[] { QuestIDKey, entry });
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaInfestedQuestTier] WriteQuestId failed: {e.Message}");
            }
            return false;
        }

        // --- reflection helpers ---

        static FieldInfo FindFieldByName(Type t, string name)
        {
            const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
            {
                var f = cur.GetField(name, bf | BindingFlags.DeclaredOnly);
                if (f != null) return f;
            }
            return null;
        }

        static FieldInfo FindFieldByType(Type t, Type fieldType)
        {
            const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
            {
                foreach (var f in cur.GetFields(bf))
                    if (f.FieldType == fieldType) return f;
            }
            return null;
        }

        static PropertyInfo FindStringPropertyByName(Type t, string name)
        {
            const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
            {
                foreach (var p in cur.GetProperties(bf))
                {
                    if (p.PropertyType != typeof(string)) continue;
                    if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) return p;
                }
            }
            return null;
        }

        static PropertyInfo FindStringPropertyAny(Type t)
        {
            const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
            {
                foreach (var p in cur.GetProperties(bf))
                    if (p.PropertyType == typeof(string)) return p;
            }
            return null;
        }

        static FieldInfo FindDictionaryField(Type t, Type keyType)
        {
            const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
            {
                foreach (var f in cur.GetFields(bf))
                {
                    var ft = f.FieldType;
                    if (!ft.IsGenericType) continue;
                    if (ft.GetGenericTypeDefinition() != typeof(Dictionary<,>)) continue;
                    var args = ft.GetGenericArguments();
                    if (args.Length == 2 && args[0] == keyType) return f;
                }
            }
            return null;
        }

        static MethodInfo FindMethod(Type t, string name, Type[] paramTypes, Type returnType)
        {
            try
            {
                var m = AccessTools.Method(t, name, paramTypes);
                if (m != null && m.ReturnType == returnType) return m;
            }
            catch { }
            return null;
        }

        static void DumpTypeMembers(Type t, string label)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[WakaInfestedQuestTier] === Member dump ({label}) for {t.FullName} ===");
                const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
                {
                    sb.AppendLine($"  [{cur.FullName}]");
                    foreach (var f in cur.GetFields(bf))
                        sb.AppendLine($"    F  {f.FieldType.FullName} {f.Name}");
                    foreach (var p in cur.GetProperties(bf))
                        sb.AppendLine($"    P  {p.PropertyType.FullName} {p.Name} {(p.CanRead ? "get" : "")}{(p.CanWrite ? "set" : "")}");
                    foreach (var m in cur.GetMethods(bf))
                    {
                        var ps = m.GetParameters();
                        var paramStr = string.Join(",", System.Linq.Enumerable.Select(ps, p => p.ParameterType.Name));
                        sb.AppendLine($"    M  {m.ReturnType.Name} {m.Name}({paramStr})");
                    }
                }
                Log.Out(sb.ToString());
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaInfestedQuestTier] DumpTypeMembers failed: {e.Message}");
            }
        }

        /// <summary>
        /// Returns the POI difficulty tier (1..6) at the player's current
        /// position, or -1 if outside any POI / detection fails.
        /// </summary>
        public static int GetPlayerPOITier(EntityPlayerLocal player)
        {
            if (player == null) return -1;
            try
            {
                var world = GameManager.Instance?.World;
                if (world == null) return -1;

                var pos = new Vector3i(
                    Mathf.FloorToInt(player.position.x),
                    Mathf.FloorToInt(player.position.y),
                    Mathf.FloorToInt(player.position.z));

                var dpd = world.ChunkClusters?[0]?.ChunkProvider?.GetDynamicPrefabDecorator();
                if (dpd == null)
                {
                    Log.Out("[WakaInfestedQuestTier] DynamicPrefabDecorator unavailable");
                    return -1;
                }

                var instance = dpd.GetPrefabAtPosition(pos);
                if (instance == null)
                {
                    Log.Out($"[WakaInfestedQuestTier] No POI at {pos}");
                    return -1;
                }

                int tier = instance.prefab.DifficultyTier;
                Log.Out($"[WakaInfestedQuestTier] POI DifficultyTier={tier}");
                if (tier < 1) return -1;
                if (tier > 6) tier = 6;
                return tier;
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaInfestedQuestTier] GetPlayerPOITier error: {e.Message}");
                return -1;
            }
        }
    }
}
