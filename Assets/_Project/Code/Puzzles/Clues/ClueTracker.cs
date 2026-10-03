using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
namespace Tikal.Puzzles.Clues
{
    [DisallowMultipleComponent]
    public sealed class ClueTracker : MonoBehaviour
    {
        private readonly Dictionary<string,ClueDefinition> _discovered=new Dictionary<string,ClueDefinition>(StringComparer.Ordinal);
        private readonly List<ClueDefinition> _discoveryOrder=new List<ClueDefinition>();
        private ReadOnlyCollection<ClueDefinition> _view;
        public IReadOnlyList<ClueDefinition> DiscoveredClues=>_view??= _discoveryOrder.AsReadOnly();
        public int Count=>_discovered.Count;
        public event Action<ClueDefinition> ClueDiscovered;
        public event Action ProgressRestored;
        public bool HasClue(string id)=>!string.IsNullOrWhiteSpace(id)&&_discovered.ContainsKey(id);
        public bool TryGetClue(string id,out ClueDefinition clue){clue=null;return !string.IsNullOrWhiteSpace(id)&&_discovered.TryGetValue(id,out clue);}
        public bool Discover(ClueDefinition clue)
        {
            if(clue==null||string.IsNullOrWhiteSpace(clue.Id)||_discovered.ContainsKey(clue.Id))return false;
            _discovered.Add(clue.Id,clue);_discoveryOrder.Add(clue);ClueDiscovered?.Invoke(clue);return true;
        }
        public ClueProgressSnapshot CaptureSnapshot()
        {
            var ids=new string[_discoveryOrder.Count];for(int i=0;i<ids.Length;i++)ids[i]=_discoveryOrder[i].Id;
            return new ClueProgressSnapshot(ids);
        }
        // Resolve everything before replacing state. Loading is distinct from a new discovery notification.
        public bool TryRestoreSnapshot(ClueProgressSnapshot snapshot,IEnumerable<ClueDefinition> catalogue,out string error)
        {
            error=null;if(snapshot==null||catalogue==null){error="Snapshot and clue catalogue are required.";return false;}
            var definitions=new Dictionary<string,ClueDefinition>(StringComparer.Ordinal);
            foreach(var clue in catalogue)
            {
                if(clue==null||string.IsNullOrWhiteSpace(clue.Id)||definitions.ContainsKey(clue.Id)){error="Clue catalogue contains an invalid or duplicate ID.";return false;}
                definitions.Add(clue.Id,clue);
            }
            var restored=new List<ClueDefinition>();var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var id in snapshot.DiscoveredClueIds)
            {
                if(string.IsNullOrWhiteSpace(id)||!definitions.TryGetValue(id,out var clue)){error="Snapshot contains an unknown clue ID.";return false;}
                if(seen.Add(id))restored.Add(clue);
            }
            _discovered.Clear();_discoveryOrder.Clear();
            foreach(var clue in restored){_discovered.Add(clue.Id,clue);_discoveryOrder.Add(clue);}
            ProgressRestored?.Invoke();return true;
        }
    }
}
