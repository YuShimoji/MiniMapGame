using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Generates deterministic furniture and discovery prop layouts from room semantics.
    /// Keeps all placement in pure data so runtime rendering stays stateless.
    /// </summary>
    public static class InteriorFurniturePlanner
    {
        private const float PlacementMargin = 0.75f;
        private const int MaxDiscoveryMarkersPerRoom = 2;

        private readonly struct FurnitureTemplate
        {
            public readonly FurnitureType type;
            public readonly Vector2 normalizedOffset;
            public readonly float angle;
            public readonly float scale;

            public FurnitureTemplate(FurnitureType type, Vector2 normalizedOffset, float angle = 0f, float scale = 1f)
            {
                this.type = type;
                this.normalizedOffset = normalizedOffset;
                this.angle = angle;
                this.scale = scale;
            }
        }

        public static List<InteriorFurniture> Generate(
            SeededRng rng,
            InteriorBuildingContext context,
            InteriorPreset preset,
            InteriorFloorData floorData)
        {
            var furniture = new List<InteriorFurniture>();
            if (preset == null || preset.furnitureDensity <= 0f)
            {
                return furniture;
            }

            foreach (var room in floorData.rooms)
            {
                AddRoomFurniture(furniture, rng, context, preset, room);
            }

            return furniture;
        }

        private static void AddRoomFurniture(
            List<InteriorFurniture> furniture,
            SeededRng rng,
            InteriorBuildingContext context,
            InteriorPreset preset,
            InteriorRoom room)
        {
            if (room.size.x <= 1f || room.size.y <= 1f)
            {
                return;
            }

            if (room.type == InteriorRoomType.WallVoid || room.type == InteriorRoomType.Shaft)
            {
                AddDecayProps(furniture, rng, context, preset, room, Mathf.Max(1, Mathf.RoundToInt(preset.decayLevel * 2f)));
                return;
            }

            var templates = GetTemplatesForRoom(room, context, preset);
            int budget = GetFurnitureBudget(room, preset, templates.Count);
            for (int i = 0; i < budget && i < templates.Count; i++)
            {
                furniture.Add(CreateFurniture(room, templates[i], rng));
            }

            int markerCount = GetDiscoveryMarkerCount(room, preset);
            if (markerCount > 0)
            {
                var markers = GetDiscoveryTemplates(room.type);
                for (int i = 0; i < markerCount && i < markers.Count; i++)
                {
                    furniture.Add(CreateFurniture(room, markers[i], rng));
                }
            }

            if (preset.decayLevel > 0.35f)
            {
                int decayCount = Mathf.RoundToInt(preset.decayLevel * GetDecayWeight(room.type));
                if (decayCount > 0)
                {
                    AddDecayProps(furniture, rng, context, preset, room, decayCount);
                }
            }
        }

        private static int GetFurnitureBudget(InteriorRoom room, InteriorPreset preset, int templateCount)
        {
            if (templateCount == 0)
            {
                return 0;
            }

            float area = room.size.x * room.size.y;
            float areaFactor = Mathf.Clamp01(area / 28f);
            float semanticFactor = room.type switch
            {
                InteriorRoomType.Corridor or InteriorRoomType.Hallway or InteriorRoomType.Stairwell => 0.25f,
                InteriorRoomType.Entrance or InteriorRoomType.Restroom => 0.4f,
                InteriorRoomType.SecretRoom or InteriorRoomType.Vault or InteriorRoomType.Laboratory => 0.75f,
                InteriorRoomType.Ruin or InteriorRoomType.Basement => 0.6f,
                _ => 1f
            };

            int budget = Mathf.RoundToInt(templateCount * preset.furnitureDensity * (0.55f + areaFactor * 0.65f) * semanticFactor);
            budget = Mathf.Clamp(budget, templateCount > 0 ? 1 : 0, templateCount);

            if (room.type == InteriorRoomType.Corridor || room.type == InteriorRoomType.Hallway)
            {
                budget = Mathf.Min(budget, 2);
            }

            return budget;
        }

        private static int GetDiscoveryMarkerCount(InteriorRoom room, InteriorPreset preset)
        {
            if (room.discoverySlotCount <= 0)
            {
                return 0;
            }

            if (room.type == InteriorRoomType.WallVoid || room.type == InteriorRoomType.Shaft)
            {
                return 0;
            }

            int count = Mathf.RoundToInt(room.discoverySlotCount * Mathf.Lerp(0.3f, 0.6f, preset.furnitureDensity));
            return Mathf.Clamp(count, 1, MaxDiscoveryMarkersPerRoom);
        }

        private static float GetDecayWeight(InteriorRoomType type)
        {
            return type switch
            {
                InteriorRoomType.Ruin => 3f,
                InteriorRoomType.Basement or InteriorRoomType.Storage or InteriorRoomType.Utility => 2f,
                InteriorRoomType.LoadingDock or InteriorRoomType.Workshop or InteriorRoomType.MachineryRoom => 1.5f,
                InteriorRoomType.Vault or InteriorRoomType.SecretRoom => 0.5f,
                _ => 1f
            };
        }

        private static void AddDecayProps(
            List<InteriorFurniture> furniture,
            SeededRng rng,
            InteriorBuildingContext context,
            InteriorPreset preset,
            InteriorRoom room,
            int count)
        {
            if (count <= 0)
            {
                return;
            }

            var decayTemplates = GetDecayTemplates(room, context, preset);
            for (int i = 0; i < count && i < decayTemplates.Count; i++)
            {
                furniture.Add(CreateFurniture(room, decayTemplates[i], rng));
            }
        }

        private static List<FurnitureTemplate> GetTemplatesForRoom(
            InteriorRoom room,
            InteriorBuildingContext context,
            InteriorPreset preset)
        {
            return room.type switch
            {
                InteriorRoomType.LivingRoom => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Sofa, new Vector2(-0.45f, 0f), 90f, 1.25f),
                    new(FurnitureType.Table, Vector2.zero, 0f, 1.1f),
                    new(FurnitureType.Chair, new Vector2(0.32f, -0.24f), 20f, 0.9f),
                    new(FurnitureType.Lamp, new Vector2(0.42f, 0.28f), 0f, 0.8f)
                },
                InteriorRoomType.Bedroom => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Bed, new Vector2(-0.3f, 0f), 90f, 1.25f),
                    new(FurnitureType.Cabinet, new Vector2(0.42f, 0.26f), 0f, 0.95f),
                    new(FurnitureType.Lamp, new Vector2(0.38f, -0.24f), 0f, 0.8f)
                },
                InteriorRoomType.Kitchen => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Stove, new Vector2(-0.38f, 0.3f), 0f, 0.9f),
                    new(FurnitureType.Fridge, new Vector2(0.42f, 0.3f), 0f, 0.95f),
                    new(FurnitureType.Sink, new Vector2(0f, 0.32f), 0f, 0.85f),
                    new(FurnitureType.Cabinet, new Vector2(-0.05f, -0.34f), 0f, 1.05f)
                },
                InteriorRoomType.Bathroom or InteriorRoomType.Restroom => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Bathtub, new Vector2(-0.25f, 0f), 90f, 1.1f),
                    new(FurnitureType.Sink, new Vector2(0.3f, 0.28f), 0f, 0.8f),
                    new(FurnitureType.Cabinet, new Vector2(0.34f, -0.26f), 0f, 0.75f)
                },
                InteriorRoomType.DiningRoom or InteriorRoomType.SeatingArea or InteriorRoomType.MeetingRoom => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Table, Vector2.zero, 0f, 1.2f),
                    new(FurnitureType.Chair, new Vector2(-0.32f, 0.26f), 0f, 0.85f),
                    new(FurnitureType.Chair, new Vector2(0.32f, 0.26f), 0f, 0.85f),
                    new(FurnitureType.Chair, new Vector2(0f, -0.3f), 180f, 0.85f)
                },
                InteriorRoomType.Shopfront => GetShopfrontTemplates(context),
                InteriorRoomType.Counter or InteriorRoomType.Bar => new List<FurnitureTemplate>
                {
                    new(FurnitureType.ShopCounter, new Vector2(-0.18f, 0f), 90f, 1.35f),
                    new(FurnitureType.Register, new Vector2(0.18f, 0.06f), 0f, 0.75f),
                    new(FurnitureType.Chair, new Vector2(0.34f, -0.22f), 180f, 0.8f)
                },
                InteriorRoomType.DisplayArea => new List<FurnitureTemplate>
                {
                    new(FurnitureType.DisplayCase, new Vector2(-0.3f, 0f), 90f, 1.1f),
                    new(FurnitureType.Mannequin, new Vector2(0.26f, 0.24f), 0f, 0.9f),
                    new(FurnitureType.Shelf, new Vector2(0.28f, -0.24f), 90f, 0.95f)
                },
                InteriorRoomType.Backroom or InteriorRoomType.Storage => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Shelf, new Vector2(-0.38f, 0.28f), 90f, 1f),
                    new(FurnitureType.Cabinet, new Vector2(0.38f, 0.28f), 90f, 0.9f),
                    new(FurnitureType.Container, new Vector2(-0.2f, -0.28f), 0f, 0.85f),
                    new(FurnitureType.Crate, new Vector2(0.26f, -0.24f), 0f, 0.9f)
                },
                InteriorRoomType.LoadingDock => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Pallet, new Vector2(-0.34f, 0f), 0f, 1.2f),
                    new(FurnitureType.Crate, new Vector2(0.15f, 0.18f), 10f, 1f),
                    new(FurnitureType.Barrel, new Vector2(0.34f, -0.24f), 0f, 0.9f)
                },
                InteriorRoomType.Workshop => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Workbench, new Vector2(-0.34f, 0.26f), 90f, 1.1f),
                    new(FurnitureType.Machine, new Vector2(0.32f, 0.24f), 0f, 1.1f),
                    new(FurnitureType.Crate, new Vector2(-0.16f, -0.24f), 0f, 0.9f),
                    new(FurnitureType.Barrel, new Vector2(0.28f, -0.28f), 0f, 0.85f)
                },
                InteriorRoomType.MachineryRoom => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Machine, new Vector2(-0.28f, 0f), 0f, 1.3f),
                    new(FurnitureType.Cabinet, new Vector2(0.34f, 0.26f), 0f, 0.9f),
                    new(FurnitureType.Barrel, new Vector2(0.32f, -0.24f), 0f, 0.8f)
                },
                InteriorRoomType.Lobby => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Sofa, new Vector2(-0.38f, 0.18f), 90f, 1.1f),
                    new(FurnitureType.Table, Vector2.zero, 0f, 1f),
                    new(FurnitureType.Lamp, new Vector2(0.35f, 0.24f), 0f, 0.85f),
                    new(FurnitureType.Desk, new Vector2(0.34f, -0.28f), 0f, 1f)
                },
                InteriorRoomType.Office => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Desk, new Vector2(-0.28f, 0.18f), 0f, 1.05f),
                    new(FurnitureType.Chair, new Vector2(-0.04f, 0.18f), 180f, 0.85f),
                    new(FurnitureType.Computer, new Vector2(-0.28f, 0.04f), 0f, 0.8f),
                    new(FurnitureType.FileCabinet, new Vector2(0.36f, -0.24f), 90f, 0.9f)
                },
                InteriorRoomType.Archive => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Bookshelf, new Vector2(-0.36f, 0.22f), 90f, 1.05f),
                    new(FurnitureType.FileCabinet, new Vector2(0.36f, 0.22f), 90f, 0.95f),
                    new(FurnitureType.Bookshelf, new Vector2(0f, -0.26f), 0f, 1f)
                },
                InteriorRoomType.Laboratory => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Desk, new Vector2(-0.32f, 0.22f), 0f, 1.05f),
                    new(FurnitureType.Computer, new Vector2(-0.32f, 0.06f), 0f, 0.75f),
                    new(FurnitureType.Cabinet, new Vector2(0.34f, 0.22f), 90f, 0.95f),
                    new(FurnitureType.Machine, new Vector2(0.2f, -0.26f), 0f, 1f)
                },
                InteriorRoomType.ServerRoom => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Computer, new Vector2(-0.26f, 0.22f), 0f, 0.85f),
                    new(FurnitureType.Cabinet, new Vector2(0.28f, 0.22f), 90f, 1f),
                    new(FurnitureType.Cabinet, new Vector2(0f, -0.24f), 0f, 1f)
                },
                InteriorRoomType.SecretRoom => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Container, Vector2.zero, 0f, 1.1f),
                    new(FurnitureType.Cabinet, new Vector2(-0.3f, 0.24f), 90f, 0.9f),
                    new(FurnitureType.Document, new Vector2(0.28f, -0.22f), 12f, 0.8f)
                },
                InteriorRoomType.Vault => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Container, Vector2.zero, 0f, 1.2f),
                    new(FurnitureType.Document, new Vector2(-0.3f, 0.24f), 15f, 0.75f),
                    new(FurnitureType.Note, new Vector2(0.26f, -0.2f), -10f, 0.75f)
                },
                InteriorRoomType.Basement => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Crate, new Vector2(-0.28f, 0.18f), 0f, 0.95f),
                    new(FurnitureType.Barrel, new Vector2(0.28f, 0.22f), 0f, 0.85f),
                    new(FurnitureType.Cobweb, new Vector2(0.36f, -0.26f), 0f, 0.9f)
                },
                InteriorRoomType.Utility => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Cabinet, new Vector2(-0.34f, 0.24f), 90f, 0.95f),
                    new(FurnitureType.Barrel, new Vector2(0.3f, 0.18f), 0f, 0.85f),
                    new(FurnitureType.Machine, new Vector2(0f, -0.24f), 0f, 0.95f)
                },
                InteriorRoomType.Entrance => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Lamp, new Vector2(0.34f, 0.2f), 0f, 0.8f),
                    new(FurnitureType.Note, new Vector2(-0.28f, -0.18f), 0f, 0.75f)
                },
                InteriorRoomType.Hallway or InteriorRoomType.Corridor => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Lamp, new Vector2(-0.32f, 0.2f), 0f, 0.75f),
                    new(FurnitureType.Note, new Vector2(0.28f, -0.18f), 0f, 0.75f)
                },
                InteriorRoomType.Stairwell => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Lamp, new Vector2(0.24f, 0.24f), 0f, 0.75f)
                },
                InteriorRoomType.Ruin => GetDecayTemplates(room, context, preset),
                _ => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Table, Vector2.zero, 0f, 1f),
                    new(FurnitureType.Cabinet, new Vector2(0.34f, 0.24f), 0f, 0.9f)
                }
            };
        }

        private static List<FurnitureTemplate> GetShopfrontTemplates(InteriorBuildingContext context)
        {
            return context.shopSubtype switch
            {
                ShopSubtype.Bookstore => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Bookshelf, new Vector2(-0.36f, 0.22f), 90f, 1.05f),
                    new(FurnitureType.Bookshelf, new Vector2(0.36f, 0.22f), 90f, 1.05f),
                    new(FurnitureType.DisplayCase, new Vector2(0f, -0.2f), 0f, 1f)
                },
                ShopSubtype.Pawnshop => new List<FurnitureTemplate>
                {
                    new(FurnitureType.DisplayCase, new Vector2(-0.3f, 0.16f), 90f, 1.1f),
                    new(FurnitureType.DisplayCase, new Vector2(0.3f, 0.16f), 90f, 1.1f),
                    new(FurnitureType.Register, new Vector2(0f, -0.24f), 0f, 0.75f)
                },
                ShopSubtype.Cafe => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Table, new Vector2(-0.28f, 0.18f), 0f, 0.9f),
                    new(FurnitureType.Table, new Vector2(0.28f, -0.18f), 0f, 0.9f),
                    new(FurnitureType.Chair, new Vector2(-0.08f, 0.18f), 180f, 0.8f),
                    new(FurnitureType.Chair, new Vector2(0.08f, -0.18f), 0f, 0.8f)
                },
                _ => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Shelf, new Vector2(-0.38f, 0.24f), 90f, 1f),
                    new(FurnitureType.DisplayCase, new Vector2(0f, 0f), 0f, 1.05f),
                    new(FurnitureType.Mannequin, new Vector2(0.36f, -0.24f), 0f, 0.9f)
                }
            };
        }

        private static List<FurnitureTemplate> GetDiscoveryTemplates(InteriorRoomType roomType)
        {
            return roomType switch
            {
                InteriorRoomType.Office or InteriorRoomType.Archive or InteriorRoomType.Laboratory => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Document, new Vector2(0.18f, -0.16f), 8f, 0.75f),
                    new(FurnitureType.Note, new Vector2(-0.12f, 0.18f), -12f, 0.72f)
                },
                InteriorRoomType.SecretRoom or InteriorRoomType.Vault or InteriorRoomType.Storage => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Container, new Vector2(0.24f, -0.18f), 0f, 0.82f),
                    new(FurnitureType.Document, new Vector2(-0.18f, 0.18f), 15f, 0.72f)
                },
                InteriorRoomType.Bedroom or InteriorRoomType.LivingRoom => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Photo, new Vector2(0.18f, -0.14f), -10f, 0.75f),
                    new(FurnitureType.Note, new Vector2(-0.18f, 0.18f), 10f, 0.72f)
                },
                InteriorRoomType.Shopfront or InteriorRoomType.DisplayArea or InteriorRoomType.Counter => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Note, new Vector2(0.18f, -0.18f), 5f, 0.72f),
                    new(FurnitureType.Container, new Vector2(-0.2f, 0.16f), 0f, 0.8f)
                },
                _ => new List<FurnitureTemplate>
                {
                    new(FurnitureType.Document, new Vector2(0.2f, -0.18f), 8f, 0.72f),
                    new(FurnitureType.Container, new Vector2(-0.18f, 0.18f), 0f, 0.8f)
                }
            };
        }

        private static List<FurnitureTemplate> GetDecayTemplates(
            InteriorRoom room,
            InteriorBuildingContext context,
            InteriorPreset preset)
        {
            var props = new List<FurnitureTemplate>
            {
                new(FurnitureType.Debris, new Vector2(-0.28f, 0.18f), 0f, 1f),
                new(FurnitureType.Rubble, new Vector2(0.18f, -0.2f), 0f, 1.1f),
                new(FurnitureType.Cobweb, new Vector2(0.36f, 0.26f), 0f, 0.9f)
            };

            if (context.nearHill || preset.style == InteriorStyle.Natural || room.type == InteriorRoomType.Ruin)
            {
                props.Add(new FurnitureTemplate(FurnitureType.Vine, new Vector2(-0.36f, -0.24f), 0f, 0.95f));
            }

            return props;
        }

        private static InteriorFurniture CreateFurniture(InteriorRoom room, FurnitureTemplate template, SeededRng rng)
        {
            float halfWidth = Mathf.Max(0.2f, room.size.x * 0.5f - PlacementMargin);
            float halfHeight = Mathf.Max(0.2f, room.size.y * 0.5f - PlacementMargin);
            var localOffset = new Vector2(
                template.normalizedOffset.x * halfWidth,
                template.normalizedOffset.y * halfHeight);
            var rotatedOffset = Rotate(localOffset, room.rotation);

            return new InteriorFurniture
            {
                roomId = room.id,
                type = template.type,
                position = room.position + rotatedOffset,
                angle = room.rotation + template.angle + rng.Range(-6f, 6f),
                scale = template.scale * rng.Range(0.94f, 1.08f)
            };
        }

        private static Vector2 Rotate(Vector2 point, float angleDegrees)
        {
            if (Mathf.Approximately(angleDegrees, 0f))
            {
                return point;
            }

            float rad = angleDegrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);
            return new Vector2(
                point.x * cos - point.y * sin,
                point.x * sin + point.y * cos);
        }
    }
}
