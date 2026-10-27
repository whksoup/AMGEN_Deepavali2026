using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static DH.Shaders.UIBlur.UIBlurController;
using static UnityEngine.GraphicsBuffer;
#if UNITY_URP || UNITY_RENDER_PIPELINE_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace DH.Shaders.UIBlur
{

    [CustomEditor(typeof(UIBlurController))]
    public class UIBlurEditor : Editor
    {
        SerializedProperty _propBlurAmount;
        SerializedProperty _propSharedMaterial;
        SerializedProperty _propBlurCamera;
        SerializedProperty _propIsTransparent;
        Object _startObjectCamera;

        bool _wasTransparent = false;
        private bool _foldout;
        UIBlurController _target;
#if UNITY_URP || UNITY_RENDER_PIPELINE_URP
        UniversalRenderPipelineAsset renderPipelineAsset;
#endif
        private void OnEnable()
        {
#if UNITY_URP || UNITY_RENDER_PIPELINE_URP
            renderPipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
#endif
            _propSharedMaterial = serializedObject.FindProperty("sharedMaterial");
            _propBlurAmount = serializedObject.FindProperty("BlurAmount");
            _propBlurCamera = serializedObject.FindProperty("blurCamera");
            _propIsTransparent = serializedObject.FindProperty("isTransparent");
            _target = (UIBlurController)target;

            _startObjectCamera = _propBlurCamera.objectReferenceValue;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_propBlurAmount);
            EditorGUILayout.PropertyField(_propIsTransparent);
            GUIStyle myStyle = new GUIStyle();
            myStyle.normal.textColor = Color.red;
            if (_target.isTransparent)
            {
                _wasTransparent = true;
                myStyle.normal.textColor = Color.yellow;
                GUILayout.Label("(only works for gameobjects in the scene)", myStyle);
                EditorGUILayout.PropertyField(_propBlurCamera);
               
            }
            else
            {
#if UNITY_URP || UNITY_RENDER_PIPELINE_URP
                if (renderPipelineAsset.supportsCameraOpaqueTexture) myStyle.normal.textColor = Color.green;
                GUILayout.Label("(you need to set \"Opaque Texture\" to TRUE)", myStyle);
                if (!renderPipelineAsset.supportsCameraOpaqueTexture)
                {
                    if (GUILayout.Button("Set \"Opaque Texture\" to TRUE"))
                        renderPipelineAsset.supportsCameraOpaqueTexture = true;
                }
#else
                GUILayout.Label("(you need URP in project)", myStyle);
#endif
                if (_wasTransparent)
                {
                    _wasTransparent = true;
                    _target.SetBlurCamera(true);
                }
                EditorGUILayout.PropertyField(_propSharedMaterial);

                DrawFoldout();
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                if (_propBlurCamera.objectReferenceValue != _startObjectCamera)
                {
                    _startObjectCamera = _propBlurCamera.objectReferenceValue;
                    _target.SetBlurCamera();
                }
            }

            

            
        }

        private void DrawFoldout()
        {
            Rect foldoutRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _foldout = EditorGUILayout.Foldout(_foldout, new GUIContent("UI Elements", "UI elements that will be affected by blurring."), true);

            if (_foldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                for (int i = 0; i < _target.blurUIItems.Count; i++)
                {
                    if (_target.blurUIItems[i].UiElement == null)
                    {
                        _target.blurUIItems.RemoveAt(i);
                        continue;
                    }
                    EditorGUILayout.BeginHorizontal();

                    bool before = GUI.enabled;
                    GUI.enabled = false;
                    _target.blurUIItems[i].UiElement = (Graphic)EditorGUILayout.ObjectField(_target.blurUIItems[i].UiElement, typeof(Graphic), true, GUILayout.ExpandWidth(true));
                    GUI.enabled = before;

                    bool oldIsActive = _target.blurUIItems[i].IncludeChildren;

                    _target.blurUIItems[i].IncludeChildren = GUILayout.Toggle(_target.blurUIItems[i].IncludeChildren, new GUIContent("", tooltip: "Include children"), GUILayout.Width(20));

                    if (oldIsActive != _target.blurUIItems[i].IncludeChildren)
                    {
                        OnBoolChanged(_target.blurUIItems[i], _target.sharedMaterial);
                    }

                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        Undo.RecordObject(_target, "Remove UI Object");
                        _target.blurUIItems[i].SetOldMaterial();
                        _target.blurUIItems.RemoveAt(i);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (_target.blurUIItems.Count < 1)
                {
                    EditorGUILayout.LabelField("List is Empty");
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
            HandleDragAndDrop(foldoutRect, _target.blurUIItems, _target.sharedMaterial);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_target);
            }
        }

        private void HandleDragAndDrop(Rect dropArea, List<BlurUIItem> uiObjects, Material sharedMaterial)
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        Undo.RecordObject(target, "Add UI Object");

                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            GameObject go = draggedObject as GameObject;
                            if (go != null)
                            {
                                Graphic graphic = go.GetComponent<Graphic>();
                                if (graphic != null)
                                {
                                    BlurUIItem newItem = new BlurUIItem(graphic);
                                    newItem.IncludeChildren = false;  
                                    newItem.SetMaterial(sharedMaterial);
                                    uiObjects.Add(newItem);
                                }
                            }
                        }

                        evt.Use();
                    }
                    break;
            }
        }

        private void OnBoolChanged(BlurUIItem item, Material sharedMaterial)
        {
            item.SetMaterial(sharedMaterial, true);
        }
    }
}
