using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class EditorUtilities : ScriptableObject
{

    [MenuItem("GameObject/+Normalize Parents")]
    static void NormalizeParent()
    {
        Transform[] transforms = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable);

        foreach( Transform parent in transforms)
        {
            Vector3 parentLocalScale = parent.localScale;
            parent.localScale = new Vector3(1.0f, 1.0f, 1.0f);

            for(int i=0; i<parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Vector3 newChildPosition = new Vector3( child.localPosition.x * parentLocalScale.x, 
                                                        child.localPosition.y * child.localScale.y,
                                                        child.localPosition.z * child.localScale.z);

                Vector3 newChildScale = new Vector3(   child.localScale.x * parentLocalScale.x,
                                                       child.localScale.y * parentLocalScale.y,
                                                       child.localScale.z * parentLocalScale.z);

                child.localPosition = newChildPosition;
                child.localScale = newChildScale;

            }
        }
    }
        
}