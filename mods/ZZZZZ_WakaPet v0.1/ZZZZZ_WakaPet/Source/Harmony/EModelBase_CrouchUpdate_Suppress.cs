using System;
using HarmonyLib;

namespace WakaPet
{
    /// <summary>
    /// vanilla の EModelBase.CrouchUpdate は bones / Animator 構造を前提に動くため、
    /// rigging されてない Static Mesh (Rocky 等) を Mesh に持つ entity が spawn されると
    /// 毎フレーム NullReferenceException を投げてログが汚れる.
    /// Finalizer で NullReferenceException だけ握り潰す (他の例外はそのまま伝播).
    /// CrouchUpdate を skip しても Static Mesh は元々 crouch しないので問題なし.
    /// </summary>
    [HarmonyPatch(typeof(EModelBase), "CrouchUpdate")]
    public class EModelBase_CrouchUpdate_Suppress
    {
        static Exception Finalizer(Exception __exception)
        {
            if (__exception is NullReferenceException)
            {
                return null;  // suppress
            }
            return __exception;  // pass through other exceptions
        }
    }
}
