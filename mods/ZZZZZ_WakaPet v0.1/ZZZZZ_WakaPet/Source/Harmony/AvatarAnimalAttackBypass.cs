using HarmonyLib;

namespace WakaPet
{
    /// <summary>
    /// Waka系 entity の attack animation NRE 回避.
    ///
    /// caged Rocky は npcAnimalMeleeTemplate 継承で AvatarAnimalController を使うが、
    /// Rocky の独自 mesh / rig には attack animation index が設定されておらず、
    /// AvatarAnimalController.IsAnimationAttackPlaying が null reference exception を投げる:
    ///
    ///   Entity Exception: System.NullReferenceException
    ///     at AvatarAnimalController.IsAnimationAttackPlaying ()
    ///     at EntityAlive.UseHoldingItem (System.Int32 _actionIndex, System.Boolean _isReleased)
    ///     at EntityAlive.Attack (System.Boolean _isReleased)
    ///     at UAI.UAITaskAttackTargetEntitySDX.Update (UAI.Context _context)
    ///
    /// UAI の attack consideration を `buffNPCModStopAttacking` で除外しても、
    /// 一部の attack action は consideration 持ってないため発火し続ける.
    ///
    /// 対処: Waka系 entity に対しては IsAnimationAttackPlaying が常に false を返すよう Prefix patch.
    /// Rocky は実際に攻撃しない (HandItem="" 設計) ので false で問題なし.
    /// </summary>
    [HarmonyPatch(typeof(AvatarAnimalController), nameof(AvatarAnimalController.IsAnimationAttackPlaying))]
    public class WakaPet_AvatarAnimalAttackBypass
    {
        static bool Prefix(AvatarAnimalController __instance, ref bool __result)
        {
            try
            {
                if (__instance == null) return true; // 通常処理に委ねる
                var go = __instance.gameObject;
                if (go == null) return true;
                var entity = go.GetComponent<EntityAlive>();
                if (entity == null) return true;
                var ec = EntityClass.list[entity.entityClass];
                if (ec?.entityClassName == null) return true;
                if (!ec.entityClassName.StartsWith("entityWakaPet")) return true;

                // Waka系 entity: attack中じゃないことにして NRE 回避
                __result = false;
                return false; // skip original
            }
            catch
            {
                return true; // 例外時は元の処理
            }
        }
    }
}
