using UnityEngine;
using UnityEngine.UI;

namespace Tikal.UI
{
    // Static Canvas decoration; no cameras, materials, animation or gameplay state.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ExpeditionMapFrame : MaskableGraphic
    {
        public enum Ornament { Panel, Compact, Compass, Rule }
        public Ornament ornament;
        static readonly Color Gold=new Color(.68f,.55f,.32f,.72f);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            if(ornament==Ornament.Compass){Compass(vh,r);return;}
            if(ornament==Ornament.Rule){Rule(vh,r);return;}
            bool small=ornament==Ornament.Compact;
            // Layered inner shadow and quiet warm grain over the existing stone sprite.
            for(int i=0;i<12;i++){float d=i*1.8f+4;Outline(vh,Inset(r,d),1.8f,new Color(.035f,.044f,.033f,.19f*(1-i/12f)));}
            int count=small?60:360;
            for(int i=0;i<count;i++)
            {
                float x=Mathf.Repeat(Mathf.Sin(i*73.19f)*43758.5f,1),y=Mathf.Repeat(Mathf.Sin(i*29.73f)*19842.3f,1);
                var p=r.min+Vector2.Scale(r.size,new Vector2(x,y));Quad(vh,new Rect(p,new Vector2(1.4f+i%3,.65f)),new Color(.69f,.62f,.43f,.065f));
            }
            Outline(vh,Inset(r,2),1.1f,Gold);Outline(vh,Inset(r,7),.65f,new Color(.57f,.49f,.32f,.36f));
            if(small)return;
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})
            {
                var p=new Vector2(x<0?r.xMin+13:r.xMax-13,y<0?r.yMin+13:r.yMax-13);
                for(int step=0;step<3;step++)
                {
                    var a=p+new Vector2(-x*step*6,-y*step*6);Line(vh,a,a+new Vector2(-x*(36-step*8),0),1.2f,Gold);Line(vh,a,a+new Vector2(0,-y*(36-step*8)),1.2f,Gold);
                }
                Diamond(vh,p+new Vector2(-x*7,-y*7),3,Gold);
            }
        }
        static Rect Inset(Rect r,float i)=>new Rect(r.x+i,r.y+i,r.width-i*2,r.height-i*2);
        static void Outline(VertexHelper vh,Rect r,float w,Color c){Line(vh,new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin),w,c);Line(vh,new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),w,c);Line(vh,new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),w,c);Line(vh,new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax),w,c);}
        static void Rule(VertexHelper vh,Rect r){var p=r.center;Line(vh,new Vector2(r.xMin,p.y),p-new Vector2(13,0),.8f,Gold);Line(vh,p+new Vector2(13,0),new Vector2(r.xMax,p.y),.8f,Gold);Diamond(vh,p,5,Gold);Diamond(vh,p,2,new Color(.18f,.20f,.14f,1));}
        static void Compass(VertexHelper vh,Rect r)
        {
            Vector2 c=r.center;float s=Mathf.Min(r.width,r.height)*.39f;
            for(int i=0;i<32;i++){float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;Line(vh,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*s*.7f,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*s*.7f,.65f,new Color(.62f,.54f,.34f,.46f));}
            for(int i=0;i<4;i++){float a=i*Mathf.PI/2;var axis=new Vector2(Mathf.Sin(a),Mathf.Cos(a));var side=new Vector2(axis.y,-axis.x);Triangle(vh,c+axis*s,c+side*s*.19f,c-axis*s*.2f,i==0?new Color(.85f,.72f,.46f,.95f):Gold);Triangle(vh,c+axis*s,c-side*s*.19f,c-axis*s*.2f,new Color(.36f,.33f,.23f,.95f));}Diamond(vh,c,s*.09f,Gold);
        }
        static void Diamond(VertexHelper vh,Vector2 p,float s,Color c){Triangle(vh,p+Vector2.up*s,p+Vector2.left*s,p+Vector2.down*s,c);Triangle(vh,p+Vector2.up*s,p+Vector2.down*s,p+Vector2.right*s,c);}
        static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color color){int n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);}
        static void Quad(VertexHelper vh,Rect r,Color c){Triangle(vh,r.min,new Vector2(r.xMin,r.yMax),r.max,c);Triangle(vh,r.min,r.max,new Vector2(r.xMax,r.yMin),c);}
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float w,Color c)=>ExplorationMapGraphic.Line(vh,a,b,w,c);
    }
}
