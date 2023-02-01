using System;
using System.Collections.Generic;
using Intersect.Enums;
using Intersect.GameObjects;
using Intersect.GameObjects.Maps;
using Intersect.Network.Packets.Server;
using Intersect.Server.Entities.Combat;
using Intersect.Server.Maps;
using Intersect.Utilities;

namespace Intersect.Server.Entities
{

    public partial class Projectile : Entity
    {

        public ProjectileBase Base;

        public bool HasGrappled;

        public ItemBase Item;

        private int mQuantity;

        private int mSpawnCount;

        private int mSpawnedAmount;

        private long mSpawnTime;

        private int mTotalSpawns;

        public Entity Owner;

        // Individual Spawns
        public ProjectileSpawn[] Spawns;

        public SpellBase Spell;

        public Entity Target;

        public Projectile(
            Entity owner,
            SpellBase parentSpell,
            ItemBase parentItem,
            ProjectileBase projectile,
            Guid mapId,
            byte X,
            byte Y,
            byte z,
            Directions direction,
            Entity target
        ) : base()
        {
            Base = projectile;
            Name = Base.Name;
            Owner = owner;
            Stat = owner.Stat;
            MapId = mapId;
            base.X = X;
            base.Y = Y;
            Z = z;
            SetMaxVital(Vitals.Health, 1);
            SetVital(Vitals.Health, 1);
            Dir = direction;
            Spell = parentSpell;
            Item = parentItem;

            Passable = true;
            HideName = true;
            for (var x = 0; x < ProjectileBase.SPAWN_LOCATIONS_WIDTH; x++)
            {
                for (var y = 0; y < ProjectileBase.SPAWN_LOCATIONS_HEIGHT; y++)
                {
                    for (var d = 0; d < ProjectileBase.MAX_PROJECTILE_DIRECTIONS; d++)
                    {
                        if (Base.SpawnLocations[x, y].Directions[d] == true)
                        {
                            mTotalSpawns++;
                        }
                    }
                }
            }

            mTotalSpawns *= Base.Quantity;
            Spawns = new ProjectileSpawn[mTotalSpawns];
        }

        private void AddProjectileSpawns(List<KeyValuePair<Guid, int>> spawnDeaths)
        {
            for (byte x = 0; x < ProjectileBase.SPAWN_LOCATIONS_WIDTH; x++)
            {
                for (byte y = 0; y < ProjectileBase.SPAWN_LOCATIONS_HEIGHT; y++)
                {
                    for (byte d = 0; d < ProjectileBase.MAX_PROJECTILE_DIRECTIONS; d++)
                    {
                        if (Base.SpawnLocations[x, y].Directions[d] == true && mSpawnedAmount < Spawns.Length)
                        {
                            var s = new ProjectileSpawn(
                                FindProjectileRotationDir(Dir, (Directions)d),
                                (byte) (X + FindProjectileRotationX(Dir, x - 2, y - 2)),
                                (byte) (Y + FindProjectileRotationY(Dir, x - 2, y - 2)), (byte) Z, MapId, MapInstanceId, Base, this
                            );

                            Spawns[mSpawnedAmount] = s;
                            mSpawnedAmount++;
                            mSpawnCount++;
                            if (CheckForCollision(s))
                            {
                                s.Dead = true;
                            }
                        }
                    }
                }
            }

            mQuantity++;
            mSpawnTime = Timing.Global.Milliseconds + Base.Delay;
        }

        private static int FindProjectileRotationX(Directions direction, int x, int y)
        {
            switch (direction)
            {
                case Directions.Up:
                    return x;
                case Directions.Down:
                    return -x;
                case Directions.Left:
                case Directions.UpLeft:
                case Directions.DownLeft:
                    return y;
                case Directions.Right:
                case Directions.UpRight:
                case Directions.DownRight:
                    return -y;
                default:
                    return x;
            }
        }

        private static int FindProjectileRotationY(Directions direction, int x, int y)
        {
            switch (direction)
            {
                case Directions.Up:
                    return y;
                case Directions.Down:
                    return -y;
                case Directions.Left:
                case Directions.UpLeft:
                case Directions.DownLeft:
                    return -x;
                case Directions.Right:
                case Directions.UpRight:
                case Directions.DownRight:
                    return x;
                default:
                    return y;
            }
        }

