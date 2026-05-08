using UnityEngine;

namespace WakaBeacon
{
    public class WakaBeaconBlock : Block
    {
        public override void OnBlockAdded(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue, PlatformUserIdentifierAbs _addedByPlayer)
        {
            base.OnBlockAdded(_world, _chunk, _blockPos, _blockValue, _addedByPlayer);
            if (_blockValue.Block != this) return;
            WakaBeaconManager.Register(_blockPos);
        }

        public override void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
        {
            base.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);
            if (_blockValue.Block != this) return;
            WakaBeaconManager.Register(_blockPos);
        }

        public override void OnBlockRemoved(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
        {
            WakaBeaconManager.Unregister(_blockPos);
            base.OnBlockRemoved(_world, _chunk, _blockPos, _blockValue);
        }
    }
}
