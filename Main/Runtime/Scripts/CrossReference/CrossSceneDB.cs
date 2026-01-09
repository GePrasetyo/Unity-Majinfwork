using System.Collections.Generic;
using UnityEngine;

namespace Majinfwork.CrossRef {
    //[CreateAssetMenu(fileName = "CrossSceneDB", menuName = "Scriptable Objects/CrossSceneDB")]
    public class CrossSceneDB : ScriptableObject {
        [System.Serializable]
        public class Link {
            public Object host;        // The SO or MonoBehaviour holding the field
            public string fieldName;   // Name of the variable
            public string targetGuid;  // ID of the anchor
        }
        public List<Link> links = new List<Link>();

        public void Register(Object host, string field, string id) {
            links.RemoveAll(l => l.host == host && l.fieldName == field);
            if (!string.IsNullOrEmpty(id)) links.Add(new Link { host = host, fieldName = field, targetGuid = id });
        }
    }
}