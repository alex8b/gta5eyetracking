using GTA;
using GTA.Math;
using Tobii.EyeX.Client;

namespace Gta5EyeTracking.HomingMissiles
{
    public class HomingMissile: DisposableBase
    {
        private readonly Entity _target;
        private Entity _missile;
        private Vector3 _targetPosition;
        private Vector3 _launchDir;
        private double _speed;
        private bool _initialWait;
        private double _flightTime;
        private int _fxId;
        private double _flightFixCoef;
        public bool Exists { get; set; }

        public HomingMissile(Entity target)
        {
            _target = target;
            _targetPosition = _target.Position;
            var player = Game.Player.Character;
            _launchDir = (_target.Position - player.Position);
            _launchDir.Normalize();
            Exists = true;
            CreateMissileEntity();
            _initialWait = true;
            _flightFixCoef = 0;
            _speed = 50;
        }

        public HomingMissile(Vector3 targetPosition)
        {
            _targetPosition = targetPosition;
            var player = Game.Player.Character;
            _launchDir = _targetPosition - player.Position;
            _launchDir.Normalize();
            Exists = true;
            CreateMissileEntity();
            _initialWait = true;
            _flightFixCoef = 0;
            _speed = 50;
        }

        private void CreateMissileEntity()
        {
            var model = new Model("w_at_ar_supp_02");
            var position = Game.Player.Character.GetBoneCoord(Bone.SKEL_R_Hand);
            _missile = World.CreateProp(model, position, false, false);

            Util.PlaySoundFromEntity(Util.GetSoundId(), "SPL_RPG_DIST_FLIGHT_MASTER", _missile, "");
            Util.PtfxRequestAsset("scr_exile2");
            _fxId = Util.PtfxStartOnEntity(_missile, "scr_ex2_rpg_trail", "scr_exile2", new Vector3(0.56f, 0, 0), new Vector3(0, 0, -90), 1.0);
        }

        private void Detonate()
        {
            var player = Game.Player.Character;
            World.AddOwnedExplosion(player, _missile.Position, ExplosionType.Explosion1, 5, 0.1f);
            RemoveMissileEntity();
        }

        private void RemoveMissileEntity()
        {
            Util.PtfxStop(_fxId);
           
            _missile.IsVisible = false;
            _missile.Detach();
            _missile.MarkAsNoLongerNeeded();
            _missile.Position = Vector3.Zero;
            Exists = false;
        }

        private bool IsNearTarget()
        {
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

            _flightTime = _flightTime + 1;

            if (!(_flightTime > 200)) return; //start

            if (HasTimeOut()
                || HasCollided())
            {
                Detonate();
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
            _missile.Velocity = _launchDir*(float) _speed;
        }

        private bool HasTimeOut()
        {
            return (_flightTime > 20000*Util.GetTimeCoef());
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

        protected override void DisposeManagedResources()
        {
            RemoveMissileEntity();
        }
    }
}