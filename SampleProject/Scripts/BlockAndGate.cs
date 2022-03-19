using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SampleProject.Scripts;
    public class BlockAndGate : BlockPowered {

        public new MultiParentTileEntityPowered CreateTileEntity(Chunk chunk) {
            MultiParentTileEntityPowered tileEntity = new MultiParentTileEntityPowered(chunk);
            tileEntity.PowerItemType = MultiParentPowerItem.PowerItemTypes.ElectricWireRelay;
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
    }
