using UnityEngine;
using UnityEngine.UI;

namespace Tikal.UI
{
    // Presentation only. Projection and public map data remain owned by LevelMapDefinition.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ExplorationMapGraphic : MaskableGraphic
    {
        public LevelMapDefinition definition;
        public Transform playerRoot;
        int _current=-1;
        public int CurrentAreaIndex=>_current;
        void LateUpdate()
        {
            int next=-1;float smallest=float.MaxValue;
            if(definition!=null&&playerRoot!=null)
                for(int i=0;i<definition.publicAreas.Length;i++)
                {
                    var r=definition.publicAreas[i].worldXZ;
                    if(r.Contains(new Vector2(playerRoot.position.x,playerRoot.position.z))&&r.width*r.height<smallest)
                    {next=i;smallest=r.width*r.height;}
                }
            if(next!=_current){_current=next;SetVerticesDirty();}
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(definition==null||definition.worldBounds.width<=0||definition.worldBounds.height<=0)return;
            float scale=definition.DrawingRect(rectTransform.rect).width/definition.worldBounds.width;
            // Paths first, so connecting strokes tuck beneath the ruin silhouettes.
            for(int pass=0;pass<2;pass++)for(int i=0;i<definition.publicAreas.Length;i++)
            {
                var area=definition.publicAreas[i];var world=area.worldXZ;
                bool path=world.width/world.height>2||world.height/world.width>2;
                if(path!=(pass==0))continue;
                var point=definition.LocalPoint(new Vector3(world.xMin,0,world.yMin),rectTransform.rect);
                var r=new Rect(point,world.size*scale);bool active=i==_current;
                if(path)Path(vh,r,i,active);else Area(vh,r,i,active);
            }
        }
        static float Noise(int i,int seed)=>Mathf.Repeat(Mathf.Sin(i*12.9898f+seed*78.233f)*43758.5453f,1);
        static Vector2[] Contour(Rect r,int seed,float inset)
        {
            r.xMin+=inset;r.yMin+=inset;r.xMax-=inset;r.yMax-=inset;
            var points=new Vector2[32];float bevel=Mathf.Min(r.width,r.height)*.11f;
            for(int i=0;i<32;i++)
            {
                int edge=i/8;float t=(i%8)/8f;float wobble=(Noise(i,seed)-.5f)*Mathf.Min(2.4f,bevel*.38f);
                switch(edge)
                {
                    case 0:points[i]=new Vector2(Mathf.Lerp(r.xMin+bevel,r.xMax-bevel,t),r.yMin+wobble);break;
                    case 1:points[i]=new Vector2(r.xMax+wobble,Mathf.Lerp(r.yMin+bevel,r.yMax-bevel,t));break;
                    case 2:points[i]=new Vector2(Mathf.Lerp(r.xMax-bevel,r.xMin+bevel,t),r.yMax+wobble);break;
                    default:points[i]=new Vector2(r.xMin+wobble,Mathf.Lerp(r.yMax-bevel,r.yMin+bevel,t));break;
                }
            }
            return points;
        }
        static void Area(VertexHelper vh,Rect r,int seed,bool active)
        {
            var outer=Contour(r,seed,0);var inner=Contour(r,seed,Mathf.Min(1.6f,r.width*.05f));
            Fill(vh,outer,r.center,new Color(.10f,.13f,.10f,.94f));
            Fill(vh,inner,r.center,active?new Color(.27f,.29f,.18f,.95f):new Color(.18f,.22f,.17f,.92f));
            for(int i=0;i<outer.Length;i++)
            {
                if(i%7!=3)Line(vh,outer[i],outer[(i+1)%outer.Length],active?1.5f:.85f,active?new Color(.80f,.65f,.37f,.95f):new Color(.53f,.47f,.31f,.72f));
            }
            // Quiet masonry hatching: ornamental marks, never puzzle symbols.
            for(int i=0;i<14;i++)
            {
                var p=new Vector2(Mathf.Lerp(r.xMin+r.width*.18f,r.xMax-r.width*.18f,Noise(i,seed+2)),Mathf.Lerp(r.yMin+r.height*.18f,r.yMax-r.height*.18f,Noise(i,seed+8)));
                float len=Mathf.Min(10,r.width*.12f);Line(vh,p,p+new Vector2(len,0),.6f,new Color(.64f,.61f,.43f,.16f));
                if(i%3==0)Line(vh,p,p+new Vector2(0,Mathf.Min(5,r.height*.06f)),.6f,new Color(.64f,.61f,.43f,.12f));
            }
            var inside=Contour(r,seed,Mathf.Min(r.width,r.height)*.12f);
            for(int i=0;i<inside.Length;i++)if(i%4!=0)Line(vh,inside[i],inside[(i+1)%inside.Length],.5f,new Color(.66f,.60f,.38f,.16f));
        }
        static void Path(VertexHelper vh,Rect r,int seed,bool active)
        {
            bool horizontal=r.width>r.height;float length=horizontal?r.width:r.height,width=horizontal?r.height:r.width;
            Vector2 a=horizontal?new Vector2(r.xMin,r.center.y):new Vector2(r.center.x,r.yMin);
            Vector2 axis=horizontal?Vector2.right:Vector2.up,side=horizontal?Vector2.up:Vector2.right;
            for(int j=0;j<12;j++)
            {
                float t=j/12f,u=(j+1)/12f;
                Vector2 p=a+axis*(length*t)+side*((Noise(j,seed)-.5f)*width*.16f),q=a+axis*(length*u)+side*((Noise(j+1,seed)-.5f)*width*.16f);
                Line(vh,p,q,width*.95f,new Color(.33f,.34f,.24f,.18f));Line(vh,p,q,width*.68f,new Color(.49f,.46f,.31f,.30f));
                Line(vh,p,q,Mathf.Max(1,width*.3f),active?new Color(.79f,.68f,.44f,.86f):new Color(.69f,.64f,.47f,.7f));
                if(j%3!=1)Line(vh,p,q,.7f,new Color(.89f,.80f,.57f,.65f));
            }
        }
        internal static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int v=vh.currentVertCount;vh.AddVert(a-n,c,Vector2.zero);vh.AddVert(a+n,c,Vector2.zero);vh.AddVert(b+n,c,Vector2.zero);vh.AddVert(b-n,c,Vector2.zero);vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);}
        static void Fill(VertexHelper vh,Vector2[] p,Vector2 center,Color c)
        {int n=vh.currentVertCount;vh.AddVert(center,c,Vector2.zero);foreach(var v in p)vh.AddVert(v,c,Vector2.zero);for(int i=0;i<p.Length;i++)vh.AddTriangle(n,n+1+i,n+1+(i+1)%p.Length);}
    }
}
