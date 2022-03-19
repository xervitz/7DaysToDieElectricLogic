using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SampleProject.Scripts {
    public class MultiParentPowerItem : PowerItem {
        public new List<PowerItem> Parent = new List<PowerItem>();
        public new Vector3i Position = new Vector3i(9,7,0);
        public new List<PowerItem> Root;
        public new ushort Depth = ushort.MaxValue;
        public new ushort BlockID;
        protected new bool hasChangesLocal;
        public new ushort RequiredPower = 5;
        public new List<PowerItem> Children = new List<PowerItem>();
        public new MultiParentTileEntityPowered TileEntity;
        protected new bool isPowered;

        public new virtual bool IsPowered => this.isPowered;

        public MultiParentPowerItem() {
        }

        public new virtual bool CanParent(PowerItem newParent) => true;

        public new virtual int InputCount => 1;

        public new virtual PowerItem.PowerItemTypes PowerItemType => PowerItem.PowerItemTypes.Consumer;

        public virtual void AddTileEntity(MultiParentTileEntityPowered tileEntityPowered)
        {
            if (this.TileEntity == null)
            {
                this.TileEntity = tileEntityPowered;
                this.TileEntity.CreateWireDataFromPowerItem();
            }
            this.TileEntity.MarkWireDirty();
        }

        public void RemoveTileEntity(MultiParentTileEntityPowered tileEntityPowered)
        {
            if (this.TileEntity != tileEntityPowered)
                return;
            this.TileEntity = (MultiParentTileEntityPowered)null;
        }
    
        // public virtual PowerItem GetRoot() => this.Parents != null ? this.Parents.GetRoot() : this;

        public new virtual List<PowerItem> GetRoot()
        {
            List<PowerItem> retval = new List<PowerItem>();
            if (this.Parent.Count != 0)
            {
                foreach (PowerItem Parent in this.Parent)
                {
                    retval.Append(Parent.GetRoot());
                }
            }
            else
            {
                retval.Append(this);
            }
            return retval;
        }


        public new virtual void read(BinaryReader _br, byte _version)
        {
            this.BlockID = _br.ReadUInt16();
            this.SetValuesFromBlock();
            this.Position = StreamUtils.ReadVector3i(_br);
            if (_br.ReadBoolean())
                PowerManager.Instance.SetParent(this, PowerManager.Instance.GetPowerItemByWorldPos(StreamUtils.ReadVector3i(_br)));
            int num = (int)_br.ReadByte();
            this.Children.Clear();
            for (int index = 0; index < num; ++index)
            {
                PowerItem node = PowerItem.CreateItem((PowerItem.PowerItemTypes)_br.ReadByte());
                node.read(_br, _version);
                PowerManager.Instance.AddPowerNode(node, this);
            }
        }

        public new void RemoveSelfFromParent() => PowerManager.Instance.RemoveParent(this);

        public new virtual void write(BinaryWriter _bw)
        {
            _bw.Write(this.BlockID);
            StreamUtils.Write(_bw, this.Position);
            _bw.Write(this.Parent.Count != 0);
            if (this.Parent.Count != 0)
                foreach(PowerItem parent in this.Parent)
                    StreamUtils.Write(_bw, parent.Position);
            _bw.Write((byte)this.Children.Count);
            for (int index = 0; index < this.Children.Count; ++index)
            {
                _bw.Write((byte)this.Children[index].PowerItemType);
                this.Children[index].write(_bw);
            }
        }

        public new virtual bool PowerChildren() => true;

        protected new virtual void IsPoweredChanged(bool newPowered)
        {
        }

        public new virtual void HandlePowerReceived(ref ushort power)
        {
            ushort num = (ushort)Mathf.Min((int)this.RequiredPower, (int)power);
            bool newPowered = (int)num == (int)this.RequiredPower;
            if (newPowered != this.isPowered)
            {
                this.isPowered = newPowered;
                this.IsPoweredChanged(newPowered);
                if (this.TileEntity != null)
                    this.TileEntity.SetModified();
            }
            power -= num;
            if (power <= (ushort)0 || !this.PowerChildren())
                return;
            for (int index = 0; index < this.Children.Count; ++index)
            {
                this.Children[index].HandlePowerReceived(ref power);
                if (power <= (ushort)0)
                    break;
            }
        }

        internal PowerItem GetChild(Vector3 childPosition)
        {
            Vector3i vector3i = new Vector3i(childPosition);
            for (int index = 0; index < this.Children.Count; ++index)
            {
                if (this.Children[index].Position == vector3i)
                    return this.Children[index];
            }
            return (PowerItem)null;
        }

        internal bool HasChild(Vector3 child)
        {
            Vector3i vector3i = new Vector3i(child);
            for (int index = 0; index < this.Children.Count; ++index)
            {
                if (this.Children[index].Position == vector3i)
                    return true;
            }
            return false;
        }

        public new virtual void HandlePowerUpdate(bool isOn)
        {
        }

        public new virtual void HandleDisconnect()
        {
            if (this.isPowered)
                this.IsPoweredChanged(false);
            this.isPowered = false;
            this.HandlePowerUpdate(false);
            for (int index = 0; index < this.Children.Count; ++index)
                this.Children[index].HandleDisconnect();
        }

        public static MultiParentPowerItem CreateItem(MultiParentPowerItem.PowerItemTypes itemType)
        {
            switch (itemType)
            {
                case MultiParentPowerItem.PowerItemTypes.AndGate:
                    return new PowerBlockAndGate();
                default:
                    return new MultiParentPowerItem();
            }
        }

        public new virtual void SetValuesFromBlock()
        {
            Block block = Block.list[(int)this.BlockID];
            if (!block.Properties.Values.ContainsKey("RequiredPower"))
                return;
            this.RequiredPower = ushort.Parse(block.Properties.Values["RequiredPower"]);
        }

        public new void ClearChildren()
        {
            for (int index = 0; index < this.Children.Count; ++index)
                PowerManager.Instance.RemoveChild(this.Children[index]);
            if (this.TileEntity == null)
                return;
            this.TileEntity.DrawWires();
        }

        public new void SendHasLocalChangesToRoot()
        {
            this.hasChangesLocal = true;
            foreach (PowerItem parents in this.Parent)
                for (PowerItem parent = parents; parent != null; parent = parent.Parent)
                    parent.SendHasLocalChangesToRoot();
        }

        public new enum PowerItemTypes
        {
            None,
            Consumer,
            ConsumerToggle,
            Trigger,
            Timer,
            Generator,
            SolarPanel,
            BatteryBank,
            RangedTrap,
            ElectricWireRelay,
            TripWireRelay,
            PressurePlate,
            AndGate,
        }
    }
}
