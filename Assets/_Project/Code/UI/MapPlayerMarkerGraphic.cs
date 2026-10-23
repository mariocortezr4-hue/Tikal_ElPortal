using UnityEngine;
using UnityEngine.UI;

namespace Tikal.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapPlayerMarkerGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();float s=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height);
            Triangle(vh,s*.52f,new Color(.035f,.045f,.025f,1));Triangle(vh,s*.38f,new Color(1,.87f,.48f,1));
        }
        static void Triangle(VertexHelper vh,float s,Color c)
        {int n=vh.currentVertCount;vh.AddVert(new Vector3(0,s),c,Vector2.up);vh.AddVert(new Vector3(-s*.65f,-s*.75f),c,Vector2.zero);vh.AddVert(new Vector3(s*.65f,-s*.75f),c,Vector2.right);vh.AddTriangle(n,n+1,n+2);}
    }
}
