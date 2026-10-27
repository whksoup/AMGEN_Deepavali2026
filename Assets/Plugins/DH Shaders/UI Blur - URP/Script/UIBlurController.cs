using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DH.Shaders.UIBlur
{

    public class UIBlurController : MonoBehaviour
    {
        [HideInInspector][Range(0.001f, 0.015f)] public float BlurAmount = 0.005f;
        private Image _uiImage;
        public bool isTransparent = false;
        public Camera blurCamera;
public List<UnityEngine.UI.Image> UIImages = new List<UnityEngine.UI.Image>();
        public Material sharedMaterial;
        [SerializeField, HideInInspector] public List<BlurUIItem> blurUIItems = new();

        private void OnValidate()
        {
            this.Start();
        }
        void Start()
        {
            SetBlurAmount();
        }

        /// <summary>
        /// Sets the blur amount, clamping the input value to ensure it remains within the range of 0.001f to 0.015f.
        /// </summary>
        /// <param name="newBlurAmount">The new blur amount value to be set.</param>
        public void SetBlurAmount(float newBlurAmount)
        {
            BlurAmount = Mathf.Clamp(newBlurAmount, 0.001f, 0.015f);
            SetBlurAmount();
        }

        /// <summary>
        /// Sets the blur camera to the specified camera.
        /// </summary>
        /// <param name="cameraToSet">The camera to be set as the blur camera.</param>
        public void SetBlurCamera(Camera cameraToSet)
        {
            blurCamera = cameraToSet;
            SetBlurCamera();
        }

        public void SetBlurAmount()
        {
            if (_uiImage == null)
            {
                _uiImage = GetComponent<Image>();
            }

            if (isTransparent)
            {
                _uiImage.material.SetFloat("_BlurAmount", BlurAmount);
                SetBlurCamera();
                return;
            }

            float aspect = Camera.main.aspect;

            float xAmount = BlurAmount;
            float yAmount = BlurAmount;

            if (aspect > 1f)
            {
                xAmount /= aspect;
            }
            else
            {
                yAmount *= aspect;
            }


            _uiImage.material.SetFloat("_yBlur", yAmount);
            _uiImage.material.SetFloat("_xBlur", xAmount);

        }


        public void SetBlurCamera(bool reverse = false)
        {
            if (blurCamera == null) return;
            if (blurCamera.targetTexture != null)
            {
                blurCamera.targetTexture.Release();
            }

            if (!reverse)
            {
                blurCamera.targetTexture = new RenderTexture(blurCamera.pixelWidth, blurCamera.pixelHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                blurCamera.targetTexture.name = "BlurTexture";
                _uiImage.material.SetTexture("_RenTex", blurCamera.targetTexture);
            }
            else
            {
                blurCamera.targetTexture = null;
            }
          
        }


        [Serializable]
        public class BlurUIItem
        {
            [field:SerializeField] public Graphic UiElement { get; set; }
            [field: SerializeField] public Material OldMaterial { get; private set; }
            [field: SerializeField] public bool IncludeChildren { get; set; }
            [field: SerializeField] private List<BlurUIItem> children = new List<BlurUIItem>();

            public BlurUIItem(Graphic uiElement, bool includeChildren = false)
            {
                UiElement = uiElement;
                OldMaterial = UiElement.material;
                IncludeChildren = includeChildren;
                children.Clear();
                if (includeChildren)
                {
                    var newChildren = UiElement.GetComponentsInChildren<Graphic>();
                    foreach (var item in newChildren)
                    {
                        if (item.transform.parent == UiElement.transform && !children.Any(e => e.UiElement == item)) children.Add(new BlurUIItem(item, true));
                    }
                }
            }
            public void SetOldMaterial()
            {
                UiElement.material = OldMaterial;
                foreach (BlurUIItem item in children)
                {
                    item.SetOldMaterial();
                }
            }
            public void SetMaterial(Material newMaterial, bool changedIncludeChildren = false)
            {
                if (changedIncludeChildren)
                {
                    if (IncludeChildren)
                    {
                        var newChildren = UiElement.GetComponentsInChildren<Graphic>();
                        foreach (var item in newChildren)
                        {
                            if (item.transform.parent == UiElement.transform && !children.Any(e => e.UiElement == item)) children.Add(new BlurUIItem(item, true));
                            
                        }

                        List<BlurUIItem> toRemove = new();

                        foreach (var item in children)
                        {
                            if (!newChildren.Any(e => e == item.UiElement)) toRemove.Add(item);
                        }

                        foreach (var item in toRemove)
                        {
                            children.Remove(item);
                        }
                    }
                    else
                    {
                        children.ForEach(e => e.SetOldMaterial());
                        children.Clear();
                    }
                }


                foreach (BlurUIItem item in children)
                {
                    item.SetMaterial(newMaterial);
                }

                ChangeMaterial(UiElement, newMaterial);
            }

            internal void ChangeMaterial(Graphic uiElement, Material newMaterial)
            {
                uiElement.material = new Material(newMaterial);

                Texture texture = uiElement.mainTexture;
                uiElement.material.SetTexture("_BaseMap", texture);
                uiElement.material.color = uiElement.color;
            }
        }
    }
}
