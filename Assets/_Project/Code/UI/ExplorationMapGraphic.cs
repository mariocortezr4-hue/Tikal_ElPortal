using UnityEngine;
using UnityEngine.UI;

namespace Tikal.UI
{
    // Authored public floor silhouettes only. No world camera, hidden-object scan or clue state.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ExplorationMapGraphic : MaskableGraphic
    {
        public LevelMapDefinition definition;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(definition==null||definition.worldBounds.width<=0||definition.worldBounds.height<=0)return;
            var drawing=definition.DrawingRect(rectTransform.rect);float scale=drawing.width/definition.worldBounds.width;
            foreach(var area in definition.publicAreas)
            {
                var world=area.worldXZ;var point=definition.LocalPoint(new Vector3(world.xMin,0,world.yMin),rectTransform.rect);var size=world.size*scale;var rect=new Rect(point,size);
                Quad(vh,rect,new Color(.43f,.37f,.22f,.9f));float inset=Mathf.Min(1.2f,Mathf.Min(rect.width,rect.height)*.15f);rect.xMin+=inset;rect.yMin+=inset;rect.xMax-=inset;rect.yMax-=inset;Quad(vh,rect,new Color(.12f,.2f,.15f,.98f));
            }
        }
        static void Quad(VertexHelper vh,Rect r,Color color)
        {int n=vh.currentVertCount;vh.AddVert(new Vector3(r.xMin,r.yMin),color,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),color,Vector2.up);vh.AddVert(new Vector3(r.xMax,r.yMax),color,Vector2.one);vh.AddVert(new Vector3(r.xMax,r.yMin),color,Vector2.right);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}
