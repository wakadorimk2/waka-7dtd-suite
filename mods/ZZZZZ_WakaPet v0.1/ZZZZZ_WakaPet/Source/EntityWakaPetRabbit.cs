using UnityEngine;

namespace WakaPet
{
    /// <summary>
    /// v0.4 以降は SCore の EntityAliveSDX に乗ったため、この派生 class は使われていない.
    /// entityclasses.xml の Class が WakaPet.EntityWakaPetRabbit を指定していないので
    /// 残しておいてもインスタンス化されない (将来の拡張用に保持).
    /// 実体の物理は npcAnimalMeleeTemplate + EntityAliveSDX (SCore) に委譲.
    /// </summary>
    public class EntityWakaPetRabbit : EntityAnimalRabbit
    {
        public override void Init(int _entityClass)
        {
            base.Init(_entityClass);
            AdjustCharacterController();
        }

        // vanilla rabbit の物理は CharacterController を見ていない (player 専用)
        // 独自の motion / raycast で動く想定だが、Static Prefab だと地面検出が機能せず落下する
        // → 強制的に毎フレーム地面 raycast で position を補正して接地させる
        // SetPosition 経由で vanilla の internal position field も同期 (fell off 判定回避)
        void LateUpdate()
        {
            try
            {
                Vector3 pos = transform.position;
                if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f))
                {
                    if (pos.y < hit.point.y - 0.05f)
                    {
                        SetPosition(new Vector3(pos.x, hit.point.y, pos.z), false);
                    }
                }
            }
            catch { /* swallow: LateUpdate 毎フレームなのでログ汚さない */ }
        }

        void AdjustCharacterController()
        {
            try
            {
                // root → 子全体 → なければ追加 の順で探索
                var cc = gameObject.GetComponent<CharacterController>();
                string source = "root";
                if (cc == null)
                {
                    cc = gameObject.GetComponentInChildren<CharacterController>(true);
                    source = cc != null ? ("child:" + cc.gameObject.name) : "none";
                }

                // 物理関連 component の状況をログにダンプ（rabbit の物理が何で動いてるか把握用）
                var rb = gameObject.GetComponentInChildren<Rigidbody>(true);
                var allColliders = gameObject.GetComponentsInChildren<Collider>(true);
                string colInfo = $"colliders={allColliders.Length}, rb={(rb != null ? rb.gameObject.name : "none")}";
                Log.Out($"[WakaPet] Init dump: cc_source={source}, {colInfo}");

                if (cc == null)
                {
                    Log.Warning("[WakaPet] No CharacterController found - adding to root");
                    cc = gameObject.AddComponent<CharacterController>();
                }
                cc.height = 1.5f;
                cc.radius = 0.4f;
                cc.center = new Vector3(0f, 0.75f, 0f);
                Log.Out($"[WakaPet] CC adjusted on {cc.gameObject.name}: h={cc.height} r={cc.radius} center={cc.center}");
            }
            catch (System.Exception e)
            {
                Log.Error($"[WakaPet] CC adjust failed: {e}");
            }
        }
    }
}
