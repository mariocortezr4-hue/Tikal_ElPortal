using System.Linq;
using Tikal.UI;
using Tikal.Player;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    // Idempotent visual upgrade; no input/modal/data authoring or environment changes.
    public static class Level01MapHUDStyling
    {
        static readonly Color Gold=new Color(.72f,.59f,.36f),Cream=new Color(.84f,.81f,.70f);
        public static void Apply(LevelMapHUD hud)
        {
            var root=hud.transform;var small=root.Find("Minimap and hints");var stone=small.Find("Minimap stone");var overlay=root.Find("Expanded map overlay");var card=overlay.Find("Map stone card");
            overlay.GetComponent<Image>().color=new Color(.025f,.033f,.027f,.70f);
            foreach(var panel in new[]{stone,card})
            {
                panel.GetComponent<Image>().color=new Color(.095f,.11f,.09f,.94f);
                var outline=panel.GetComponent<Outline>();if(outline!=null)outline.enabled=false;
                var decor=Decoration("Expedition frame",panel,panel==stone?ExpeditionMapFrame.Ornament.Compact:ExpeditionMapFrame.Ornament.Panel);
                decor.transform.SetAsFirstSibling();
            }
            var miniTitle=stone.Find("Minimap title").GetComponent<TMP_Text>();miniTitle.text="EXPEDICIÓN";miniTitle.fontSize=13;miniTitle.characterSpacing=3;miniTitle.color=Cream;
            var north=stone.Find("North").GetComponent<TMP_Text>();north.fontSize=13;north.color=Gold;
            var mapHint=small.Find("Map binding hint").GetComponent<TMP_Text>();var journalHint=small.Find("Journal binding hint").GetComponent<TMP_Text>();
            Fixed(mapHint.rectTransform,new Vector2(0,-237),new Vector2(224,25));Fixed(journalHint.rectTransform,new Vector2(0,-267),new Vector2(224,25));
            foreach(var hint in new[]{mapHint,journalHint}){hint.fontSize=16;hint.characterSpacing=1;hint.color=Cream;hint.richText=true;if(hint.GetComponent<MapHintTypography>()==null)hint.gameObject.AddComponent<MapHintTypography>();}
            small.GetComponent<RectTransform>().sizeDelta=new Vector2(224,300);
            var heading=card.Find("Map heading").GetComponent<TMP_Text>();heading.text="LA ENTRADA SECRETA";heading.fontSize=34;heading.characterSpacing=5;heading.color=Gold;Top(heading.rectTransform,53,49);
            var level=Label("Expedition level",card,"NIVEL 1",14,Gold);level.characterSpacing=5;Top(level.rectTransform,29,22);
            var subtitle=Label("Expedition subtitle",card,"MAPA",13,Cream);subtitle.characterSpacing=6;Top(subtitle.rectTransform,113,24);
            var rule=Decoration("Heading rule",card,ExpeditionMapFrame.Ornament.Rule);Top(rule.rectTransform,102,12);rule.rectTransform.anchorMin=new Vector2(.32f,1);rule.rectTransform.anchorMax=new Vector2(.68f,1);
            var large=card.Find("Public map").GetComponent<RectTransform>();large.offsetMin=new Vector2(50,105);large.offsetMax=new Vector2(-50,-151);
            var compass=Decoration("North compass",card,ExpeditionMapFrame.Ornament.Compass);var cr=compass.rectTransform;cr.anchorMin=cr.anchorMax=new Vector2(.88f,.76f);cr.pivot=new Vector2(.5f,.5f);cr.sizeDelta=new Vector2(66,66);cr.anchoredPosition=Vector2.zero;
            var n=Label("Compass north",card,"N",15,Gold);n.characterSpacing=2;var nr=n.rectTransform;nr.anchorMin=nr.anchorMax=new Vector2(.88f,.76f);nr.pivot=new Vector2(.5f,0);nr.sizeDelta=new Vector2(40,24);nr.anchoredPosition=new Vector2(0,35);
            var note=Label("Expedition note",card,"TIKAL  /  PETÉN",12,new Color(.62f,.60f,.46f));note.characterSpacing=3;var r=note.rectTransform;r.anchorMin=new Vector2(.07f,.47f);r.anchorMax=new Vector2(.27f,.53f);r.offsetMin=r.offsetMax=Vector2.zero;
            var legend=card.Find("Map legend").GetComponent<TMP_Text>();legend.text="TU POSICIÓN";legend.fontSize=12;legend.characterSpacing=2;legend.color=new Color(.70f,.67f,.53f);Bottom(legend.rectTransform,65,22);
            var dot=Decoration("Legend diamond",card,ExpeditionMapFrame.Ornament.Compass);var dr=dot.rectTransform;dr.anchorMin=dr.anchorMax=new Vector2(.5f,0);dr.sizeDelta=new Vector2(18,18);dr.anchoredPosition=new Vector2(-80,76);
            var separator=Decoration("Footer rule",card,ExpeditionMapFrame.Ornament.Rule);Bottom(separator.rectTransform,53,10);
            var footer=card.Find("Close hint").GetComponent<TMP_Text>();Bottom(footer.rectTransform,18,29);footer.fontSize=17;footer.characterSpacing=1;footer.color=Cream;footer.richText=true;if(footer.GetComponent<MapHintTypography>()==null)footer.gameObject.AddComponent<MapHintTypography>();
            var player=Level01MapHUDAuthoring.All<PlayerController>().Single().transform;
            foreach(var g in root.GetComponentsInChildren<ExplorationMapGraphic>(true)){g.playerRoot=player;EditorUtility.SetDirty(g);}
            foreach(var m in root.GetComponentsInChildren<MapPlayerMarkerGraphic>(true))m.rectTransform.sizeDelta=Vector2.one*(m.transform.parent==large?25:17);
            foreach(var t in root.GetComponentsInChildren<TMP_Text>(true)){t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Ellipsis;t.raycastTarget=false;EditorUtility.SetDirty(t);}
        }
        static RectTransform Ensure(string name,Transform parent)
        {var t=parent.Find(name);if(t!=null)return t.GetComponent<RectTransform>();var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        static ExpeditionMapFrame Decoration(string name,Transform parent,ExpeditionMapFrame.Ornament kind)
        {var r=Ensure(name,parent);if(r.GetComponent<CanvasRenderer>()==null)r.gameObject.AddComponent<CanvasRenderer>();var g=r.GetComponent<ExpeditionMapFrame>()??r.gameObject.AddComponent<ExpeditionMapFrame>();g.ornament=kind;g.raycastTarget=false;EditorUtility.SetDirty(g);return g;}
        static TMP_Text Label(string name,Transform parent,string text,int size,Color color)
        {var r=Ensure(name,parent);var t=r.GetComponent<TMP_Text>()??r.gameObject.AddComponent<TextMeshProUGUI>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/InteractionPromptFont.asset");t.text=text;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.Center;return t;}
        static void Top(RectTransform r,float y,float h){r.anchorMin=new Vector2(.06f,1);r.anchorMax=new Vector2(.94f,1);r.pivot=new Vector2(.5f,1);r.sizeDelta=new Vector2(0,h);r.anchoredPosition=new Vector2(0,-y);}
        static void Bottom(RectTransform r,float y,float h){r.anchorMin=new Vector2(.06f,0);r.anchorMax=new Vector2(.94f,0);r.pivot=new Vector2(.5f,0);r.sizeDelta=new Vector2(0,h);r.anchoredPosition=new Vector2(0,y);}
        static void Fixed(RectTransform r,Vector2 p,Vector2 size){r.anchoredPosition=p;r.sizeDelta=size;}
    }
}
