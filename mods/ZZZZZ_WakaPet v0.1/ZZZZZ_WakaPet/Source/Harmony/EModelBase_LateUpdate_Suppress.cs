using System;
using HarmonyLib;

namespace WakaPet
{
    /// <summary>
    /// vanilla EModelBase.LateUpdate は CrouchUpdate / LookAtUpdate 等 bones 前提の処理を呼び出す.
    /// Static Mesh の entity (Rocky 等) では bones 不在で NullReferenceException が連発する.
    /// LateUpdate 全体に Finalizer を当てて NullRef だけまとめて握り潰す.
    /// 個別の method patch (CrouchUpdate 単体等) より広く効く.
    /// </summary>
    [HarmonyPatch(typeof(EModelBase), "LateUpdate")]
    public class EModelBase_LateUpdate_Suppress
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
