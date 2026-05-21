using System.Collections.Generic;
using UnityEngine;

namespace WakaPet
{
    /// <summary>
    /// デバッグ用 console command. F1 開いて使用.
    ///
    /// 使い方:
    ///   wakapet spawn          - プレイヤーの前 5m に caged Rocky を 1体 spawn
    ///   wakapet spawn N        - N体まとめて spawn (multiple test)
    ///   wakapet find           - 同 chunk内の caged を探してプレイヤー位置に呼び寄せ
    ///   wakapet count          - loaded world の Waka系 entity 数を表示
    ///   wakapet clear          - loaded world の Waka系 entity を全部 destroy (test cleanup)
    /// </summary>
    public class ConsoleCmdWakaPet : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new[] { "wakapet", "wp" };
        }

        public override string getDescription()
        {
            return "Waka Pet debug commands (spawn/find/count/clear)";
        }

        public override string getHelp()
        {
            return "Usage:\n" +
                   "  wakapet spawn [N]   spawn N caged Rocky in front of player (default 1)\n" +
                   "  wakapet find        teleport all caged Rocky in chunk to player\n" +
                   "  wakapet count       count Waka entities in loaded world\n" +
                   "  wakapet clear       destroy all Waka entities in loaded world";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (_params.Count == 0)
            {
                Log.Out(getHelp());
                return;
            }

            var world = GameManager.Instance?.World;
            if (world == null)
            {
                Log.Error("[WakaPet/Cmd] World not ready");
                return;
            }

            var player = world.GetPrimaryPlayer();
            if (player == null)
            {
                Log.Error("[WakaPet/Cmd] No primary player");
                return;
            }

            string sub = _params[0].ToLower();
            switch (sub)
            {
                case "spawn":
                    int count = 1;
                    if (_params.Count > 1) int.TryParse(_params[1], out count);
                    SpawnCaged(world, player, count);
                    break;
                case "find":
                    FindAndTeleportCaged(world, player);
                    break;
                case "count":
                    CountWakaEntities(world);
                    break;
                case "clear":
                    ClearWakaEntities(world);
                    break;
                default:
                    Log.Out(getHelp());
                    break;
            }
        }

        static void SpawnCaged(World world, EntityPlayer player, int count)
        {
            int classId = EntityClass.FromString("entityWakaPetRabbit_caged");
            if (classId == 0)
            {
                Log.Error("[WakaPet/Cmd] entityWakaPetRabbit_caged class not found");
                return;
            }

            // プレイヤーの前 5m + 円形配置
            Vector3 forward = player.transform.forward;
            Vector3 basePos = player.position + forward * 5f;

            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * 360f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * 1.5f, 0, Mathf.Sin(angle) * 1.5f);
                Vector3 pos = basePos + offset;

                // 地面に補正
                if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f))
                {
                    pos.y = hit.point.y;
                }

                var entity = EntityFactory.CreateEntity(classId, pos, Quaternion.identity.eulerAngles);
                if (entity == null)
                {
                    Log.Error($"[WakaPet/Cmd] Failed to create entity (i={i})");
                    continue;
                }
                world.SpawnEntityInWorld(entity);
                Log.Out($"[WakaPet/Cmd] caged Rocky spawned at {pos}, entityId={entity.entityId}");
            }
        }

        static void FindAndTeleportCaged(World world, EntityPlayer player)
        {
            int found = 0;
            var playerPos = player.position;
            var entities = new List<Entity>(world.Entities.list);
            foreach (var e in entities)
            {
                if (!(e is EntityAlive ea)) continue;
                var ec = EntityClass.list[ea.entityClass];
                if (ec?.entityClassName == null) continue;
                if (!ec.entityClassName.StartsWith("entityWakaPet")) continue;

                ea.SetPosition(playerPos + new Vector3(2 * (found % 3 - 1), 0, 2), false);
                Log.Out($"[WakaPet/Cmd] teleported entity {ea.entityId} ({ec.entityClassName}) to player");
                found++;
            }
            Log.Out($"[WakaPet/Cmd] {found} Waka entities teleported");
        }

        static void CountWakaEntities(World world)
        {
            int caged = 0, normal = 0, other = 0;
            foreach (var e in world.Entities.list)
            {
                if (!(e is EntityAlive ea)) continue;
                var ec = EntityClass.list[ea.entityClass];
                if (ec?.entityClassName == null) continue;
                if (ec.entityClassName == "entityWakaPetRabbit_caged") caged++;
                else if (ec.entityClassName == "entityWakaPetRabbit") normal++;
                else if (ec.entityClassName.StartsWith("entityWakaPet")) other++;
            }
            Log.Out($"[WakaPet/Cmd] caged={caged}, normal={normal}, other={other}");
        }

        static void ClearWakaEntities(World world)
        {
            int cleared = 0;
            var entities = new List<Entity>(world.Entities.list);
            foreach (var e in entities)
            {
                if (!(e is EntityAlive ea)) continue;
                var ec = EntityClass.list[ea.entityClass];
                if (ec?.entityClassName == null) continue;
                if (!ec.entityClassName.StartsWith("entityWakaPet")) continue;

                world.RemoveEntity(ea.entityId, EnumRemoveEntityReason.Despawned);
                cleared++;
            }
            Log.Out($"[WakaPet/Cmd] {cleared} Waka entities cleared");
        }
    }
}
