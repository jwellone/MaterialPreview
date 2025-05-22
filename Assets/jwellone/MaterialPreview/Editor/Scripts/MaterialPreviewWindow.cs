using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System;

#nullable enable

namespace jwelloneEditor
{
    public sealed class MaterialPreviewWindow : EditorWindow
    {
        class CustomMaterialEditor : MaterialEditor
        {
            static Type _type = typeof(MaterialEditor);
            MethodInfo? _miGetPreviewType;
            readonly GUIStyle _bg = new();

            bool isSupportRenderingPreview
            {
                get
                {
                    var mi = _type.GetMethod("SupportRenderingPreview", (BindingFlags.NonPublic | BindingFlags.Static));
                    return (bool)mi.Invoke(null, new object[] { base.target });
                }
            }

            public bool isPreviewTypeForPlane
            {
                get
                {
                    _miGetPreviewType ??= _type.GetMethod("GetPreviewType", (BindingFlags.NonPublic | BindingFlags.Static));
                    return 1 == (int)_miGetPreviewType.Invoke(null, new object[] { target });
                }
            }

            CustomMaterialEditor()
            {
                var pi = _type.GetProperty("firstInspectedEditor", BindingFlags.NonPublic | BindingFlags.Instance);
                pi.SetValue(this, true);
            }

            public void DefaultPreviewGUI(Rect rc)
            {
                DefaultPreviewGUI(rc, _bg);
            }

            public Texture2D? RenderStaticPreview(int width, int height)
            {
                if (!isSupportRenderingPreview)
                {
                    return null;
                }

                Init();

                var mi = _type.GetMethod("GetPreviewRendererUtility", (BindingFlags.NonPublic | BindingFlags.Static));
                var previewRendererUtility = (PreviewRenderUtility)mi.Invoke(null, null);

                EditorUtility.SetCameraAnimateMaterials(previewRendererUtility.camera, animate: true);
                previewRendererUtility.BeginStaticPreview(new Rect(0f, 0f, width, height));

                mi = _type.GetMethod("StreamRenderResources", (BindingFlags.NonPublic | BindingFlags.Instance));
                mi.Invoke(this, null);

                mi = _type.GetMethod("DoRenderPreview", (BindingFlags.NonPublic | BindingFlags.Instance));
                mi.Invoke(this, new object[] { previewRendererUtility, false });

                return previewRendererUtility.EndStaticPreview();
            }
        }

        Vector2 _scrollPosition;
        [SerializeField] Shader? _shader;
        [SerializeField] Texture2D? _sourceTexture;
        [NonSerialized] Material? _material;
        [NonSerialized] Texture2D? _textureBg;
        [NonSerialized] Texture2D? _destTexture;
        [NonSerialized] CustomMaterialEditor? _materialEditor;

        [MenuItem("jwellone/Window/Material Preview")]
        static void Init()
        {
            var window = GetWindow(typeof(MaterialPreviewWindow));
            window.titleContent = new("Material Preview");
            window.Show();
        }

        void OnEnable()
        {
            if (_shader == null)
            {
                _shader = Shader.Find("UI/Default");
            }
            _textureBg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/jwellone/MaterialPreview/Editor/Textures/preview-bg.jpg");
        }

        void OnDisable()
        {
            _textureBg = null;
            DestroyDestTexture();
            DestroyMaterial();
            DestroyMaterialEditor();
        }

