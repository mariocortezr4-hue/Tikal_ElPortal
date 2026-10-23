using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace Tikal.UI
{
    // TMP presentation preprocessing leaves the original device-aware binding strings untouched.
    [RequireComponent(typeof(TMP_Text))]
    public sealed class MapHintTypography : MonoBehaviour,ITextPreprocessor
    {
        TMP_Text _text;
        void OnEnable(){_text=GetComponent<TMP_Text>();_text.textPreprocessor=this;_text.SetAllDirty();}
        void OnDisable(){if(_text!=null&&ReferenceEquals(_text.textPreprocessor,this)){_text.textPreprocessor=null;_text.SetAllDirty();}}
        public string PreprocessText(string text)
        {
            text=Regex.Replace(text,@"\s*·\s*(.+) VOLVER$","      [$1] VOLVER");
            return Regex.Replace(text,@"\[([^\]]+)\]","<color=#D9BF86><b>[$1]</b></color>");
        }
    }
}
