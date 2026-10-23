using UnityEngine;

namespace Tikal.UI
{
    [CreateAssetMenu(menuName="Tikal/Exploration Map",fileName="LevelMap")]
    public sealed class LevelMapDefinition : ScriptableObject
    {
        [System.Serializable] public struct Area {public string id;public Rect worldXZ;}
        public string levelTitle="LEVEL 1";
        public Rect worldBounds;
        public Area[] publicAreas=System.Array.Empty<Area>();
        public Rect DrawingRect(Rect available)
        {
            float scale=Mathf.Min((available.width-20)/worldBounds.width,(available.height-20)/worldBounds.height);
            var size=worldBounds.size*Mathf.Max(0,scale);return new Rect(available.center-size*.5f,size);
        }
        public Vector2 LocalPoint(Vector3 world,Rect available)
        {
            var rect=DrawingRect(available);return rect.min+Vector2.Scale(rect.size,new Vector2(Mathf.Clamp01((world.x-worldBounds.xMin)/worldBounds.width),Mathf.Clamp01((world.z-worldBounds.yMin)/worldBounds.height)));
        }
    }
}
