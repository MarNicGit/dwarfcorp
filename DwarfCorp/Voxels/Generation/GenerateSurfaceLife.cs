using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DwarfCorp.GameStates;
using LibNoise;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Math = System.Math;

namespace DwarfCorp.Generation
{
    public static partial class Generator
    {
        public static void GenerateSurfaceLife(VoxelChunk TopChunk, GeneratorSettings Settings)
        {
            var creatureCounts = new Dictionary<string, Dictionary<string, int>>();
            var worldDepth = Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY;

            for (var x = TopChunk.Origin.X; x < TopChunk.Origin.X + VoxelConstants.ChunkSizeX; x++)
                for (var z = TopChunk.Origin.Z; z < TopChunk.Origin.Z + VoxelConstants.ChunkSizeZ; z++)
                {
                    var overworldPosition = Overworld.WorldToOverworld(new Vector2(x, z), Settings.World.WorldScale, Settings.World.WorldOrigin);
                    var biome = Overworld.Map[(int)MathFunctions.Clamp(overworldPosition.X, 0, Overworld.Map.GetLength(0) - 1), (int)MathFunctions.Clamp(overworldPosition.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;
                    var biomeData = BiomeLibrary.Biomes[biome];

                    var normalizedHeight = NormalizeHeight(Overworld.LinearInterpolate(overworldPosition, Overworld.Map, Overworld.ScalarFieldType.Height), Settings.MaxHeight);
                    var height = (int)MathFunctions.Clamp(normalizedHeight * worldDepth, 0.0f, worldDepth - 2);

                    var voxel = Settings.World.ChunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(x, height, z));

                    if (!voxel.IsValid
                        || voxel.Coordinate.Y == 0
                        || voxel.Coordinate.Y >= worldDepth - Settings.TreeLine)
                        continue;

                    if (voxel.LiquidLevel != 0)
                        continue;

                    var above = VoxelHelpers.GetVoxelAbove(voxel);
                    if (above.IsValid && (above.LiquidLevel != 0 || !above.IsEmpty))
                        continue;
                    
                    foreach (var animal in biomeData.Fauna)
                    {
                        if (MathFunctions.RandEvent(animal.SpawnProbability))
                        {
                            if (!creatureCounts.ContainsKey(biomeData.Name))
                            {
                                creatureCounts[biomeData.Name] = new Dictionary<string, int>();
                            }
                            var dict = creatureCounts[biomeData.Name];
                            if (!dict.ContainsKey(animal.Name))
                            {
                                dict[animal.Name] = 0;
                            }
                            if (dict[animal.Name] < animal.MaxPopulation)
                            {
                                if (Settings.OverworldSettings.RevealSurface)
                                {
                                    EntityFactory.CreateEntity<GameComponent>(animal.Name,
                                        voxel.WorldPosition + Vector3.Up * 1.5f);
                                }
                                else
                                {
                                    var lambdaAnimal = animal;
                                    Settings.World.ComponentManager.RootComponent.AddChild(new SpawnOnExploredTrigger(Settings.World.ComponentManager, voxel)
                                    {
                                        EntityToSpawn = lambdaAnimal.Name,
                                        SpawnLocation = voxel.WorldPosition + new Vector3(0.5f, 1.5f, 0.5f)
                                    });
                                }
                            }
                            break;
                        }
                    }

                    if (voxel.Type.Name != biomeData.SoilLayer.VoxelType)
                        continue;

                    foreach (VegetationData veg in biomeData.Vegetation)
                    {
                        if (voxel.GrassType == 0)
                            continue;

                        if (MathFunctions.RandEvent(veg.SpawnProbability) &&
                            Settings.NoiseGenerator.Noise(voxel.Coordinate.X / veg.ClumpSize,
                            veg.NoiseOffset, voxel.Coordinate.Z / veg.ClumpSize) >= veg.ClumpThreshold)
                        {
                            var treeSize = MathFunctions.Rand() * veg.SizeVariance + veg.MeanSize;
                            if (Settings.OverworldSettings.RevealSurface)
                            {
                                EntityFactory.CreateEntity<Plant>(veg.Name,
                                voxel.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f),
                                Blackboard.Create("Scale", treeSize));
                            }
                            else
                            {
                                var lambdaFloraType = veg;
                                Settings.World.ComponentManager.RootComponent.AddChild(new SpawnOnExploredTrigger(Settings.World.ComponentManager, voxel)
                                {
                                    EntityToSpawn = lambdaFloraType.Name,
                                    SpawnLocation = voxel.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f),
                                    BlackboardData = Blackboard.Create("Scale", treeSize)
                                });
                            }

                            break;
                        }
                    }
                }
        }
    }
}