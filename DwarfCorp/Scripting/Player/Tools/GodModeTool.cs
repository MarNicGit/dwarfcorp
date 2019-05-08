using System.Collections.Generic;
using System.Data;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    /// This is the debug tool that allows the player to mess with the engine.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class GodModeTool : PlayerTool
    {
        public String Command;
        private bool RotateLeftPressed = false;
        private bool RotateRightPressed = false;

        public ChunkManager Chunks { get; set; }

        public override void OnBegin()
        {
            Player.VoxSelector.SelectionType = GetSelectionTypeBySelectionBoxValue(Command);
        }

        public override void OnEnd()
        {

        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public GodModeTool(GameMaster master)
        {
            Player = master;
            Chunks = Player.World.ChunkManager;
        }

        private VoxelSelectionType GetSelectionTypeBySelectionBoxValue(string arg)
        {
            if (arg == "Delete Block" || arg.Contains("Build") || arg == "Kill Block" || arg.Contains("Decal") || arg.Contains("Grass"))
            {
                return VoxelSelectionType.SelectFilled;
            }
            else if (arg == "Nuke Column")
                return VoxelSelectionType.SelectFilled;
            else
            {
                return VoxelSelectionType.SelectEmpty;
            }
        }

        private void SelectorBox_OnSelectionModified(string arg)
        {
            Player.VoxSelector.SelectionType = GetSelectionTypeBySelectionBoxValue(arg);
        }

        public override void OnVoxelsSelected(List<VoxelHandle> refs, InputManager.MouseButton button)
        {
            if(Command.Contains("Build/"))
            {
                string type = Command.Substring(6);
                var room = RoomLibrary.CreateRoom(Player.Faction, type, Player.World);
                Player.Faction.RoomBuilder.DesignatedRooms.Add(room);
                RoomLibrary.CompleteRoomImmediately(room, refs);
            }
            if (Command.Contains("Spawn/"))
            {
                string type = Command.Substring(6);
                foreach (var vox in refs.Where(vox => vox.IsValid))
                {
                    if (vox.IsEmpty)
                    {
                        var craftItem = CraftLibrary.GetCraftable(type);
                        var offset = Vector3.Zero;

                        if (craftItem != null)
                            offset = craftItem.SpawnOffset;

                        var body = EntityFactory.CreateEntity<GameComponent>(type, vox.WorldPosition + new Vector3(0.5f, 0.0f, 0.5f) + offset);
                        if (body != null)
                        {
                            body.PropogateTransforms();

                            if (craftItem != null)
                            {
                                if (craftItem.AddToOwnedPool)
                                    Player.Faction.OwnedObjects.Add(body);

                                if (craftItem.Moveable)
                                    body.Tags.Add("Moveable");

                                if (craftItem.Deconstructable)
                                    body.Tags.Add("Deconstructable");
                            }
                        }
                    }
                }
            }
            else if (Command.Contains("Rail/"))
            {
                string type = Command.Substring("Rail/".Length);
                var junction = new Rail.JunctionPiece
                {
                    RailPiece = type,
                    Orientation = Rail.PieceOrientation.North,
                    Offset = Point.Zero
                };

                foreach (var vox in refs.Where(vox => vox.IsValid))
                {
                    if (vox.IsEmpty)
                    {
                        var entity = new Rail.RailEntity(Player.World.ComponentManager, vox, junction);
                        Player.World.ComponentManager.RootComponent.AddChild(entity);
                    }
                }
            }
            else if (Command.Contains("Grass/"))
            {
                var type = GrassLibrary.GetGrassType(Command.Substring(6));
                foreach (var vox in refs.Where(v => v.IsValid))
                {
                    var v = vox;
                    if (!vox.IsEmpty)
                    {
                        v.GrassType = type.ID;
                        v.GrassDecay = type.InitialDecayValue;
                    }
                }
            }
            //else if (Command.Contains("Decal/"))
            //{
            //    var type = DecalLibrary.GetGrassType(Command.Substring(6));
            //    foreach (var vox in refs.Where(v => v.IsValid))
            //    {
            //        var v = vox;
            //        if (!vox.IsEmpty)
            //            v.Decal = DecalType.EncodeDecal(DecalOrientation, type.ID);
            //    }
            //}
            else
            {
                foreach (var vox in refs.Where(vox => vox.IsValid))
                {
                    if (Command.Contains("Place/"))
                    {
                        string type = Command.Substring(6);
                        var v = vox;
                        v.Type = VoxelLibrary.GetVoxelType(type);
                        v.QuickSetLiquid(LiquidType.None, 0);

                        if (type == "Magic")
                        {
                            Player.World.ComponentManager.RootComponent.AddChild(
                                new DestroyOnTimer(Player.World.ComponentManager, Player.World.ChunkManager, vox)
                                {
                                    DestroyTimer = new Timer(5.0f + MathFunctions.Rand(-0.5f, 0.5f), true)
                                });
                        }
                    }
                    else switch (Command)
                        {
                            case "Delete Block":
                                {
                                    var v = vox;
                                    Player.World.Master.Faction.OnVoxelDestroyed(vox);
                                    v.Type = VoxelLibrary.EmptyType;
                                    v.QuickSetLiquid(LiquidType.None, 0);
                                }
                                break;
                            case "Nuke Column":
                                {
                                    for (var y = 1; y < Player.World.WorldSizeInVoxels.Y; ++y)
                                    {
                                        var v = Player.World.ChunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(vox.Coordinate.X, y, vox.Coordinate.Z));
                                        v.Type = VoxelLibrary.EmptyType;
                                        v.QuickSetLiquid(LiquidType.None, 0);
                                    }
                                }
                                break;
                            case "Kill Block":
                                foreach (var selected in refs)
                                {
                                    if (!selected.IsEmpty)
                                        VoxelHelpers.KillVoxel(Player.World, selected);
                                }
                                break;
                            case "Fill Water":
                                {
                                    if (vox.IsEmpty)
                                    {
                                        var v = vox;
                                        v.QuickSetLiquid(LiquidType.Water, WaterManager.maxWaterLevel);
                                    }
                                }
                                break;
                            case "Fill Lava":
                                {
                                    if (vox.IsEmpty)
                                    {
                                        var v = vox;
                                        v.QuickSetLiquid(LiquidType.Lava, WaterManager.maxWaterLevel);
                                    }
                                }
                                break;
                            case "Fire":
                                {
                                    foreach (var flam2 in Player.World.EnumerateIntersectingObjects(vox.GetBoundingBox(), CollisionType.Both).OfType<Flammable>())
                                        flam2.Heat = flam2.Flashpoint + 1;
                                }
                                break;

                            case "Kill Things":
                                {
                                    foreach (var comp in Player.World.EnumerateIntersectingObjects(vox.GetBoundingBox(), CollisionType.Both))
                                        comp.Die();
                                }
                                break;
                            case "Disease":
                                {
                                    foreach (var creature in Player.World.EnumerateIntersectingObjects(vox.GetBoundingBox(), CollisionType.Both).OfType<Creature>())
                                    {
                                        creature.Stats.AcquireDisease(DiseaseLibrary.GetRandomDisease());
                                    }
                                    break;
                                }
                            default:
                                break;
                        }
                }
            }
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.World.SetMouse(null);
                return;
            }

            Player.VoxSelector.Enabled = true;
            Player.BodySelector.Enabled = false;
            Player.World.SetMouse(Player.World.MousePointer);

            if (Command == "Repulse")
            {
                var location = Player.VoxSelector.VoxelUnderMouse;
                var center = location.GetBoundingBox().Center();
                foreach (var body in Player.World.EnumerateIntersectingObjects(location.GetBoundingBox(), CollisionType.Dynamic))
                {
                    var delta = center - body.Position;
                    delta.Normalize();
                    if (delta.Y < 0)
                        delta.Y = 0;
                    var transform = body.LocalTransform;
                    transform.Translation += delta * (float)time.ElapsedGameTime.TotalSeconds * 5;
                    body.LocalTransform = transform;
                }
            }
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
        }


        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            
        }
    }

}
