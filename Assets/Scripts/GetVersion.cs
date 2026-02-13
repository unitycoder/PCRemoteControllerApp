using TMPro;
using UnityEngine;

namespace UnityLibrary
{
    public class GetVersion : MonoBehaviour
    {
        void Awake()
        {
            var t = GetComponent<TextMeshProUGUI>();
            if (t != null)
            {
                t.text = "v" + Application.version;
            }
        }
    }
}