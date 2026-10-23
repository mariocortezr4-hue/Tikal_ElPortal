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
            // Static halo and chiseled arrow. Rotation/position remain controlled by the approved HUD.
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;int n=vh.currentVertCount;
                vh.AddVert(Vector3.zero,new Color(.87f,.69f,.34f,.18f),Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Cos(a),Mathf.Sin(a))*s*.72f,new Color(.87f,.69f,.34f,0),Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Cos(b),Mathf.Sin(b))*s*.72f,new Color(.87f,.69f,.34f,0),Vector2.zero);vh.AddTriangle(n,n+1,n+2);
            }
            Arrow(vh,s*.53f,new Color(.07f,.08f,.055f,1));Arrow(vh,s*.41f,new Color(.91f,.77f,.47f,1));
            int v=vh.currentVertCount;vh.AddVert(new Vector3(0,s*.41f),new Color(.98f,.9f,.69f,1),Vector2.zero);vh.AddVert(Vector3.zero,new Color(.98f,.9f,.69f,1),Vector2.zero);vh.AddVert(new Vector3(-s*.19f,-s*.21f),new Color(.74f,.59f,.33f,1),Vector2.zero);vh.AddTriangle(v,v+1,v+2);
        }
        static void Arrow(VertexHelper vh,float s,Color c)
        {int n=vh.currentVertCount;vh.AddVert(new Vector3(0,s),c,Vector2.zero);vh.AddVert(new Vector3(-s*.68f,-s*.72f),c,Vector2.zero);vh.AddVert(new Vector3(0,-s*.38f),c,Vector2.zero);vh.AddVert(new Vector3(s*.68f,-s*.72f),c,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}
