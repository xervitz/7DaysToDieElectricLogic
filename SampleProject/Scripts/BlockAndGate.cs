using System.Collections.Generic;
using Platform;
using SampleProject.Scripts;
using UnityEngine;

public class BlockAndGate : BlockPowered {
    private List<string> buffActions;
    private float brokenPercentage;
    public override void Init() {
        base.Init();
        if (this.Properties.Values.ContainsKey("BrokenPercentage"))
            this.brokenPercentage = Mathf.Clamp01(StringParsers.ParseFloat(this.Properties.Values["BrokenPercentage"]));
        else
            this.brokenPercentage = 0.25f;
    }
    public new MultiParentTileEntityPowered CreateTileEntity(Chunk chunk) {
        MultiParentTileEntityPowered tileEntity = new MultiParentTileEntityPowered(chunk);
        tileEntity.PowerItemType = MultiParentPowerItem.PowerItemTypes.AndGate;
        return tileEntity;
    }

    public override void OnBlockAdded(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue) {
        base.OnBlockAdded(_world, _chunk, _blockPos, _blockValue);
        if (_world.GetTileEntity(_chunk.ClrIdx, _blockPos) is TileEntityPoweredMeleeTrap)
            return;
        MultiParentTileEntityPowered tileEntity = this.CreateTileEntity(_chunk);
        tileEntity.localChunkPos = World.toBlock(_blockPos);
        tileEntity.InitializePowerData();
        _chunk.AddTileEntity((TileEntity)tileEntity);
    }

    public override void PlaceBlock(WorldBase _world, BlockPlacement.Result _result, EntityAlive _ea) {
        base.PlaceBlock(_world, _result, _ea);
        if (!(_world.GetTileEntity(_result.clrIdx, _result.blockPos) is TileEntityPoweredMeleeTrap tileEntity) || !((Object)_ea != (Object)null) || _ea.entityType != EntityType.Player)
            return;
        tileEntity.SetOwner(PlatformManager.InternalLocalUserIdentifier);
    }
    public override int OnBlockDamaged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _damagePoints, int _entityIdThatDamaged, bool _bUseHarvestTool, bool _bBypassMaxDamage, int _recDepth = 0) {
        if (((uint)_blockValue.meta & 2U) > 0U && 1.0 - (double)_blockValue.damage / (double)_blockValue.Block.MaxDamage > (double)this.brokenPercentage) {
            if (this.buffActions == null && this.Properties.Values.ContainsKey("Buff")) {
                string str1 = this.Properties.Values["Buff"];
                char[] chArray = new char[1] { ',' };
                foreach (string str2 in str1.Split(chArray))
                    this.buffActions.Add(str2);
            }
            if (this.buffActions != null && _world.GetTileEntity(_clrIdx, _blockPos) is TileEntityPoweredMeleeTrap tileEntity && tileEntity.IsPowered && _world.GetEntity(_entityIdThatDamaged) is EntityAlive entity) {
                ItemAction action = entity.inventory.holdingItemData.item.Actions[0];
                switch (action) {
                    case ItemActionMelee _:
                    label_8:
                        for (int index = 0; index < this.buffActions.Count; ++index) {
                            int num = (int)entity.Buffs.AddBuff(this.buffActions[index], tileEntity.OwnerEntityID);
                        }
                        break;
                    case ItemActionRanged _:
                        if (!((action as ItemActionRanged).HitmaskOverride != (string)null) || !((action as ItemActionRanged).HitmaskOverride.Value == "Melee"))
                            break;
                        goto label_8;
                }
            }
        }
        return base.OnBlockDamaged(_world, _clrIdx, _blockPos, _blockValue, _damagePoints, _entityIdThatDamaged, _bUseHarvestTool, _bBypassMaxDamage, _recDepth);
    }
}
