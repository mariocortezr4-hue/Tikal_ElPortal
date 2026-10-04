using System;
using System.Collections.Generic;
using UnityEngine;
namespace Tikal.Puzzles.Clues
{
    [Serializable]
    public sealed class ClueProgressSnapshot
    {
        [SerializeField] private string[] _discoveredClueIds=Array.Empty<string>();
        public IReadOnlyList<string> DiscoveredClueIds=>Array.AsReadOnly(_discoveredClueIds??Array.Empty<string>());
        public ClueProgressSnapshot() { }
        public ClueProgressSnapshot(IEnumerable<string> ids)
        {
            if(ids==null)throw new ArgumentNullException(nameof(ids));
            _discoveredClueIds=new List<string>(ids).ToArray();
        }
    }
}
