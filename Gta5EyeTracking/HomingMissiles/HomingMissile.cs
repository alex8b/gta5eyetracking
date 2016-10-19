using System;
using GTA;
using GTA.Math;

namespace Gta5EyeTracking.HomingMissiles
{
    public class HomingMissile: IDisposable
    {
        public bool Exists { get; private set; }
        public bool Detonated { get; private set; }

        private readonly Entity _target;
        private Entity _missile;
        private Vector3 _targetPosition;
        private Vector3 _launchDir;
        private double _speed;
        private bool _initialWait;
        private double _flightTime;
        private int _fxId;
        private double _flightFixCoef;
        private TimeSpan _timeout;
        private TimeSpan _initTime;
        private TimeSpan _waitAfterDetonationTime;
        private double _detonatedTime;
        private int _soundId;

        public HomingMissile(Entity target)
        {
            _target = target;
            _targetPosition = _target.Position;
            var player = Game.Player.Character;
            _launchDir = (_target.Position - player.Position);
            _launchDir.Normalize();
            Init();
        }

        public HomingMissile(Vector3 targetPosition)
        {
            _targetPosition = targetPosition;
            var player = Game.Player.Character;
            _launchDir = _targetPosition - player.Position;
            _targetPosition = player.Position + _launchDir*10000;
            _launchDir.Normalize();
            Init();
        }

        private void Init()
        {
            Exists = true;
            CreateMissileEntity();
            _initialWait = true;
            _flightFixCoef = 0;
            _speed = 100;
            _timeout = TimeSpan.FromSeconds(15);
            _initTime = TimeSpan.FromSeconds(0.25);
            _waitAfterDetonationTime = TimeSpan.FromSeconds(1.75);
        }

        private void CreateMissileEntity()
        {
            var model = new Model("w_at_ar_supp_02");
            var playerCharacter = Game.Player.Character;
            if (playerCharacter == null) return;
            var position = playerCharacter.GetBoneCoord(Bone.SKEL_R_Hand) + _launchDir * 1f;
            if (playerCharacter.IsInVehicle())
            {
                position = playerCharacter.GetBoneCoord(Bone.SKEL_R_Hand) + _launchDir * 2f;
            }
            if (playerCharacter.IsInPlane)
            {
                position = playerCharacter.GetBoneCoord(Bone.SKEL_R_Hand) + _launchDir * 4f;
            }
            _missile = World.CreateProp(model, position, false, false);
            if (_missile == null)
            {
                Debug.Log("Failed to create a missile prop");
                Exists = false;
                return;
            }
            GTA.Native.Function.Call(GTA.Native.Hash.SET_ENTITY_RECORDS_COLLISIONS, _missile, false);
            _soundId = ScriptHookExtensions.GetSoundId();
            ScriptHookExtensions.PlaySoundFromEntity(_soundId, "SPL_RPG_DIST_FLIGHT_MASTER", _missile, "");
            ScriptHookExtensions.PtfxRequestAsset("scr_exile2");
            _fxId = ScriptHookExtensions.PtfxStartOnEntity(_missile, "scr_ex2_rpg_trail", "scr_exile2", new Vector3(0.56f, 0, 0),
                new Vector3(0, 0, -90), 1.0);
            UpdatePosition();
        }

        private void Detonate()
        {
            if (_missile == null) return;

            if (Exists && !Detonated)
            {
                var player = Game.Player.Character;
                var dist = (player.Position - _missile.Position).Length();
                if (dist > 1.5)
                {
                    World.AddOwnedExplosion(player, _missile.Position, ExplosionType.Rocket, 1.5f, 0.1f);
                    ScriptHookExtensions.PtfxStop(_fxId);
                    ScriptHookExtensions.StopSound(_soundId);
                    Detonated = true;
                }              
            }
            
        }

        private void RemoveMissileEntity()
        {
            ScriptHookExtensions.PtfxStop(_fxId);
            if (_missile != null)
            {
                _missile.IsVisible = false;
                _missile.Detach();
                _missile.MarkAsNoLongerNeeded();
                _missile.Position = Vector3.Zero;                
            }

            Exists = false;
        }

        private bool IsNearTarget()
        {
            if (_target == null) return false;
            if (_missile == null) return false;

            double tmpDist = _missile.Position.DistanceTo(_targetPosition - _launchDir * 0.25f);
            double tmpDistAhead = _missile.Position.DistanceTo(_targetPosition + _launchDir * 0.25f);

            return ((tmpDist > tmpDistAhead) || (tmpDist < 0.5));
        }

        public void Process()
        {
            UpdateTargetPosition();

            if (IsNearTarget())
            {
                Detonate();
            }

            UpdatePosition();

            _flightTime = _flightTime + 0.05;

            if (_flightTime < _initTime.TotalSeconds) return; //start

            if (HasTimeOut())
            {
                RemoveMissileEntity(); 
            }

            if (HasCollided())
            {
                Detonate();
            }

            if (Detonated)
            {
                _detonatedTime = _detonatedTime  + 0.05;
            }

            if (_detonatedTime > _waitAfterDetonationTime.TotalSeconds)
            {
                RemoveMissileEntity(); 
            }
        }

        private void UpdateTargetPosition()
        {
            if (_target != null
                && _target.Exists())
            {
                var ped = _target as Ped;
                _targetPosition = ped != null ? ped.GetBoneCoord(Bone.SKEL_Spine0) : _target.Position;
            }
        }

        private void UpdatePosition()
        {
            if (_missile == null) return;

            var flightDir = _targetPosition - _missile.Position;
            flightDir.Normalize();
            _launchDir += (flightDir - _launchDir)*(float) _flightFixCoef;

            if (_flightFixCoef < 1)
            {
                _flightFixCoef += 0.025;
            }

            var tmpRot = Geometry.DirectionToRotation(_launchDir);
            tmpRot.Z += 90;

            _missile.Rotation = tmpRot;

            if (Detonated)
            {
                _missile.Velocity = Vector3.Zero;
            }
            else
            {
                _missile.Velocity = _launchDir * (float)_speed;
            }
        }

        private bool HasTimeOut()
        {
            return (_flightTime > _timeout.TotalSeconds * ScriptHookExtensions.GetTimeCoef());
        }

        private bool HasCollided()
        {
            if (_initialWait)
            {
                _initialWait = false;

                GTA.Native.Function.Call(GTA.Native.Hash.SET_ENTITY_RECORDS_COLLISIONS, _missile, false);
                GTA.Native.Function.Call(GTA.Native.Hash.SET_ENTITY_RECORDS_COLLISIONS, _missile, true);
            }
            
            return ((GTA.Native.Function.Call<bool>(GTA.Native.Hash.HAS_ENTITY_COLLIDED_WITH_ANYTHING, _missile)));
        }

        public void Dispose()
        {
            RemoveMissileEntity();
        }
    }
}