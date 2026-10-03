namespace Tikal.Input
{
    public readonly struct InputPromptData
    {
        public string DisplayText { get; }
        public string ControlPath { get; }
        public InputPromptData(string displayText, string controlPath)
        {
            DisplayText = displayText;
            ControlPath = controlPath;
        }
    }
}
