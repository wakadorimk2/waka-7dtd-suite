using System;
using System.Collections.Generic;

namespace WakaPlayLog
{
    /// <summary>
    /// Combat-category event builders. Classifies kills, detects player
    /// death (via EntityDeathPatch), HP threshold latching, equipment
    /// broken detection, buff acquisition tagging (vanilla/body_layer/
    /// medical_conditions) and infection stage progression.
    /// </summary>
    public static class CombatEvents
    {
        static readonly string[] BloodfallVariants = {
            "brutal", "alpha", "prime", "apex", "torment",
            "nightmare", "hellborn", "overlord", "demigod", "bloodlord",
            "stalker", "berserker"
        };

        static readonly string[] BossKeywords = {
            "demolition", "behemoth", "bossCop", "bossZombie"
        };

        public static void HandleEntityKilled(Entity killed, Entity killer)
        {
            if (!LogWriter.IsActive || killed == null) return;

            if (killed is EntityPlayer)
            {
                // Player death is also handled by EntityDeathPatch with richer
                // context. Skip here to avoid duplicate events.
                return;
            }

            if (!(killer is EntityPlayer)) return;

            var killedAlive = killed as EntityAlive;
            if (killedAlive == null) return;

            string entityName = killedAlive.EntityClass?.entityClassName ?? "unknown";
            string lo = entityName.ToLowerInvariant();
            string variant = DetectBloodfallVariant(lo);
            bool isFeral = lo.Contains("feral") || lo.Contains("radiated");
            bool isBoss = IsBoss(lo);

            string evt; string sev;
            if (isBoss) { evt = "boss_kill"; sev = "rare"; }
            else if (variant != null) { evt = "zombie_kill_special"; sev = "notable"; }
            else if (isFeral) { evt = "zombie_kill_special"; sev = "notable"; }
            else { evt = "zombie_kill_basic"; sev = "trivial"; }

            var data = new Dictionary<string, object>
            {
                { "entity", entityName },
                { "weapon", GetHeldItemName(killer) },
            };
            if (variant != null) data["variant"] = variant;
            if (isFeral) data["feral"] = true;

            LogWriter.Write("combat", evt, sev, data);
        }

        public static void HandlePlayerDeath(EntityAlive player, EntityAlive killerEntity, int damageType, string source)
        {
            if (!LogWriter.IsActive || player == null) return;
            GameState.Deaths++;
            GameState.CriticalHPLatched = false;

            string biome = null;
            try { biome = player.biomeStandingOn?.m_sBiomeName; } catch { }

            string killerName = null;
            try { killerName = killerEntity?.EntityClass?.entityClassName; } catch { }

            var data = new Dictionary<string, object>
            {
                { "killer_entity", killerName },
                { "damage_source", source },
                { "day", GameTime.CurrentDay() },
                { "biome", biome },
                { "pos", new int[] { (int)player.position.x, (int)player.position.y, (int)player.position.z } },
                { "deaths_this_session", GameState.Deaths },
            };

            LogWriter.Write("combat", "player_death", "rare", data);
        }

