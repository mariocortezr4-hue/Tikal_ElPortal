using System.Linq;
using Tikal.Interaction;
using Tikal.UI;
using UnityEditor;
using UnityEngine;

namespace Tikal.Tests.Editor
{
    // Editor-only, opt-in Scene gizmos. No component or technical UI enters the build.
    public static class ReticleAimDebugGizmos
    {
        const string MenuPath="Tikal/Debug/Reticle Camera Aim Gizmos",Key="Tikal.ReticleAimGizmos";
        [MenuItem(MenuPath)] static void Toggle(){SessionState.SetBool(Key,!SessionState.GetBool(Key,false));SceneView.RepaintAll();}
        [MenuItem(MenuPath,true)] static bool MenuState(){UnityEditor.Menu.SetChecked(MenuPath,SessionState.GetBool(Key,false));return true;}
        [DrawGizmo(GizmoType.Selected|GizmoType.NonSelected)]
        static void Draw(PlayerInteraction interaction,GizmoType type)
        {
            if(!SessionState.GetBool(Key,false)||interaction.ViewCamera==null||interaction.InteractionOrigin==null)return;
            var camera=interaction.ViewCamera;var ray=camera.ViewportPointToRay(new Vector3(.5f,.5f,0));
            int mask=new SerializedObject(interaction).FindProperty("_interactionLayers").intValue;
            var hit=Physics.RaycastAll(ray,30,mask,QueryTriggerInteraction.Ignore).Where(h=>!h.transform.IsChildOf(interaction.transform)).OrderBy(h=>h.distance).FirstOrDefault();var aim=hit.collider!=null?hit.point:ray.GetPoint(30);
            Gizmos.color=Color.cyan;Gizmos.DrawLine(ray.origin,aim);Gizmos.color=Color.yellow;Gizmos.DrawWireSphere(aim,.06f);Handles.Label(aim,"Camera AimPoint");
            var physical=Application.isPlaying&&interaction.HasResolvedAimPoint?interaction.ResolvedAimPoint:aim;
            Gizmos.color=Color.magenta;Gizmos.DrawLine(interaction.InteractionOrigin.position,physical);Gizmos.DrawWireSphere(physical,.04f);
            var reticle=Object.FindObjectsByType<ReticlePresenter>(FindObjectsSortMode.None).FirstOrDefault(r=>r.ViewCamera==camera);if(reticle==null)return;
            var canvas=reticle.GetComponentInParent<Canvas>();var uiCamera=canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;var screen=RectTransformUtility.WorldToScreenPoint(uiCamera,reticle.transform.position);var viewport=camera.ScreenToViewportPoint(screen);var reticleRay=camera.ViewportPointToRay(viewport);
            Gizmos.color=Color.white;Gizmos.DrawWireSphere(reticleRay.GetPoint(1),.015f);Handles.Label(ray.GetPoint(1),"Reticle viewport "+new Vector2(viewport.x,viewport.y).ToString("F4")+" | cyan camera / yellow aim / magenta physical reach");
        }
    }
}
