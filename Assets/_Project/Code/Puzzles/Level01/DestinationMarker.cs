using UnityEngine;
namespace Tikal.Puzzles.Level01
{
    // Abstract blockout shapes; no historical or cultural meanings are assigned.
    public enum GuardianSymbol { SplitCircle, Angular, Radial }
    public sealed class DestinationMarker : MonoBehaviour
    {
        [SerializeField] private GuardianSymbol _symbol;
        public GuardianSymbol Symbol => _symbol;
    }
}
