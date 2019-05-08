using System;
using System.IO;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public partial class CreatureStats
    {
        #region Stat Stack

        [JsonProperty] private List<StatAdjustment> StatAdjustments = new List<StatAdjustment>();

        public void AddStatAdjustment(StatAdjustment Adjustment)
        {
            StatAdjustments.Add(Adjustment);
        }

        public void RemoveStatAdjustment(String Name)
        {
            StatAdjustments.RemoveAll(a => a.Name == Name);
        }

        public StatAdjustment FindAdjustment(String Name)
        {
            return StatAdjustments.FirstOrDefault(a => a.Name == Name);
        }

        public IEnumerable<StatAdjustment> EnumerateStatAdjustments()
        {
            return StatAdjustments;
        }

        public void ResetBuffs()
        {
            StatAdjustments.RemoveAll(a => a.Name != "base stats");
        }

        public float BaseDexterity { set { FindAdjustment("base stats").Dexterity = value; } }
        public float BaseConstitution { set { FindAdjustment("base stats").Constitution = value; } }
        public float BaseStrength { set { FindAdjustment("base stats").Strength = value; } }
        public float BaseWisdom { set { FindAdjustment("base stats").Wisdom = value; } }
        public float BaseCharisma { set { FindAdjustment("base stats").Charisma = value; } }
        public float BaseIntelligence { set { FindAdjustment("base stats").Intelligence = value; } }
        public float BaseSize { set { FindAdjustment("base stats").Size = value; } }

        public float Dexterity { get { return Math.Max(1, StatAdjustments.Sum(a => a.Dexterity)); } }
        public float Constitution { get { return Math.Max(1, StatAdjustments.Sum(a => a.Constitution)); } }
        public float Strength { get { return Math.Max(1, StatAdjustments.Sum(a => a.Strength)); } }
        public float Wisdom { get { return Math.Max(1, StatAdjustments.Sum(a => a.Wisdom)); } }
        public float Charisma { get { return Math.Max(1, StatAdjustments.Sum(a => a.Charisma)); } }
        public float Intelligence { get { return Math.Max(1, StatAdjustments.Sum(a => a.Intelligence)); } }
        public float Size { get { return Math.Max(1, StatAdjustments.Sum(a => a.Size)); } }

        #endregion

        public float MaxSpeed => Dexterity;
        public float MaxAcceleration => MaxSpeed * 2.0f;
        public float StoppingForce => MaxAcceleration * 6.0f;
        public float BaseDigSpeed => Strength + Size;
        public float BaseChopSpeed => Strength * 3.0f + Dexterity * 1.0f;
        public float JumpForce => 1000.0f;
        public float MaxHealth => (Strength + Constitution + Size) * 10.0f;
        public float EatSpeed => Size + Strength;
        public float HungerGrowth => Size * 0.025f;
        public float BaseFarmSpeed => Intelligence + Strength;
        public float BuildSpeed => (Intelligence + Dexterity) / 10.0f;
        public float HungerResistance => Constitution;
        public float Tiredness => CanSleep ? 1.0f / Constitution : 0.0f;

        public bool CanEat = false;
        public int Age = 0;
        public int RandomSeed;
        public float VoicePitch = 1.0f;
        public Gender Gender = Gender.Male;
        public bool CanSleep = false;
        public bool CanGetBored = false;
        public string FullName = "";
        public string Title = "";
        public int NumBlocksDestroyed = 0;
        public int NumItemsGathered = 0;
        public int NumRoomsBuilt = 0;
        public int NumThingsKilled = 0;
        public int NumBlocksPlaced = 0;
        public int XP = 0;
        public int LevelIndex = 0;
        public CreatureStatus Status;
        [JsonIgnore] public CreatureClass CurrentClass = null;
        [JsonIgnore] public CreatureClass.Level CurrentLevel => CurrentClass.Levels[LevelIndex]; 
        public Task.TaskCategory AllowedTasks = Task.TaskCategory.Attack | Task.TaskCategory.Gather | Task.TaskCategory.Plant | Task.TaskCategory.Harvest | Task.TaskCategory.Chop | Task.TaskCategory.Wrangle | Task.TaskCategory.TillSoil;
        public bool IsMigratory = false;
        [JsonIgnore] public bool IsOverQualified => CurrentClass != null ? CurrentClass.Levels.Count > LevelIndex + 1 && XP > CurrentClass.Levels[LevelIndex + 1].XP : false;
        public bool LaysEggs = false;
        public bool IsAsleep = false;
        public float HungerDamageRate = 10.0f;
        public bool IsOnStrike = false;
        public DwarfBux Money = 0;
        public bool IsFleeing = false;
        public bool CanReproduce = false;

        public bool IsTaskAllowed(Task.TaskCategory TaskCategory)
        {
            return (AllowedTasks & TaskCategory) == TaskCategory;
        }

        public CreatureStats()
        {
            Age = (int)Math.Max(MathFunctions.RandNormalDist(30, 15), 10);
            RandomSeed = MathFunctions.RandInt(int.MinValue, int.MaxValue);
            AddStatAdjustment(new StatAdjustment { Name = "base stats" });
            Status = new CreatureStatus();
        }

        public CreatureStats(CreatureClass creatureClass, int level) : this()
        {
            CurrentClass = creatureClass;
            AllowedTasks = CurrentClass.Actions;
            LevelIndex = level;
            XP = creatureClass.Levels[level].XP;

            BaseCharisma = CurrentLevel.BaseStats.Charisma;
            BaseConstitution = CurrentLevel.BaseStats.Constitution;
            BaseDexterity = CurrentLevel.BaseStats.Dexterity;
            BaseIntelligence = CurrentLevel.BaseStats.Intelligence;
            BaseStrength = CurrentLevel.BaseStats.Strength;
            BaseWisdom = CurrentLevel.BaseStats.Wisdom;
        }
        
        public void LevelUp()
        {
            LevelIndex = Math.Min(LevelIndex + 1, CurrentClass.Levels.Count - 1);
            StatAdjustments.RemoveAll(a => a.Name == "base stats");

            StatAdjustments.Add(new StatAdjustment
            {
                Name = "base stats",
                Dexterity = CurrentLevel.BaseStats.Dexterity,
                Constitution = CurrentLevel.BaseStats.Constitution,
                Strength = CurrentLevel.BaseStats.Strength,
                Wisdom = CurrentLevel.BaseStats.Wisdom,
                Charisma = CurrentLevel.BaseStats.Charisma,
                Intelligence = CurrentLevel.BaseStats.Intelligence
            });
        }
    }
}
