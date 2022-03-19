using Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SampleProject.Scripts {
    public class MultiParentTileEntityPowered : TileEntity, IPowered {
        private List<Vector3i> wireDataList = new List<Vector3i>();
        protected MultiParentPowerItem PowerItem;
        public MultiParentPowerItem.PowerItemTypes PowerItemType = MultiParentPowerItem.PowerItemTypes.Trigger;
        public Vector3 WireOffset = Vector3.zero;
        public MultiParentTileEntityPowered(Chunk _chunk) : base(_chunk) { }
        private bool activateDirty;
        protected bool wiresDirty;
        private bool needBlockData;
        private int requiredPower;
        private Transform blockTransform;
        private List<IWireNode> currentWireNodes = new List<IWireNode>();
        private Vector3i parentPosition = new Vector3i(-9999, -9999, -9999);

        public Transform BlockTransform {
            get => this.blockTransform;
            set {
                this.blockTransform = value;
                BlockValue block = GameManager.Instance.World.GetBlock(this.ToWorldPos());
                if ((UnityEngine.Object)this.blockTransform != (UnityEngine.Object)null) {
                    Transform transform = this.blockTransform.Find("WireOffset");
                    if ((UnityEngine.Object)transform != (UnityEngine.Object)null) {
                        this.WireOffset = block.Block.shape.GetRotation(block) * transform.localPosition;
                        return;
                    }
                }
                if (!block.Block.Properties.Values.ContainsKey("WireOffset"))
                    return;
                this.WireOffset = block.Block.shape.GetRotation(block) * StringParsers.ParseVector3(block.Block.Properties.Values["WireOffset"]);
            }
        }

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
                this.PowerItem = (MultiParentPowerItem)PowerManager.Instance.GetPowerItemByWorldPos(this.ToWorldPos());
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
        public MultiParentPowerItem CreatePowerItemForTileEntity(ushort blockID) {
            if (this.PowerItem == null) {
                this.PowerItem = this.CreatePowerItem();
                this.PowerItem.Position = this.ToWorldPos();
                this.PowerItem.BlockID = blockID;
                this.PowerItem.SetValuesFromBlock();
                PowerManager.Instance.AddPowerNode(this.PowerItem);
            }
            return this.PowerItem;
        }

        protected virtual MultiParentPowerItem CreatePowerItem() => MultiParentPowerItem.CreateItem(this.PowerItemType);
        public void AddWireData(Vector3i child) {
            this.wireDataList.Add(child);
            this.SendWireData();
        }

        public bool CanHaveParent(IPowered newParent) => true;

        public void CreateWireDataFromPowerItem() {
            this.wireDataList.Clear();
            if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
                return;
            for (int index = 0; index < this.PowerItem.Children.Count; ++index)
                this.wireDataList.Add(this.PowerItem.Children[index].Position);
        }

        public void DrawWires() {
            if ((UnityEngine.Object)this.BlockTransform == (UnityEngine.Object)null) {
                this.wiresDirty = true;
            }
            else {
                bool isOn = WireManager.Instance.ShowPulse;
                if (this.wireDataList.Count > 0) {
                    World world = GameManager.Instance.World;
                    if (isOn)
                        isOn = world.CanPlaceBlockAt(this.ToWorldPos(), world.gameManager.GetPersistentLocalPlayer(), false);
                }
                for (int index = 0; index < this.wireDataList.Count; ++index) {
                    Vector3i wireData = this.wireDataList[index];
                    if (GameManager.Instance.World.GetChunkFromWorldPos(wireData) is Chunk chunkFromWorldPos) {
                        TileEntityPowered tileEntity = GameManager.Instance.World.GetTileEntity(chunkFromWorldPos.ClrIdx, wireData) as TileEntityPowered;
                        bool flag = false;
                        if (tileEntity != null && (UnityEngine.Object)tileEntity.BlockTransform != (UnityEngine.Object)null)
                            flag = true;
                        if (!flag) {
                            this.wiresDirty = true;
                            return;
                        }
                    }
                }
                int index1 = 0;
                for (int index2 = 0; index2 < this.wireDataList.Count; ++index2) {
                    Vector3i wireData = this.wireDataList[index2];
                    if (GameManager.Instance.World.GetChunkFromWorldPos(wireData) is Chunk chunkFromWorldPos) {
                        MultiParentTileEntityPowered tileEntity = GameManager.Instance.World.GetTileEntity(chunkFromWorldPos.ClrIdx, wireData) as MultiParentTileEntityPowered;
                        if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer || !GameManager.IsDedicatedServer || tileEntity == null || this.PowerItemType == MultiParentPowerItem.PowerItemTypes.TripWireRelay && tileEntity.PowerItemType == MultiParentPowerItem.PowerItemTypes.TripWireRelay) {
                            if (index1 >= this.currentWireNodes.Count)
                                this.currentWireNodes.Add(WireManager.Instance.GetWireNodeFromPool());
                            this.currentWireNodes[index1].SetStartPosition(this.BlockTransform.position + Origin.position);
                            this.currentWireNodes[index1].SetStartPositionOffset(this.WireOffset);
                            if (tileEntity != null) {
                                if (this.PowerItemType == MultiParentPowerItem.PowerItemTypes.ElectricWireRelay && tileEntity.PowerItemType == MultiParentPowerItem.PowerItemTypes.ElectricWireRelay) {
                                    this.currentWireNodes[index1].SetPulseColor((Color)new Color32((byte)0, (byte)97, byte.MaxValue, byte.MaxValue));
                                    this.currentWireNodes[index1].SetWireRadius(0.005f);
                                    this.currentWireNodes[index1].SetWireDip(0.0f);
                                    ElectricWireController electricWireController = this.currentWireNodes[index1].GetGameObject().GetComponent<ElectricWireController>();
                                    if ((UnityEngine.Object)electricWireController == (UnityEngine.Object)null)
                                        electricWireController = this.currentWireNodes[index1].GetGameObject().AddComponent<ElectricWireController>();
                                    //electricWireController.TileEntityParent = this as TileEntityPoweredMeleeTrap;
                                    //electricWireController.TileEntityChild = tileEntity as TileEntityPoweredMeleeTrap;
                                    electricWireController.WireNode = this.currentWireNodes[index1];
                                    electricWireController.Init(this.chunk.GetBlock(this.localChunkPos).Block.Properties);
                                }
                                else if (this.PowerItemType == MultiParentPowerItem.PowerItemTypes.TripWireRelay && tileEntity.PowerItemType == MultiParentPowerItem.PowerItemTypes.TripWireRelay) {
                                    this.currentWireNodes[index1].SetPulseColor(Color.magenta);
                                    this.currentWireNodes[index1].SetWireRadius(0.0035f);
                                    this.currentWireNodes[index1].SetWireDip(0.0f);
                                    TripWireController tripWireController = this.currentWireNodes[index1].GetGameObject().GetComponent<TripWireController>();
                                    if ((UnityEngine.Object)tripWireController == (UnityEngine.Object)null)
                                        tripWireController = this.currentWireNodes[index1].GetGameObject().AddComponent<TripWireController>();
                                    //tripWireController.TileEntityParent = this as TileEntityPoweredTrigger;
                                    //tripWireController.TileEntityChild = tileEntity as TileEntityPoweredTrigger;
                                    tripWireController.WireNode = this.currentWireNodes[index1];
                                }
                                else {
                                    UnityEngine.Object.Destroy((UnityEngine.Object)this.currentWireNodes[index1].GetGameObject().GetComponent<ElectricWireController>());
                                    UnityEngine.Object.Destroy((UnityEngine.Object)this.currentWireNodes[index1].GetGameObject().GetComponent<TripWireController>());
                                }
                            }
                            this.currentWireNodes[index1].SetEndPosition(wireData.ToVector3() + Origin.position);
                            if (tileEntity != null)
                                this.currentWireNodes[index1].SetEndPositionOffset(tileEntity.WireOffset + new Vector3(0.5f, 0.5f, 0.5f));
                            this.currentWireNodes[index1].BuildMesh();
                            this.currentWireNodes[index1].TogglePulse(isOn);
                            ++index1;
                        }
                    }
                }
                for (int index3 = index1; index3 < this.currentWireNodes.Count; ++index3) {
                    WireManager.Instance.ReturnToPool(this.currentWireNodes[index1]);
                    this.currentWireNodes.RemoveAt(index1);
                }
                this.wiresDirty = false;
            }
        }

        public Vector3i GetParent() => this.parentPosition;

        public PowerItem GetPowerItem() => this.PowerItem;

        public int GetRequiredPower() => this.RequiredPower;

        public override TileEntityType GetTileEntityType() => TileEntityType.Powered;

        public Vector3 GetWireOffset() => this.WireOffset;

        public void MarkChanged() => this.SetModified();

        public void MarkWireDirty() => this.wiresDirty = true;

        public void RemoveParentWithWiringTool(int wiringEntityID) {
            if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer) {
                if (this.PowerItem.Parent.Count != 0) {
                    List<PowerItem> parents = this.PowerItem.Parent;
                    foreach (PowerItem parent in parents)
                    {
                        Vector3i position = parent.Position;
                        this.PowerItem.RemoveSelfFromParent();
                        if (parent.TileEntity != null)
                        {
                            parent.TileEntity.CreateWireDataFromPowerItem();
                            parent.TileEntity.SendWireData();
                            parent.TileEntity.RemoveWires();
                            parent.TileEntity.DrawWires();
                        }
                        Manager.BroadcastPlay(position.ToVector3(), this.PowerItem.IsPowered ? "wire_live_break" : "wire_dead_break");
                    }
                }
            }
            else {
                this.parentPosition = new Vector3i(-9999, -9999, -9999);
                SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer((NetPackage)NetPackageManager.GetPackage<NetPackageWireActions>().Setup(NetPackageWireActions.WireActions.RemoveParent, this.ToWorldPos(), new List<Vector3i>(), wiringEntityID));
            }
            this.SetModified();
        }

        public void RemoveWires() {
            for (int index = 0; index < this.currentWireNodes.Count; ++index)
                WireManager.Instance.ReturnToPool(this.currentWireNodes[index]);
            this.currentWireNodes.Clear();
        }

        public virtual bool Activate(bool activated) => false;
        public virtual bool ActivateOnce() => false;
        public void SendWireData() {
            if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
                SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage((NetPackage)NetPackageManager.GetPackage<NetPackageWireActions>().Setup(NetPackageWireActions.WireActions.SendWires, this.ToWorldPos(), this.wireDataList));
            else
                SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer((NetPackage)NetPackageManager.GetPackage<NetPackageWireActions>().Setup(NetPackageWireActions.WireActions.SendWires, this.ToWorldPos(), this.wireDataList));
        }

        public void SetParentWithWireTool(IPowered newParentTE, int wiringEntityID) {
            /*if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer) {
                PowerItem powerItem = newParentTE.GetPowerItem();
                PowerItem parent = this.PowerItem.Parent;
                PowerManager.Instance.SetParent(this.PowerItem, powerItem);
                if (parent != null && parent.TileEntity != null) {
                    parent.TileEntity.CreateWireDataFromPowerItem();
                    parent.TileEntity.SendWireData();
                    parent.TileEntity.RemoveWires();
                    parent.TileEntity.DrawWires();
                }
                newParentTE.CreateWireDataFromPowerItem();
                newParentTE.SendWireData();
                newParentTE.RemoveWires();
                newParentTE.DrawWires();
                Manager.BroadcastPlay(this.ToWorldPos().ToVector3(), powerItem.IsPowered ? "wire_live_connect" : "wire_dead_connect");
            }
            else {
                this.parentPosition = ((TileEntity)newParentTE).ToWorldPos();
                ConnectionManager instance = SingletonMonoBehaviour<ConnectionManager>.Instance;
                NetPackageWireActions package = NetPackageManager.GetPackage<NetPackageWireActions>();
                Vector3i worldPos = this.ToWorldPos();
                List<Vector3i> _wireChildren = new List<Vector3i>();
                _wireChildren.Add(this.parentPosition);
                int wiringEntity = wiringEntityID;
                NetPackageWireActions _package = package.Setup(NetPackageWireActions.WireActions.SetParent, worldPos, _wireChildren, wiringEntity);
                instance.SendToServer((NetPackage)_package);
            }
            this.SetModified();*/
        }

        public void SetWireData(List<Vector3i> wireChildren) {
            this.wireDataList = wireChildren;
            this.RemoveWires();
            this.DrawWires();
        }
    }
}