        void OnGUI()
        {
            var width = (int)(position.size.x / 2f);
            var margin = 36;
            minSize = new Vector2(512, width + margin);
            maxSize = new Vector2(maxSize.x, width + margin);

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal();

            if (!_materialEditor?.isPreviewTypeForPlane ?? false)
            {
                _materialEditor?.DefaultPreviewSettingsGUI();
            }

            GUILayout.FlexibleSpace();

            _sourceTexture = (Texture2D)EditorGUILayout.ObjectField(_sourceTexture, typeof(Texture2D), false);

            if (GUILayout.Button("リセット", GUILayout.Width(54)))
            {
                _shader = _material?.shader ?? Shader.Find("UI/Default");
                DestroyDestTexture();
                DestroyMaterial();
                DestroyMaterialEditor();
            }

            if (GUILayout.Button("保存", GUILayout.Width(54)))
            {
                var filePath = EditorUtility.SaveFilePanel("Preview保存", Application.dataPath + "/../", "", ".png");
                Save(filePath);
            }

            GUILayout.EndHorizontal();

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

            CreateDestTextureIfNeeded();

            EditorGUILayout.BeginHorizontal();

            if ((!_materialEditor?.isPreviewTypeForPlane ?? false) || _destTexture == null)
            {
                var rect = GUILayoutUtility.GetRect(width, width, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
                _materialEditor?.DefaultPreviewGUI(rect);
            }
            else if (_destTexture != null)
            {
                var textureRect = Rect.zero;
                if (_destTexture.height > _destTexture.width)
                {
                    var rcWidth = width * _destTexture.width / _destTexture.height;
                    var halfRcWidth = (width - rcWidth) / 2f;
                    GUILayout.Space(halfRcWidth);
                    textureRect = GUILayoutUtility.GetRect(rcWidth, width, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
                    GUILayout.Space(halfRcWidth);
                }
                else
                {
                    textureRect = GUILayoutUtility.GetRect(width, _destTexture.height * width / _destTexture.width, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
                }

                GUI.BeginGroup(textureRect);
                {
                    var xLoop = Mathf.RoundToInt(textureRect.width);
                    var yLoop = Mathf.RoundToInt(textureRect.height);
                    var bgWidth = _textureBg!.width;
                    var bgHeight = _textureBg!.height;
                    var bgRect = new Rect(0, 0, bgWidth, bgHeight);
                    for (var y = 0; y < yLoop; y += bgHeight)
                    {
                        for (var x = 0; x < xLoop; x += bgWidth)
                        {
                            bgRect.x = x;
                            bgRect.y = y;
                            GUI.DrawTexture(bgRect, _textureBg);
                        }
                    }
                }
                GUI.EndGroup();

                GUI.DrawTexture(textureRect, _destTexture);
            }

            if (_material == null)
            {
                _material = new Material(_shader);
                _material.hideFlags |= HideFlags.DontSaveInEditor;
            }

            if (_materialEditor == null && _material != null)
            {
                _materialEditor = (CustomMaterialEditor)Editor.CreateEditor(_material, typeof(CustomMaterialEditor));
                if (_materialEditor != null)
                {
                    UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(_materialEditor.target, true);
                }
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            GUILayout.BeginVertical();
            _materialEditor?.DrawHeader();
            _materialEditor?.OnInspectorGUI();
            GUILayout.EndVertical();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndHorizontal();

            var isBlit = EditorGUI.EndChangeCheck();
            if (_material != null && _material.shader != _shader || _material!.mainTexture != _sourceTexture)
            {
                _shader = _material.shader;
                isBlit = true;
            }

            _material!.mainTexture = _sourceTexture;

            if (isBlit)
            {
                Blit(_sourceTexture, _destTexture, _material);
            }
        }

        void CreateDestTextureIfNeeded()
        {
            if (_sourceTexture == null)
            {
                return;
            }

            var width = _sourceTexture.width;
            var height = _sourceTexture.height;
            if (_destTexture == null || width != _destTexture.width || height != _destTexture.height)
            {
                DestroyDestTexture();
                _destTexture = new Texture2D(width, height, TextureFormat.BGRA32, false);
            }
        }

        void Save(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var (isDelete, texture) = _materialEditor?.isPreviewTypeForPlane ?? false ? (false, _destTexture) : (true, _materialEditor?.RenderStaticPreview(512, 512));

            var bytes = default(byte[]);
            switch (Path.GetExtension(path))
            {
                case ".jpg":
                case ".jpeg":
                    bytes = texture.EncodeToJPG();
                    break;

                case ".tga":
                    bytes = texture.EncodeToTGA();
                    break;

                default:
                    bytes = texture.EncodeToPNG();
                    break;
            }

            File.WriteAllBytes(path, bytes);

            if (isDelete)
            {
                DestroyImmediate(texture);
            }
        }

        void Blit(Texture2D? source, Texture2D? dest, Material? material)
        {
            if (source == null || dest == null)
            {
                return;
            }

            var tmpRT = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(source.width, source.height, 24, dest.graphicsFormat);
            rt.Release();

            RenderTexture.active = rt;

            Graphics.Blit(source, rt, material);

            dest.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            dest.Apply();

            RenderTexture.ReleaseTemporary(rt);
            RenderTexture.active = tmpRT;
        }

        void DestroyDestTexture()
        {
            if (_destTexture != null)
            {
                DestroyImmediate(_destTexture);
                _destTexture = null;
            }
        }

        void DestroyMaterial()
        {
            if (_material != null)
            {
                _material.mainTexture = null;
                DestroyImmediate(_material);
                _material = null;
            }
        }

        void DestroyMaterialEditor()
        {
            if (_materialEditor != null)
            {
                DestroyImmediate(_materialEditor);
                _materialEditor = null;
            }
        }
    }
}