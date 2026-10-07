using Tikal.Input;
using Tikal.Puzzles.Clues;
using UnityEngine.InputSystem;
namespace Tikal.UI
{
    public static class CluePresentation
    {
        public static bool IsBindingChange(object source, InputActionChange change, InputAction action) =>
            change == InputActionChange.BoundControlsChanged && action != null &&
            (ReferenceEquals(source, action) || ReferenceEquals(source, action.actionMap) ||
                ReferenceEquals(source, action.actionMap?.asset));

        public static string Description(ClueDefinition clue,PlayerInputReader input,InputDeviceManager devices)
        {
            if(clue.SemanticContent!=ClueSemanticContent.SecretCommand)return clue.Description;
            var binding=InputPromptResolver.Resolve(input?.SecretCommandAction,devices?.ActiveDevice??InputDeviceFamily.KeyboardMouse).DisplayText;
            return string.IsNullOrEmpty(binding)?clue.Description:"[ "+binding+" ]\n"+clue.Description;
        }
    }
}
