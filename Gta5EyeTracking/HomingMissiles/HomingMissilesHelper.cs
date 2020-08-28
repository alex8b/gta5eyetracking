using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;

namespace Gta5EyeTracking.HomingMissiles
{
    public class HomingMissilesHelper : IDisposable
    {
        private List<HomingMissile> _missiles;
        private bool _initFlag;

        public HomingMissilesHelper()
        {
            _missiles = new List<HomingMissile>();
            _initFlag = true;
        }

        public void Launch(Entity target)
        {
            var missile = new HomingMissile(target);
            _missiles.Add(missile);
        }

        public void Launch(Vector3 targetPosition)
        {
            var missile = new HomingMissile(targetPosition);
            if (missile.Exists)
            {
                _missiles.Add(missile);
            }
            else
            {
                missile.Dispose();
            }
        }

        public void Process()
        {
            if (_initFlag)
            {
                ScriptHookExtensions.PtfxRequestAsset("scr_exile2");
                _initFlag = false;
            }

            foreach (var missile in _missiles)
            {
                missile.Process();
                if (!missile.Exists)
                {
                    missile.Dispose();
                }
            }
            _missiles = _missiles.Where(missile => missile.Exists).ToList();
        }

        public void Dispose()
        {
            foreach (var missile in _missiles)
            {
                missile.Dispose();
            }
            _missiles = null;
        }
    }
}
