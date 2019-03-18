using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.SteamPipes
{
    public class ElevatorTrack : Body
    {
        [EntityFactory("Elevator Track")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            var resources = Data.GetData<List<ResourceAmount>>("Resources", null);

            if (resources == null)
                resources = new List<ResourceAmount>() { new ResourceAmount(ResourceType.Wood) };

            return new ElevatorTrack(Manager, Position, resources);
        }

        public UInt32 TrackAbove = ComponentManager.InvalidID;
        public UInt32 TrackBelow = ComponentManager.InvalidID;
        
        public RawPrimitive Primitive;
        private Color VertexColor = Color.White;
        private Color LightRamp = Color.White;
        private SpriteSheet Sheet;

        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            switch (messageToReceive.Type)
            {
                case Message.MessageType.OnChunkModified:
                    HasMoved = true;
                    break;
            }

            base.ReceiveMessageRecursive(messageToReceive);
        }

        public ElevatorTrack()
        {
            CollisionType = CollisionType.Static;
        }

        public ElevatorTrack(ComponentManager Manager, Vector3 Position, List<ResourceAmount> Resources) :
            base(Manager, "Elevator Track", Matrix.Identity, Vector3.One, Vector3.Zero)
        {
            CollisionType = CollisionType.Static;

            AddChild(new CraftDetails(Manager, "Elevator Track", Resources));

            CreateCosmeticChildren(Manager);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            Sheet = new SpriteSheet(ContentPaths.rail_tiles, 32, 32);
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect)
        {
            if (!IsVisible) return;

            base.RenderSelectionBuffer(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect);
            effect.SelectionBufferColor = this.GetGlobalIDColor().ToVector4();
            Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, false);
        }

        private float AngleBetweenVectors(Vector2 A, Vector2 B)
        {
            A.Normalize();
            B.Normalize();
            float DotProduct = Vector2.Dot(A, B);
            DotProduct = MathHelper.Clamp(DotProduct, -1.0f, 1.0f);
            float Angle = (float)global::System.Math.Acos(DotProduct);
            if (CrossZ(A, B) < 0) return -Angle;
            return Angle;
        }

        private float CrossZ(Vector2 A, Vector2 B)
        {
            return (B.Y * A.X) - (B.X * A.Y);
        }

        private void DrawNeighborConnection(UInt32 NeighborID)
        {
            if (NeighborID == ComponentManager.InvalidID) return;
            var neighbor = Manager.FindComponent(NeighborID);
            if (neighbor is ElevatorTrack neighborElevator)
                Drawer3D.DrawLine(Position, neighborElevator.Position, new Color(0.0f, 1.0f, 1.0f), 0.1f);
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);

            if (Debugger.Switches.DrawRailNetwork)
            {
                DrawNeighborConnection(TrackAbove);
                DrawNeighborConnection(TrackBelow);
            }

            if (Primitive == null)
            {
                var bounds = Vector4.Zero;
                var uvs = Sheet.GenerateTileUVs(new Point(0, 0), out bounds);

                Primitive = new RawPrimitive();

                Primitive.AddQuad(Matrix.CreateTranslation(0.5f, 0.0f, 0.0f), Color.White, Color.White, uvs, bounds);
            }

            if (Primitive.VertexCount == 0) return;

            var under = new VoxelHandle(chunks.ChunkData,
                    GlobalVoxelCoordinate.FromVector3(Position));

            if (under.IsValid)
            {
                Color color = new Color(under.Sunlight ? 255 : 0, 255, 0);
                LightRamp = color;
            }
            else
                LightRamp = new Color(200, 255, 0);

            Color origTint = effect.VertexColorTint;
            if (!Active)
            {
                DoStipple(effect);
            }
            effect.VertexColorTint = VertexColor;
            effect.LightRamp = LightRamp;
            effect.World = GlobalTransform;

            effect.MainTexture = Sheet.GetTexture();


            effect.EnableWind = false;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Primitive.Render(graphicsDevice);
            }

            effect.VertexColorTint = origTint;
            if (!Active)
            {
                EndDraw(effect);
            }
        }

        private string previousEffect = null;

        public void DoStipple(Shader effect)
        {
#if DEBUG
            if (effect.CurrentTechnique.Name == Shader.Technique.Stipple)
            {
                throw new InvalidOperationException("Stipple technique not cleaned up. Was EndDraw called?");
            }
#endif
            if (effect.CurrentTechnique != effect.Techniques[Shader.Technique.SelectionBuffer] && effect.CurrentTechnique != effect.Techniques[Shader.Technique.SelectionBufferInstanced])
            {
                previousEffect = effect.CurrentTechnique.Name;
                effect.CurrentTechnique = effect.Techniques[Shader.Technique.Stipple];
            }
            else
            {
                previousEffect = null;
            }
        }

        public void EndDraw(Shader shader)
        {
            if (!String.IsNullOrEmpty(previousEffect))
            {
                shader.CurrentTechnique = shader.Techniques[previousEffect];
            }
        }

        public void DetachFromNeighbors()
        {
            if (Manager.FindComponent(TrackAbove) is ElevatorTrack neighbor)
                neighbor.DetachNeighborBelow();
            if (Manager.FindComponent(TrackBelow) is ElevatorTrack neighbor2)
                neighbor2.DetachNeighborAbove();

            TrackAbove = ComponentManager.InvalidID;
            TrackBelow = ComponentManager.InvalidID;
            Primitive = null;
        }        

        public void DetachNeighborBelow()
        {
            TrackBelow = ComponentManager.InvalidID;
            Primitive = null;
        }

        public void DetachNeighborAbove()
        {
            TrackAbove = ComponentManager.InvalidID;
            Primitive = null;
        }

        private bool FindNeighbor(BoundingBox Bounds, out ElevatorTrack Neighbor)
        {
            Neighbor = null;

            foreach (var entity in Manager.World.EnumerateIntersectingObjects(Bounds, CollisionType.Static))
            {
                if (Object.ReferenceEquals(entity, this)) continue;
                if (entity is ElevatorTrack found)
                {
                    Neighbor = found;
                    return true;
                }
            }

            return false;
        }
                
        public void AttachToNeighbors()
        {
            System.Diagnostics.Debug.Assert(TrackAbove == ComponentManager.InvalidID && TrackBelow == ComponentManager.InvalidID);

            if (FindNeighbor(this.BoundingBox.Offset(0.0f, 1.0f, 0.0f).Expand(-0.2f), out ElevatorTrack aboveNeighbor))
            {
                AttachNeighborAbove(aboveNeighbor.GlobalID);
                aboveNeighbor.AttachNeighborBelow(this.GlobalID);
            }

            if (FindNeighbor(this.BoundingBox.Offset(0.0f, -1.0f, 0.0f).Expand(-0.2f), out ElevatorTrack belowNeighbor))
            {
                AttachNeighborAbove(belowNeighbor.GlobalID);
                belowNeighbor.AttachNeighborBelow(this.GlobalID);
            }
            
            Primitive = null;
        }

        public void AttachNeighborBelow(uint ID)
        {
            TrackBelow = ID;
            Primitive = null;
        }

        public void AttachNeighborAbove(uint ID)
        {
            TrackAbove = ID;
            Primitive = null;
        }

        public override void Delete()
        {
            base.Delete();
            DetachFromNeighbors();
        }

        public override void Die()
        {
            base.Die();
            DetachFromNeighbors();
        }
    }
}
