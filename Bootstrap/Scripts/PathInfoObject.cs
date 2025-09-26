using TMPro;

using UnityEngine;
namespace ViitorCloud.MultiScreenVideoPlayer {
    public class PathInfoObject : MonoBehaviour {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI pathText;
        
        public PathInfoObject Init(string name, string path) {
            nameText.text = name;
            pathText.text = path;
            
            return this;
        }
    }
}