using Audio;

namespace SampleProject.Scripts
{
    public class PowerBlockAndGate : MultiParentPowerItem
    {
        protected string StartSound = "";
        protected string EndSound = "";
        protected bool lastActivate;

        public override void HandlePowerUpdate(bool isOn)
        {
            bool activated = this.isPowered & isOn;
            if (this.TileEntity != null)
            {
                this.TileEntity.Activate(activated);
                if (activated && this.lastActivate != activated)
                    this.TileEntity.ActivateOnce();
                this.TileEntity.SetModified();
            }
            this.lastActivate = activated;
            if (!this.PowerChildren())
                return;
            for (int index = 0; index < this.Children.Count; ++index)
                this.Children[index].HandlePowerUpdate(isOn);
        }

        public override void SetValuesFromBlock()
        {
            base.SetValuesFromBlock();
            Block block = Block.list[(int)this.BlockID];
            if (block.Properties.Values.ContainsKey("RequiredPower"))
                this.RequiredPower = ushort.Parse(block.Properties.Values["RequiredPower"]);
            if (block.Properties.Values.ContainsKey("StartSound"))
                this.StartSound = block.Properties.Values["StartSound"];
            if (!block.Properties.Values.ContainsKey("EndSound"))
                return;
            this.EndSound = block.Properties.Values["EndSound"];
        }

        protected override void IsPoweredChanged(bool newPowered) => Manager.BroadcastPlay(this.Position.ToVector3(), newPowered ? this.StartSound : this.EndSound);
    }
}