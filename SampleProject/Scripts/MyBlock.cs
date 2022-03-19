
// <property name="Class" value="MyBlock, SampleProject" />
class MyBlock : Block
{
    public override void Init()
    {
        Log.Out($"Hello from {GetType()}! Suck mah dick!");
        base.Init();
    }

    public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player) {

        Log.Out("OUCH!");
        _player.AddHealth(-20);
        return base.OnBlockActivated(_indexInBlockActivationCommands, _world, _cIdx, _blockPos, _blockValue, _player);
    }
}