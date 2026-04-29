using UnityEngine;

namespace CyberDefense.Spatial
{
    public interface IQuadTreeEntity
    {
        Vector2 Position2D { get; }
        float SpatialRadius { get; }
        bool IsSpatiallyActive { get; }
    }
}
