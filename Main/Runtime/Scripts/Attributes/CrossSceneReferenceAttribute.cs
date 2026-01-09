using UnityEngine;

namespace Majinfwork.CrossRef {
    /// <summary>
    /// Marks a field to be resolved by CrossSceneManager from a scene object with CrossSceneAnchor.
    /// The reference is stored in Resources/CrossSceneData/Global_Asset_Refs.asset.
    ///
    /// <para><b>Setup:</b></para>
    /// <list type="number">
    ///   <item>Add CrossSceneAnchor component to the target GameObject in the scene</item>
    ///   <item>Mark your field with [CrossSceneReference]</item>
    ///   <item>Link in Inspector - automatically creates entry in CrossSceneDB</item>
    /// </list>
    ///
    /// <para><b>Auto-resolution:</b> CrossSceneManager resolves on scene load.</para>
    ///
    /// <para><b>After cloning/instantiating, call:</b></para>
    /// <code>
    /// // ScriptableObject or MonoBehaviour
    /// var clone = Instantiate(original);
    /// clone.ResolveCrossSceneReferences(original);
    ///
    /// // Prefab (resolves all MonoBehaviours)
    /// var instance = Instantiate(prefab);
    /// instance.ResolveCrossSceneReferences(prefab);
    ///
    /// // Batch clone
    /// cloneMap.ResolveCrossSceneReferences();
    /// </code>
    /// </summary>
    public class CrossSceneReferenceAttribute : PropertyAttribute { }
}
