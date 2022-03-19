using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SampleProject.Scripts {
    class MultiParentTileEntityPowered : TileEntity, IPowered {
        private List<Vector3i> wireDataList = new List<Vector3i>();
        protected PowerItem PowerItem;
        public PowerItem.PowerItemTypes PowerItemType = PowerItem.PowerItemTypes.Trigger;
        public MultiParentTileEntityPowered(Chunk _chunk) : base(_chunk) { }
        private bool activateDirty;
        private bool needBlockData;
        private int requiredPower;
        public int RequiredPower {
            get {
                if (this.needBlockData && !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer) {
                    this.SetValuesFromBlock((ushort)GameManager.Instance.World.GetBlock(this.ToWorldPos()).type);
                    this.needBlockData = false;
                }
                return this.requiredPower;
            }
            private set => this.requiredPower = value;
        }

        public void InitializePowerData() {
            if ((UnityEngine.Object)GameManager.Instance == (UnityEngine.Object)null)
                return;
            ushort blockID = (ushort)GameManager.Instance.World.GetBlock(this.ToWorldPos()).type;
            if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer) {
                this.PowerItem = PowerManager.Instance.GetPowerItemByWorldPos(this.ToWorldPos());
                if (this.PowerItem == null)
                    this.CreatePowerItemForTileEntity(blockID);
                else
                    blockID = this.PowerItem.BlockID;
                this.PowerItem.AddTileEntity(this);
                this.SetModified();
                this.activateDirty = true;
            }
            this.SetValuesFromBlock(blockID);
            if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
                return;
            this.DrawWires();
        }

        protected virtual void SetValuesFromBlock(ushort blockID) {
            if (Block.list[(int)blockID].Properties.Values.ContainsKey("RequiredPower"))
                this.RequiredPower = Convert.ToInt32(Block.list[(int)blockID].Properties.Values["RequiredPower"]);
            else
                this.RequiredPower = 5;
        }
        public PowerItem CreatePowerItemForTileEntity(ushort blockID) {
            if (this.PowerItem == null) {
                this.PowerItem = this.CreatePowerItem();
                this.PowerItem.Position = this.ToWorldPos();
                this.PowerItem.BlockID = blockID;
                this.PowerItem.SetValuesFromBlock();
                PowerManager.Instance.AddPowerNode(this.PowerItem);
            }
            return this.PowerItem;
        }

        protected virtual PowerItem CreatePowerItem() => PowerItem.CreateItem(this.PowerItemType);
        public void AddWireData(Vector3i child) {
            this.wireDataList.Add(child);
            this.SendWireData();
        }

        public bool CanHaveParent(IPowered newParent) {
            throw new NotImplementedException();
        }

        public void CreateWireDataFromPowerItem() {
            throw new NotImplementedException();
        }

        public void DrawWires() {
            throw new NotImplementedException();
        }

        public Vector3i GetParent() {
            throw new NotImplementedException();
        }

        public PowerItem GetPowerItem() {
            throw new NotImplementedException();
        }

        public int GetRequiredPower() {
            throw new NotImplementedException();
        }

        public override TileEntityType GetTileEntityType() {
            throw new NotImplementedException();
        }

        public Vector3 GetWireOffset() {
            throw new NotImplementedException();
        }

        public void MarkChanged() {
            throw new NotImplementedException();
        }

        public void MarkWireDirty() {
            throw new NotImplementedException();
        }

        public void RemoveParentWithWiringTool(int wiringEntityID) {
            throw new NotImplementedException();
        }

        public void RemoveWires() {
            throw new NotImplementedException();
        }

        public void SendWireData() {
            throw new NotImplementedException();
        }

        public void SetParentWithWireTool(IPowered parent, int entityID) {
            throw new NotImplementedException();
        }

        public void SetWireData(List<Vector3i> wireChildren) {
            throw new NotImplementedException();
        }
    }
}
