using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using Tobii.EyeX.Client;

namespace Gta5EyeTracking.HomingMissiles
{
    public class HomingMissilesHelper: DisposableBase
    {
        private List<HomingMissile> _missiles;

        public HomingMissilesHelper()
        {
            _missiles = new List<HomingMissile>();
        }

        public void Launch(Entity target)
        {
            var missile = new HomingMissile(target);
            _missiles.Add(missile);
        }

        public void Launch(Vector3 targetPosition)
        {
            var missile = new HomingMissile(targetPosition);
            _missiles.Add(missile);
        }

        public void Process()
        {
            Util.PtfxRequestAsset("scr_exile2");
            foreach (var missile in _missiles)
            {
                missile.Process();
            }
            _missiles = _missiles.Where(missile => missile.Exists).ToList();
        }

        protected override void DisposeManagedResources()
        {
            foreach (var missile in _missiles)
            {
                missile.Dispose();
            }
            _missiles = null;
        }
    }
}
