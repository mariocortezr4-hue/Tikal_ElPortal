using Tikal.Core;
using UnityEngine;
using UnityEngine.Playables;

namespace Tikal.Cinematics
{
    public sealed class PlazaPassageSequence : MonoBehaviour
    {
        [SerializeField] PlayableDirector _director;
        [SerializeField] Transform _visualRoot;
        [SerializeField] CanvasGroup _localFade;
        Animator _visual;
        Level01PlazaTransition _transition;
        bool _completed;
        public string SelectedCharacterId { get; private set; }
        public PlayableDirector Director => _director;
        public Animator VisualAnimator => _visual;

        void Start()
        {
            _transition = Level01PlazaTransition.ForCinematicPreview();
            SelectedCharacterId = _transition.CharacterId;
            var definition = CharacterCatalog.GetOrDefault(SelectedCharacterId);
            if (definition.VisualPrefab == null)
            {
                Debug.LogError("Selected cinematic character has no visual prefab.");
                _transition.Arrive();
                return;
            }
            var visual = Instantiate(definition.VisualPrefab, _visualRoot);
            visual.name = definition.DisplayName + "_CinematicVisual";
            _visual = visual.GetComponentInChildren<Animator>(true);
            if (_visual != null)
            {
                _visual.applyRootMotion = false;
                _visual.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _visual.SetBool("IsGrounded", true);
                _visual.SetBool("IsCrouching", false);
                _visual.SetFloat("Speed", 0);
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _director.stopped += Finished;
            _director.timeUpdateMode = DirectorUpdateMode.Manual;
            _director.Play();
            _director.Evaluate();
            _transition.Fade.alpha = 0;
        }

        void Update()
        {
            if (_director == null || _completed) return;
            _director.time = System.Math.Min(_director.duration, _director.time + Time.unscaledDeltaTime);
            _director.Evaluate();
            // Reuse the existing locomotion controller and its real Walk clip at the authored threshold.
            if (_visual != null)
            {
                bool walking = _director.time >= 2.6;
                _visual.SetFloat("Speed", walking ? .61538464f : 0);
                // Existing gameplay Walk is calibrated for 4 m/s; this shot covers 13.7 m in 7.2 s.
                _visual.speed = walking ? (13.7f / 7.2f) / 4f : 1f;
            }
            if (_director.time >= _director.duration - .001) Finished(_director);
        }

        void Finished(PlayableDirector director)
        {
            if (!isActiveAndEnabled || _localFade == null || director.time < director.duration - .05) return;
            _completed = true;
            _localFade.alpha = 1;
            _transition.Arrive();
        }

        void OnDisable()
        {
            if (_director != null) _director.stopped -= Finished;
        }
    }
}
