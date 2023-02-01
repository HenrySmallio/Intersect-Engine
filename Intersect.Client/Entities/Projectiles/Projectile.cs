using System;
using Intersect.Client.Framework.Entities;
using Intersect.Client.General;
using Intersect.Enums;
using Intersect.GameObjects;
using Intersect.GameObjects.Maps;
using Intersect.Network.Packets.Server;
using Intersect.Utilities;

namespace Intersect.Client.Entities.Projectiles
{

    public partial class Projectile : Entity
    {

        private bool mDisposing;

        private bool mLoaded;

        private object mLock = new object();

        private ProjectileBase mMyBase;

        private Guid mOwner;

        private int mQuantity;

        private int mSpawnCount;

        private int mSpawnedAmount;

        private long mSpawnTime;

        private int mTotalSpawns;

        public Guid ProjectileId;

        // Individual Spawns
        public ProjectileSpawns[] Spawns;

        public Guid TargetId;

        /// <summary>
        ///     The constructor for the inherated projectile class
        /// </summary>
        public Projectile(Guid id, ProjectileEntityPacket packet) : base(id, packet, EntityTypes.Projectile)
        {
            Vital[(int) Vitals.Health] = 1;
            MaxVital[(int) Vitals.Health] = 1;
            HideName = true;
            Passable = true;
            IsMoving = true;
        }

        public override void Load(EntityPacket packet)
        {
            if (mLoaded)
            {
                return;
            }

            base.Load(packet);
            var pkt = (ProjectileEntityPacket) packet;
            ProjectileId = pkt.ProjectileId;
            Dir = (Directions)pkt.ProjectileDirection;
            TargetId = pkt.TargetId;
            mOwner = pkt.OwnerId;
            mMyBase = ProjectileBase.Get(ProjectileId);
            if (mMyBase != null)
            {
                for (var x = 0; x < ProjectileBase.SPAWN_LOCATIONS_WIDTH; x++)
                {
                    for (var y = 0; y < ProjectileBase.SPAWN_LOCATIONS_WIDTH; y++)
                    {
                        for (var d = 0; d < ProjectileBase.MAX_PROJECTILE_DIRECTIONS; d++)
                        {
                            if (mMyBase.SpawnLocations[x, y].Directions[d] == true)
                            {
                                mTotalSpawns++;
                            }
                        }
                    }
                }

                mTotalSpawns *= mMyBase.Quantity;
            }

            Spawns = new ProjectileSpawns[mTotalSpawns];
            mLoaded = true;
        }

