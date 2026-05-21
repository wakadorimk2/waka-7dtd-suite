using UnityEngine;

namespace WakaPet
{
    /// <summary>
    /// caged Rocky を「destroy せずに無効化」する共通処理.
    ///
    /// destroy (Killed / Despawned) すると sleeper system が
    /// "Restoring 'entityWakaPetRabbit_caged', count N" で
    /// 同じ slot に新規 spawn を繰り返す (sleeperVolume の roster 補充仕様).
    ///
    /// 解決策: entity を world に残したまま、見た目・物理・干渉を無効化.
    /// sleeper system は「sleeper まだ存在してる」と判定して restore を発火しない.
    /// プレイヤーが POI 離れたら chunk unload で自然に消える.
    ///
    /// 無効化処置:
    ///   - mesh / glow を非表示
    ///   - collider を無効化 (押せない / interact不可)
    ///   - 位置は動かさない (SetPosition で動かすと sleeper system が「逃げた」判定で補充発火)
    /// </summary>
    public static class CagedNeutralizer
    {
        public static void Neutralize(EntityAlive caged)
        {
            if (caged == null) return;
            try
            {
                var go = caged.gameObject;
                if (go == null) return;

                // 1. mesh 非表示 (見えなくする)
                foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    smr.enabled = false;
                }
                foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    mr.enabled = false;
                }

                // 2. light 消す
                foreach (var light in go.GetComponentsInChildren<Light>(true))
                {
                    light.enabled = false;
                }

                // 3. collider を無効化 (プレイヤー押さない、interact取られない)
                foreach (var col in go.GetComponentsInChildren<Collider>(true))
                {
                    col.isTrigger = true;
                    col.enabled = false;
                }

                // ★ 位置は絶対に動かさない: SetPosition すると sleeper system が
                //   「entity が assigned position から消えた = 失踪」判定で補充発火する.
                //   元位置のまま見た目だけ消す.

                Log.Out($"[WakaPet/Neutralize] caged {caged.entityId} neutralized (kept in world at original position)");
            }
            catch (System.Exception e)
            {
                Log.Error($"[WakaPet/Neutralize] Exception on entity {caged.entityId}: {e}");
            }
        }
    }
}