        private static Directions FindProjectileRotationDir(Directions entityDir, Directions projectionDir)
        {
            switch (entityDir)
            {
                case Directions.Up:
                    return projectionDir;
                case Directions.Down:
                    switch (projectionDir)
                    {
                        case Directions.Up:
                            return Directions.Down;
                        case Directions.Down:
                            return Directions.Up;
                        case Directions.Left:
                            return Directions.Right;
                        case Directions.Right:
                            return Directions.Left;
                        case Directions.UpLeft:
                            return Directions.DownLeft;
                        case Directions.UpRight:
                            return Directions.DownRight;
                        case Directions.DownRight:
                            return Directions.UpLeft;
                        case Directions.DownLeft:
                            return Directions.UpRight;
                        default:
                            return projectionDir;
                    }
                case Directions.Left:
                    switch (projectionDir)
                    {
                        case Directions.Up:
                            return Directions.Left;
                        case Directions.Down:
                            return Directions.Right;
                        case Directions.Left:
                            return Directions.Down;
                        case Directions.Right:
                            return Directions.Up;
                        case Directions.UpLeft:
                            return Directions.DownRight;
                        case Directions.UpRight:
                            return Directions.UpLeft;
                        case Directions.DownRight:
                            return Directions.DownLeft;
                        case Directions.DownLeft:
                            return Directions.UpRight;
                        default:
                            return projectionDir;
                    }
                case Directions.Right:
                    switch (projectionDir)
                    {
                        case Directions.Up:
                            return Directions.Right;
                        case Directions.Down:
                            return Directions.Left;
                        case Directions.Left:
                            return Directions.Up;
                        case Directions.Right:
                            return Directions.Down;
                        case Directions.UpLeft:
                            return Directions.UpRight;
                        case Directions.UpRight:
                            return Directions.DownRight;
                        case Directions.DownRight:
                            return Directions.DownLeft;
                        case Directions.DownLeft:
                            return Directions.UpLeft;
                        default:
                            return projectionDir;
                    }
                case Directions.UpLeft:
                    switch (projectionDir)
                    {
                        case Directions.Up:
                            return Directions.UpLeft;
                        case Directions.Down:
                            return Directions.DownRight;
                        case Directions.Left:
                            return Directions.DownLeft;
                        case Directions.Right:
                            return Directions.UpRight;
                        case Directions.UpLeft:
                            return Directions.Left;
                        case Directions.UpRight:
                            return Directions.Up;
                        case Directions.DownRight:
                            return Directions.Right;
                        case Directions.DownLeft:
                            return Directions.Down;
                        default:
                            return projectionDir;
                    }
                case Directions.UpRight:
                    switch (projectionDir)
                    {
                        case Directions.Up:
                            return Directions.UpRight;
                        case Directions.Down:
                            return Directions.DownLeft;
                        case Directions.Left:
                            return Directions.UpLeft;
                        case Directions.Right:
                            return Directions.DownRight;
                        case Directions.UpLeft:
                            return Directions.Up;
                        case Directions.UpRight:
                            return Directions.Right;
                        case Directions.DownRight:
                            return Directions.Down;
                        case Directions.DownLeft:
                            return Directions.Left;
                        default:
                            return projectionDir;
                    }
                case Directions.DownLeft:
                    switch (projectionDir)
                    {
                        case Directions.Up:
                            return Directions.DownLeft;
                        case Directions.Down:
                            return Directions.UpRight;
                        case Directions.Left:
                            return Directions.DownRight;
                        case Directions.Right:
                            return Directions.UpLeft;
                        case Directions.UpLeft:
                            return Directions.Down;
                        case Directions.UpRight:
                            return Directions.Left;
                        case Directions.DownRight:
                            return Directions.Up;
                        case Directions.DownLeft:
                            return Directions.Right;
                        default:
                            return projectionDir;
                    }
                case Directions.DownRight:
                    switch (projectionDir)
                    {
                        case Directions.Up:
                            return Directions.DownRight;
                        case Directions.Down:
                            return Directions.UpLeft;
                        case Directions.Left:
                            return Directions.UpRight;
                        case Directions.Right:
                            return Directions.DownLeft;
                        case Directions.UpLeft:
                            return Directions.Right;
                        case Directions.UpRight:
                            return Directions.Down;
                        case Directions.DownRight:
                            return Directions.Left;
                        case Directions.DownLeft:
                            return Directions.Up;
                        default:
                            return projectionDir;
                    }
                default:
                    return projectionDir;
            }
        }

