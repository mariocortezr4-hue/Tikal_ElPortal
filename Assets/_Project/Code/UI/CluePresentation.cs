using Tikal.Input;
using Tikal.Puzzles.Clues;
namespace Tikal.UI
{
    public static class CluePresentation
    {
        public static string Description(ClueDefinition clue,PlayerInputReader input,InputDeviceManager devices)
        {
            if(clue.SemanticContent!=ClueSemanticContent.SecretCommand)return clue.Description;
            var binding=InputPromptResolver.Resolve(input?.SecretCommandAction,devices?.ActiveDevice??InputDeviceFamily.KeyboardMouse).DisplayText;
            return string.IsNullOrEmpty(binding)?clue.Description:"[ "+binding+" ]\n"+clue.Description;
        }
    }
}