        public static void HandleBuffAdded(EntityAlive entity, string buffName)
        {
            if (!LogWriter.IsActive || entity == null || string.IsNullOrEmpty(buffName)) return;
            if (!(entity is EntityPlayer)) return;

            string source; var tags = new List<object>();
            ClassifyBuff(buffName, out source, tags);
            if (source == null) return; // not interesting

            var data = new Dictionary<string, object>
            {
                { "buff_name", buffName },
                { "source", source },
                { "tags", tags },
            };

            // Infection stage progression: dedicated event
            if (buffName.IndexOf("infection", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                int stage = ParseInfectionStage(buffName);
                if (stage > 0)
                {
                    var sdata = new Dictionary<string, object>(data) { { "stage", stage } };
                    LogWriter.Write("combat", "infection_progressed", "notable", sdata);
                    return;
                }
            }

            LogWriter.Write("combat", "buff_acquired", "notable", data);
        }

        public static void PollCriticalHP(EntityPlayerLocal player)
        {
            if (player == null) return;
            try
            {
                float hp = player.Health;
                float max = player.GetMaxHealth();
                if (max <= 0f) return;
                float pct = hp / max;
                if (!GameState.CriticalHPLatched && pct < 0.30f && hp > 0f)
                {
                    GameState.CriticalHPLatched = true;
                    LogWriter.Write("combat", "critical_hp", "notable", new Dictionary<string, object>
                    {
                        { "hp", (int)hp },
                        { "hp_max", (int)max },
                        { "pct", pct },
                    });
                }
                else if (GameState.CriticalHPLatched && pct > 0.60f)
                {
                    GameState.CriticalHPLatched = false;
                }
            }
            catch { }
        }

        public static void PollEquipmentBroken(EntityPlayerLocal player)
        {
            if (player == null) return;
            try
            {
                // Held weapon
                CheckBrokenItem(player.inventory?.holdingItemItemValue, "weapon");

                // Armor slots
                var eq = player.equipment;
                if (eq != null)
                {
                    int slotCount = eq.GetSlotCount();
                    for (int i = 0; i < slotCount; i++)
                    {
                        var iv = eq.GetSlotItem(i);
                        if (iv != null && !iv.IsEmpty()) CheckBrokenItem(iv, "armor");
                    }
                }
            }
            catch { }
        }

        static void CheckBrokenItem(ItemValue iv, string slot)
        {
            if (iv == null || iv.IsEmpty()) return;
            try
            {
                float useTimes = iv.UseTimes;
                float max = iv.MaxUseTimes;
                if (max <= 0f) return;
                if (useTimes < max) return;

                int id = iv.ItemClass?.Id ?? 0;
                int key = id * 10 + (slot == "weapon" ? 1 : 2);
                if (GameState.BrokenItemKeys.Contains(key)) return;
                GameState.BrokenItemKeys.Add(key);

                string evt = slot == "weapon" ? "weapon_broken" : "armor_broken";
                LogWriter.Write("combat", evt, "notable", new Dictionary<string, object>
                {
                    { "item", iv.ItemClass?.GetItemName() ?? "unknown" },
                    { "slot", slot },
                    { "quality", (int)iv.Quality },
                });
            }
            catch { }
        }

        // ---------- classification helpers ----------

        static bool IsBoss(string lo)
        {
            foreach (var k in BossKeywords)
                if (lo.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        static string DetectBloodfallVariant(string lo)
        {
            foreach (var v in BloodfallVariants)
                if (lo.Contains(v)) return v;
            return null;
        }

        static string GetHeldItemName(Entity killer)
        {
            try
            {
                if (killer is EntityPlayer p && p.inventory != null)
                {
                    var iv = p.inventory.holdingItemItemValue;
                    if (iv != null && !iv.IsEmpty()) return iv.ItemClass?.GetItemName();
                }
            }
            catch { }
            return null;
        }

        static void ClassifyBuff(string buffName, out string source, List<object> tags)
        {
            source = null;
            if (string.IsNullOrEmpty(buffName)) return;
            string lo = buffName.ToLowerInvariant();

            // Body Layer prefix
            if (lo.StartsWith("buffwakabody") || lo.StartsWith("buffbody"))
            {
                source = "body_layer";
                if (lo.Contains("deficit")) tags.Add("deficit");
                if (lo.Contains("excess")) tags.Add("excess");
                if (lo.Contains("damage") || lo.Contains("injury")) tags.Add("body_damage");
                tags.Add("negative");
                return;
            }

            // Medical conditions
            if (lo.Contains("medicalcondition") || lo.StartsWith("buffmc") || lo.StartsWith("buffmedical"))
            {
                source = "medical_conditions";
                if (lo.Contains("infection")) { tags.Add("infection"); tags.Add("progressed"); }
                else if (lo.Contains("vitamin") || lo.Contains("deficit")) tags.Add("deficit");
                else tags.Add("condition");
                tags.Add("negative");
                return;
            }

            // Vanilla negative DoT-like
            if (lo.Contains("bleeding") || lo.Contains("infection")
                || lo.Contains("sprained") || lo.Contains("broken")
                || lo.Contains("abrasion") || lo.Contains("burning")
                || lo.Contains("hypothermia") || lo.Contains("heatstroke")
                || lo.Contains("dysentery") || lo.Contains("foodpoisoning"))
            {
                source = "vanilla";
                if (lo.Contains("infection")) { tags.Add("infection"); tags.Add("progressed"); }
                else if (lo.Contains("bleed")) tags.Add("dot");
                else if (lo.Contains("broken") || lo.Contains("sprained")) tags.Add("injury");
                else tags.Add("dot");
                tags.Add("negative");
                return;
            }
        }

        static int ParseInfectionStage(string buffName)
        {
            // Looks for stage indicator digits in buff name (e.g. "infectionStage2")
            try
            {
                string lo = buffName.ToLowerInvariant();
                int idx = lo.IndexOf("stage", StringComparison.Ordinal);
                if (idx >= 0 && idx + 5 < lo.Length)
                {
                    char c = lo[idx + 5];
                    if (c >= '0' && c <= '9') return c - '0';
                }
                if (lo.EndsWith("01") || lo.EndsWith("02") || lo.EndsWith("03") || lo.EndsWith("04"))
                {
                    return int.Parse(lo.Substring(lo.Length - 2));
                }
            }
            catch { }
            return 0;
        }
    }
}