        public void Update(List<Guid> projDeaths, List<KeyValuePair<Guid, int>> spawnDeaths)
        {
            if (mQuantity < Base.Quantity && Timing.Global.Milliseconds > mSpawnTime)
            {
                AddProjectileSpawns(spawnDeaths);
            }

            ProcessFragments(projDeaths, spawnDeaths);
        }

        private static float GetRangeX(Directions direction, float range)
        {
            switch (direction)
            {
                case Directions.Left:
                case Directions.UpLeft:
                case Directions.DownLeft:
                    return -range;
                case Directions.Right:
                case Directions.UpRight:
                case Directions.DownRight:
                    return range;
                case Directions.Up:
                case Directions.Down:
                default:
                    return 0;
            }
        }

        private static float GetRangeY(Directions direction, float range)
        {
            switch (direction)
            {
                case Directions.Up:
                case Directions.UpLeft:
                case Directions.UpRight:
                    return -range;
                case Directions.Down:
                case Directions.DownLeft:
                case Directions.DownRight:
                    return range;
                case Directions.Left:
                case Directions.Right:
                default:
                    return 0;
            }
        }

        public void ProcessFragments(List<Guid> projDeaths, List<KeyValuePair<Guid, int>> spawnDeaths)
        {
            if (Base == null)
            {
                return;
            }

            if (mSpawnCount != 0 || mQuantity < Base.Quantity)
            {
                for (var i = 0; i < mSpawnedAmount; i++)
                {
                    var spawn = Spawns[i];
                    if (spawn != null)
                    {
                        while (Timing.Global.Milliseconds > spawn.TransmittionTimer && Spawns[i] != null)
                        {
                            var x = spawn.X;
                            var y = spawn.Y;
                            var map = spawn.MapId;
                            var killSpawn = false;
                            if (!spawn.Dead)
                            {
                                killSpawn = MoveFragment(spawn);
                                if (!killSpawn && (x != spawn.X || y != spawn.Y || map != spawn.MapId))
                                {
                                    killSpawn = CheckForCollision(spawn);
                                }
                            }

                            if (killSpawn || spawn.Dead)
                            {
                                spawnDeaths.Add(new KeyValuePair<Guid, int>(Id, i));
                                Spawns[i] = null;
                                mSpawnCount--;
                            }
                        }
                    }
                }
            }
            else
            {
                lock (EntityLock)
                {
                    projDeaths.Add(Id);
                    Die(false);
                }
            }
        }

        public bool CheckForCollision(ProjectileSpawn spawn)
        {
            if(spawn == null)
            {
                return false;
            }

            var killSpawn = MoveFragment(spawn, false);

            //Check Map Entities For Hits
            var map = MapController.Get(spawn.MapId);
            if ((int)spawn.X < 0 || (int)spawn.X >= Options.Instance.MapOpts.Width ||
                (int)spawn.Y < 0 || (int)spawn.Y >= Options.Instance.MapOpts.Height)
            {
                return false;
            }
            var attribute = map.Attributes[(int)spawn.X, (int)spawn.Y];

            if (!killSpawn && attribute != null)
            {
                //Check for Z-Dimension
                if (!spawn.ProjectileBase.IgnoreZDimension)
                {
                    if (attribute.Type == MapAttributes.ZDimension)
                    {
                        if (((MapZDimensionAttribute) attribute).GatewayTo > 0)
                        {
                            spawn.Z = (byte) (((MapZDimensionAttribute) attribute).GatewayTo - 1);
                        }
                    }
                }

                //Check for grapplehooks.
                if (attribute.Type == MapAttributes.GrappleStone &&
                    Base.GrappleHookOptions.Contains(GrappleOptions.MapAttribute) &&
                    !spawn.Parent.HasGrappled &&
                    (spawn.X != Owner.X || spawn.Y != Owner.Y))
                {
                    if (spawn.Dir <= Directions.Right) //Don't handle directional projectile grapplehooks
                    {
                        spawn.Parent.HasGrappled = true;

                        //Only grapple if the player hasnt left the firing position.. if they have then we assume they dont wanna grapple
                        if (Owner.X == X && Owner.Y == Y && Owner.MapId == MapId)
                        {
                            Owner.Dir = spawn.Dir;
                            new Dash(
                                Owner, spawn.Distance, Owner.Dir, Base.IgnoreMapBlocks,
                                Base.IgnoreActiveResources, Base.IgnoreExhaustedResources, Base.IgnoreZDimension
                            );
                        }

                        killSpawn = true;
                    }
                }

                if (!spawn.ProjectileBase.IgnoreMapBlocks &&
                    (attribute.Type == MapAttributes.Blocked || attribute.Type == MapAttributes.Animation && ((MapAnimationAttribute)attribute).IsBlock))
                {
                    killSpawn = true;
                }
            }

            if (!killSpawn && MapController.TryGetInstanceFromMap(map.Id, MapInstanceId, out var mapInstance))
            {
                var entities = mapInstance.GetEntities();
                for (var z = 0; z < entities.Count; z++)
                {
                    if (entities[z] != null && entities[z] != spawn.Parent.Owner && entities[z].Z == spawn.Z &&
                        (entities[z].X == Math.Round(spawn.X) || entities[z].X == Math.Ceiling(spawn.X) || entities[z].X == Math.Floor(spawn.X)) &&
                        (entities[z].Y == Math.Round(spawn.Y) || entities[z].Y == Math.Ceiling(spawn.Y) || entities[z].Y == Math.Floor(spawn.Y)) &&
                        (spawn.X != Owner.X || spawn.Y != Owner.Y))
                    {
                        killSpawn = spawn.HitEntity(entities[z]);
                        if (killSpawn && !spawn.ProjectileBase.PierceTarget)
                        {
                            return killSpawn;
                        }
                    }
                    else
                    {
                        if (z == entities.Count - 1)
                        {
                            if (spawn.Distance >= Base.Range)
                            {
                                killSpawn = true;
                            }
                        }
                    }
                }
            }

            return killSpawn;
        }

