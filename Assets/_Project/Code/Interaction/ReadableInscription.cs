using System;
using UnityEngine;
namespace Tikal.Interaction
{
    [DisallowMultipleComponent]
    public sealed class ReadableInscription : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _title;
        [SerializeField, TextArea] private string _text;
        public string Title => _title;
        public string Text => _text;
        public string InteractionText => "Leer";
        public bool CanInteract => isActiveAndEnabled&&!string.IsNullOrWhiteSpace(_text);
        public event Action<ReadableInscription> ReadRequested;
        public void Interact() { if(CanInteract)ReadRequested?.Invoke(this); }
    }
}
