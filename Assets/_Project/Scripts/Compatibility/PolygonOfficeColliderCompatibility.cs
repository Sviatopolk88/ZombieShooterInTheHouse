using System;
using UnityEngine;

namespace _Project.Scripts.Compatibility
{
    /// <summary>
    /// Совместимость для старого компонента PolygonOffice, который больше не поставляется с пакетом.
    /// Хранит сериализованные данные prefab и не выполняет runtime-логики.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PolygonOfficeColliderCompatibility : MonoBehaviour
    {
        [Serializable]
        private struct ColliderParams
        {
            public int maxConvexHulls;
            public int resolution;
            public float concavity;
            public int planeDownsampling;
            public int convexhullApproximation;
            public float alpha;
            public float beta;
            public int pca;
            public int mode;
            public int maxNumVerticesPerCH;
            public float minVolumePerCH;
        }

        [SerializeField] private ColliderParams Params;
        [SerializeField] private Collider[] m_colliders;
        [SerializeField] private bool m_isTrigger;
        [SerializeField] private PhysicsMaterial m_material;
#pragma warning disable CS0414
        [SerializeField] private bool m_showColliders = true;
#pragma warning restore CS0414
        [SerializeField] private ScriptableObject m_colliderAsset;
    }
}