        public bool MoveFragment(ProjectileSpawn spawn, bool move = true)
        {
            float newx = spawn.X;
            float newy = spawn.Y;
            var newMapId = spawn.MapId;

            if (move)
            {
                spawn.Distance++;
                spawn.TransmittionTimer += (long)(Base.Speed / (float)Base.Range);
                newx = spawn.X + GetRangeX(spawn.Dir, 1);
                newy = spawn.Y + GetRangeY(spawn.Dir, 1);
            }

            var killSpawn = false;
            var map = MapController.Get(spawn.MapId);

            if (Math.Round(newx) < 0)
            {
                if (MapController.Get(map.Left) != null)
                {
                    newMapId = MapController.Get(spawn.MapId).Left;
                    newx = Options.MapWidth - 1;
                }
                else
                {
                    killSpawn = true;
                }
            }

            if (Math.Round(newx) > Options.MapWidth - 1)
            {
                if (MapController.Get(map.Right) != null)
                {
                    newMapId = MapController.Get(spawn.MapId).Right;
                    newx = 0;
                }
                else
                {
                    killSpawn = true;
                }
            }

            if (Math.Round(newy) < 0)
            {
                if (MapController.Get(map.Up) != null)
                {
                    newMapId = MapController.Get(spawn.MapId).Up;
                    newy = Options.MapHeight - 1;
                }
                else
                {
                    killSpawn = true;
                }
            }

            if (Math.Round(newy) > Options.MapHeight - 1)
            {
                if (MapController.Get(map.Down) != null)
                {
                    newMapId = MapController.Get(spawn.MapId).Down;
                    newy = 0;
                }
                else
                {
                    killSpawn = true;
                }
            }

            spawn.X = newx;
            spawn.Y = newy;
            spawn.MapId = newMapId;

            return killSpawn;
        }

        public override void Die(bool dropItems = true, Entity killer = null)
        {
            for (var i = 0; i < Spawns.Length; i++)
            {
                Spawns[i] = null;
            }

            if (MapController.TryGetInstanceFromMap(MapId, MapInstanceId, out var mapInstance))
            {
                mapInstance.RemoveProjectile(this);
            }
        }

        public override EntityPacket EntityPacket(EntityPacket packet = null, Player forPlayer = null)
        {
            if (packet == null)
            {
                packet = new ProjectileEntityPacket();
            }

            packet = base.EntityPacket(packet, forPlayer);

            var pkt = (ProjectileEntityPacket) packet;
            pkt.ProjectileId = Base.Id;
            pkt.ProjectileDirection = (byte) Dir;
            pkt.TargetId = Target?.Id ?? Guid.Empty;
            pkt.OwnerId = Owner?.Id ?? Guid.Empty;

            return pkt;
        }

        public override EntityTypes GetEntityType()
        {
            return EntityTypes.Projectile;
        }

    }

}
