using System;
using System.Reflection;
using HarmonyLib;

namespace WakaPlayLog.Patches
{
    /// <summary>
    /// Postfix on EntityBuffs.AddBuff(string, Vector3i, int, bool, bool, float).
    /// In v2.6 the buff add path is EntityBuffs (not BuffManager). The first arg
    /// may be a comma-separated list of buff names — split before classifying.
    /// </summary>
    [HarmonyPatch(typeof(EntityBuffs), nameof(EntityBuffs.AddBuff),
        new Type[] { typeof(string), typeof(Vector3i), typeof(int), typeof(bool), typeof(bool), typeof(float) })]
    public static class EntityBuffs_AddBuff_Patch
    {
        static FieldInfo _parentField;

        public static void Postfix(EntityBuffs __instance, string _name)
        {
            try
            {
                if (string.IsNullOrEmpty(_name) || __instance == null) return;

                EntityAlive owner = ResolveOwner(__instance);
                if (owner == null) return;

                foreach (var n in _name.Split(','))
                {
                    var buffName = n.Trim();
                    if (string.IsNullOrEmpty(buffName)) continue;
                    CombatEvents.HandleBuffAdded(owner, buffName);
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaPlayLog] EntityBuffs_AddBuff_Patch failed: {e.Message}");
            }
        }

        static EntityAlive ResolveOwner(EntityBuffs buffs)
        {
            try
            {
                if (_parentField == null)
                {
                    _parentField = typeof(EntityBuffs).GetField("parent",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        ?? typeof(EntityBuffs).GetField("entity",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        ?? typeof(EntityBuffs).GetField("_entity",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                }
                if (_parentField != null)
                    return _parentField.GetValue(buffs) as EntityAlive;
            }
            catch { }
            return null;
        }
    }
}