        public override void Dispose()
        {
            if (!mDisposing)
            {
                lock (mLock)
                {
                    mDisposing = true;
                    if (mSpawnedAmount == 0)
                    {
                        Update();
                    }

                    if (Spawns != null)
                    {
                        foreach (var s in Spawns)
                        {
                            if (s != null && s.Anim != null)
                            {
                                s.Anim.DisposeNextDraw();
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public override bool CanBeAttacked
        {
            get
            {
                return false;
            }
        }

        //Find out which animation data to load depending on what spawn wave we are on during projection.
        private int FindSpawnAnimationData()
        {
            var start = 0;
            var end = 1;
            for (var i = 0; i < mMyBase.Animations.Count; i++)
            {
                end = mMyBase.Animations[i].SpawnRange;
                if (mQuantity >= start && mQuantity < end)
                {
                    return i;
                }

                start = end;
            }

            //If reaches maximum and the developer(s) have fucked up the animation ranges on each spawn of projectiles somewhere, just assign it to the last animation state.
            return mMyBase.Animations.Count - 1;
        }

        private void AddProjectileSpawns()
        {
            var spawn = FindSpawnAnimationData();
            var animBase = AnimationBase.Get(mMyBase.Animations[spawn].AnimationId);

            for (var x = 0; x < ProjectileBase.SPAWN_LOCATIONS_WIDTH; x++)
            {
                for (var y = 0; y < ProjectileBase.SPAWN_LOCATIONS_WIDTH; y++)
                {
                    for (var d = 0; d < ProjectileBase.MAX_PROJECTILE_DIRECTIONS; d++)
                    {
                        if (mMyBase.SpawnLocations[x, y].Directions[d] == true)
                        {
                            var s = new ProjectileSpawns(
                                (byte)FindProjectileRotationDir(Dir, (Directions)d),
                                (byte)(X + FindProjectileRotationX(Dir, x - 2, y - 2)),
                                (byte)(Y + FindProjectileRotationY(Dir, x - 2, y - 2)), Z, MapId, animBase,
                                mMyBase.Animations[spawn].AutoRotate, mMyBase, this
                            );

                            Spawns[mSpawnedAmount] = s;
                            if (Collided(mSpawnedAmount))
                            {
                                Spawns[mSpawnedAmount].Dispose();
                                Spawns[mSpawnedAmount] = null;
                                mSpawnCount--;
                            }

                            mSpawnedAmount++;
                            mSpawnCount++;
                        }
                    }
                }
            }

            mQuantity++;
            mSpawnTime = Timing.Global.Milliseconds + mMyBase.Delay;
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

        /// <summary>
        ///     Gets the displacement of the projectile during projection
        /// </summary>
        /// <returns>The displacement from the co-ordinates if placed on a Options.TileHeight grid.</returns>
        private float GetDisplacement(long spawnTime)
        {
            var elapsedTime = Timing.Global.Milliseconds - spawnTime;
            var displacementPercent = elapsedTime / (float) mMyBase.Speed;

            return displacementPercent * Options.TileHeight * mMyBase.Range;
        }

        /// <summary>
        ///     Overwrite updating the offsets for projectile movement.
        /// </summary>
        public override bool Update()
        {
            if (mMyBase == null)
            {
                return false;
            }

            lock (mLock)
            {
                var tmpI = -1;
                var map = MapId;
                var y = Y;

                if (!mDisposing && mQuantity < mMyBase.Quantity && mSpawnTime < Timing.Global.Milliseconds)
                {
                    AddProjectileSpawns();
                }

                if (IsMoving)
                {
                    for (var s = 0; s < mSpawnedAmount; s++)
                    {
                        if (Spawns[s] != null && Maps.MapInstance.Get(Spawns[s].SpawnMapId) != null)
                        {
                            Spawns[s].OffsetX = GetRangeX((Directions)Spawns[s].Dir, GetDisplacement(Spawns[s].SpawnTime));
                            Spawns[s].OffsetY = GetRangeY((Directions)Spawns[s].Dir, GetDisplacement(Spawns[s].SpawnTime));
                            Spawns[s]
                                .Anim.SetPosition(
                                    Maps.MapInstance.Get(Spawns[s].SpawnMapId).GetX() +
                                    Spawns[s].SpawnX * Options.TileWidth +
                                    Spawns[s].OffsetX +
                                    Options.TileWidth / 2,
                                    Maps.MapInstance.Get(Spawns[s].SpawnMapId).GetY() +
                                    Spawns[s].SpawnY * Options.TileHeight +
                                    Spawns[s].OffsetY +
                                    Options.TileHeight / 2, X, Y, MapId, Spawns[s].AutoRotate ? (Directions)Spawns[s].Dir : 0,
                                    Spawns[s].Z
                                );

                            Spawns[s].Anim.Update();
                        }
                    }
                }

                CheckForCollision();
            }

            return true;
        }

        public void CheckForCollision()
        {
            if (mSpawnCount != 0 || mQuantity < mMyBase.Quantity)
            {
                for (var i = 0; i < mSpawnedAmount; i++)
                {
                    if (Spawns[i] != null && Timing.Global.Milliseconds > Spawns[i].TransmittionTimer)
                    {
                        var spawnMap = Maps.MapInstance.Get(Spawns[i].MapId);
                        if (spawnMap != null)
                        {
                            var newx = Spawns[i].X + (int) GetRangeX((Directions)Spawns[i].Dir, 1);
                            var newy = Spawns[i].Y + (int) GetRangeY((Directions)Spawns[i].Dir, 1);
                            var newMapId = Spawns[i].MapId;
                            var killSpawn = false;

                            Spawns[i].Distance++;

                            if (newx < 0)
                            {
                                if (Maps.MapInstance.Get(spawnMap.Left) != null)
                                {
                                    newMapId = spawnMap.Left;
                                    newx = Options.MapWidth - 1;
                                }
                                else
                                {
                                    killSpawn = true;
                                }
                            }

                            if (newx > Options.MapWidth - 1)
                            {
                                if (Maps.MapInstance.Get(spawnMap.Right) != null)
                                {
                                    newMapId = spawnMap.Right;
                                    newx = 0;
                                }
                                else
                                {
                                    killSpawn = true;
                                }
                            }

                            if (newy < 0)
                            {
                                if (Maps.MapInstance.Get(spawnMap.Up) != null)
                                {
                                    newMapId = spawnMap.Up;
                                    newy = Options.MapHeight - 1;
                                }
                                else
                                {
                                    killSpawn = true;
                                }
                            }

                            if (newy > Options.MapHeight - 1)
                            {
                                if (Maps.MapInstance.Get(spawnMap.Down) != null)
                                {
                                    newMapId = spawnMap.Down;
                                    newy = 0;
                                }
                                else
                                {
                                    killSpawn = true;
                                }
                            }

                            if (killSpawn)
                            {
                                Spawns[i].Dispose();
                                Spawns[i] = null;
                                mSpawnCount--;

                                continue;
                            }

                            Spawns[i].X = newx;
                            Spawns[i].Y = newy;
                            Spawns[i].MapId = newMapId;
                            var newMap = Maps.MapInstance.Get(newMapId);

                            //Check for Z-Dimension
                            if (newMap.Attributes[Spawns[i].X, Spawns[i].Y] != null)
                            {
                                if (newMap.Attributes[Spawns[i].X, Spawns[i].Y].Type == MapAttributes.ZDimension)
                                {
                                    if (((MapZDimensionAttribute) newMap.Attributes[Spawns[i].X, Spawns[i].Y])
                                        .GatewayTo >
                                        0)
                                    {
                                        Spawns[i].Z =
                                            ((MapZDimensionAttribute) newMap.Attributes[Spawns[i].X, Spawns[i].Y])
                                            .GatewayTo -
                                            1;
                                    }
                                }
                            }

                            if (killSpawn == false)
                            {
                                killSpawn = Collided(i);
                            }

                            Spawns[i].TransmittionTimer = Timing.Global.Milliseconds +
                                                          (long) (mMyBase.Speed / (float) mMyBase.Range);

                            if (Spawns[i].Distance >= mMyBase.Range)
                            {
                                killSpawn = true;
                            }

                            if (killSpawn)
                            {
                                Spawns[i].Dispose();
                                Spawns[i] = null;
                                mSpawnCount--;
                            }
                        }
                    }
                }
            }
            else
            {
                Globals.Entities[Id].Dispose();
            }
        }

        private bool Collided(int i)
        {
            var killSpawn = false;
            IEntity blockedBy = null;
            var tileBlocked = Globals.Me.IsTileBlocked(
                Spawns[i].X, Spawns[i].Y, Z, Spawns[i].MapId, ref blockedBy,
                Spawns[i].ProjectileBase.IgnoreActiveResources, Spawns[i].ProjectileBase.IgnoreExhaustedResources, true, true
            );

            if (tileBlocked != -1)
            {
                if (tileBlocked == -6 &&
                    blockedBy != null &&
                    blockedBy.Id != mOwner &&
                    Globals.Entities.ContainsKey(blockedBy.Id))
                {
                    if (blockedBy is Resource)
                    {
                        killSpawn = true;
                    }
                }
                else
                {
                    if (tileBlocked == -2)
                    {
                        if (!Spawns[i].ProjectileBase.IgnoreMapBlocks)
                        {
                            killSpawn = true;
                        }
                    }
                    else if (tileBlocked == -3)
                    {
                        if (!Spawns[i].ProjectileBase.IgnoreZDimension)
                        {
                            killSpawn = true;
                        }
                    }
                    else if (tileBlocked == -5)
                    {
                        killSpawn = true;
                    }
                }
            }

            return killSpawn;
        }

        /// <summary>
        ///     Rendering all of the individual projectiles from a singular spawn to a map.
        /// </summary>
        public override void Draw()
        {
            if (Maps.MapInstance.Get(MapId) == null || !Globals.GridMaps.Contains(MapId))
            {
                return;
            }
        }

        public void SpawnDead(int spawnIndex)
        {
            if (spawnIndex < mSpawnedAmount && Spawns[spawnIndex] != null)
            {
                Spawns[spawnIndex].Dispose();
                Spawns[spawnIndex] = null;
            }
        }

    }

}
