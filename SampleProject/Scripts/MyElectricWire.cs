using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MyElectricWire : BlockElectricWire {

    public override int OnBlockDamaged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _damagePoints, int _entityIdThatDamaged, bool _bUseHarvestTool, bool _bBypassMaxDamage, int _recDepth = 0) {

        Log.Out("Get thrown");
        _world.GetEntity(_entityIdThatDamaged).transform.Translate(Vector3.forward * 10, Space.World);
        
        return base.OnBlockDamaged(_world, _clrIdx, _blockPos, _blockValue, _damagePoints, _entityIdThatDamaged, _bUseHarvestTool, _bBypassMaxDamage, _recDepth);
    }

}
