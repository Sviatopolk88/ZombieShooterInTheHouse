using NeoFPS;
using NeoFPS.Constants;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Enemy
{
    /// <summary>
    /// Помечает врага как органическую поверхность NeoFPS, чтобы bullet impact использовал flesh hit FX.
    /// </summary>
    public sealed class EnemyFleshSurface : BaseSurface
    {
        public override FpsSurfaceMaterial GetSurface()
        {
            return FpsSurfaceMaterial.Flesh;
        }

        public override FpsSurfaceMaterial GetSurface(RaycastHit hit)
        {
            return FpsSurfaceMaterial.Flesh;
        }

        public override FpsSurfaceMaterial GetSurface(ControllerColliderHit hit)
        {
            return FpsSurfaceMaterial.Flesh;
        }
    }
}
