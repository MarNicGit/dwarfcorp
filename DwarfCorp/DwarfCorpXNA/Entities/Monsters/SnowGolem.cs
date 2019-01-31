// Elf.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class SnowGolem : Creature
    {
        [EntityFactory("SnowGolem")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new SnowGolem(
                new CreatureStats(SharedClass, 0),
                "Evil",
                Manager.World.PlanService,
                Manager.World.Factions.Factions["Evil"],
                Manager,
                "Snow Golem",
                Position);
        }

        private static SnowGolemClass SharedClass = new SnowGolemClass();

        public SnowGolem()
        {
            
        }

        public SnowGolem(CreatureStats stats, string allies, PlanService planService, Faction faction, ComponentManager manager, string name, Vector3 position) :
            base(manager, stats, allies, planService, faction, name)
        {
            Physics = new Physics(Manager, name, Matrix.CreateTranslation(position), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            HasMeat = false;
            HasBones = false;
            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            Physics.AddChild(new MudGolemAI(Manager, Sensors) { Movement = { IsSessile = true, CanFly = false, CanSwim = false, CanWalk = false, CanClimb = false, CanClimbWalls = false } });

            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            var gems = ResourceLibrary.GetResourcesByTag(Resource.ResourceTags.Gem);
            for (int i = 0; i < 16;  i++)
            {
                int num = MathFunctions.RandInt(1, 32 - i);
                Inventory.AddResource(new ResourceAmount(Datastructures.SelectRandom(gems), num));
                i += num - 1;
            }

            Physics.Tags.Add("MudGolem");
            Physics.Mass = 100;

            Physics.AddChild(new ParticleTrigger("snow_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 5,
                SoundToPlay = ContentPaths.Audio.gravel
            });            

            Stats.FullName = TextGenerator.GenerateRandom("$goblinname");
            Stats.Size = 4;
            Resistances[DamageType.Fire] = 5;
            Resistances[DamageType.Acid] = 5;
            Resistances[DamageType.Cold] = 5;
            Species = "Snow Golem";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            Stats.CurrentClass = SharedClass;
            CreateSprite(Stats.CurrentClass, manager);
            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 3, 3))).SetFlag(Flag.ShouldSerialize, false);

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.demon0,
                ContentPaths.Audio.gravel,
            };

            base.CreateCosmeticChildren(manager);
        }
    }

    public class SnowGolemClass : EmployeeClass
    {
        public SnowGolemClass()
        {
            if (!staticClassInitialized)
            {
                InitializeClassStatics();
            }
            if (!staticsInitiailized)
            {
                InitializeStatics();
            }
        }

        void InitializeLevels()
        {
            Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "Snow Golem",
                    Pay = 25,
                    XP = 0,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Constitution = 20.0f
                    }
                }
            };
        }

        void InitializeActions()
        {
            Actions =
                Task.TaskCategory.Gather |
                Task.TaskCategory.Guard |
                Task.TaskCategory.Attack;
        }

        void InitializeAnimations()
        {
            Animations = AnimationLibrary.LoadCompositeAnimationSet(ContentPaths.Entities.snowman_animation, "Snowman");
        }

        public void InitializeWeapons()
        {
            Attacks = new List<Attack>()
            {
                new Attack("Snowball", 0.1f, 2.0f, 50.0f, ContentPaths.Audio.demon_attack, ContentPaths.Effects.hit)
                {
                    Mode = Attack.AttackMode.Ranged,
                    LaunchSpeed = 5.0f,
                    ProjectileType = "Snowball",
                    TriggerMode = Attack.AttackTrigger.Animation,
                    TriggerFrame = 1
                }
            };
        }

        protected override sealed void InitializeStatics()
        {
            Name = "Snow Golem";
            InitializeLevels();
            InitializeAnimations();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }
    }
}