using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

#nullable enable

namespace jwelloneEditor
{
    public sealed class MaterialPreviewWindow : EditorWindow
    {
        class CustomMaterialEditor : MaterialEditor
        {
            static readonly Type _type = typeof(MaterialEditor);
            int _replacementIndex;
            MethodInfo? _miGetPreviewType;
            PreviewRenderUtility? _cachePreviewRenderUtility;
            readonly List<IPreivewReplacementShader> _replacements;
            readonly string[] _replacementLabels;

            PreviewRenderUtility _previewRenderUtility
            {
                get
                {
                    if (_cachePreviewRenderUtility != null)
                    {
                        return _cachePreviewRenderUtility;
                    }

                    var mi = _type.GetMethod("GetPreviewRendererUtility", (BindingFlags.NonPublic | BindingFlags.Static));
                    _cachePreviewRenderUtility = (PreviewRenderUtility)mi.Invoke(null, null);
                    return _cachePreviewRenderUtility;
                }
            }

            bool isSupportRenderingPreview
            {
                get
                {
                    var mi = _type.GetMethod("SupportRenderingPreview", (BindingFlags.NonPublic | BindingFlags.Static));
                    return (bool)mi.Invoke(null, new object[] { base.target });
                }
            }

            Light[] lights => _previewRenderUtility?.lights ?? Array.Empty<Light>();

            IPreivewReplacementShader _currentReplacement => _replacements[_replacementIndex];

            Camera _camera => _previewRenderUtility.camera;


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

                _replacements = new(new IPreivewReplacementShader[]
                {
                    new PreviewReplacementShaderNothing(),
                    new PreviewReplacementShaderForOverdraw(),
                    new PreviewReplacementShaderForMips(),
                    new PreviewReplacementShaderForTextureStreaming(),
                    new PreviewReplacementShaderForNormals(),
                    new PreivewReplacementShaderForChannel(PreivewReplacementShaderForChannel.eMode.R),
                    new PreivewReplacementShaderForChannel(PreivewReplacementShaderForChannel.eMode.G),
                    new PreivewReplacementShaderForChannel(PreivewReplacementShaderForChannel.eMode.B),
                    new PreivewReplacementShaderForChannel(PreivewReplacementShaderForChannel.eMode.A),
                    new PreviewReplacementShaderForMeshInfo(PreviewReplacementShaderForMeshInfo.Mode.UV0),
                    new PreviewReplacementShaderForMeshInfo(PreviewReplacementShaderForMeshInfo.Mode.UV1),
                    new PreviewReplacementShaderForMeshInfo(PreviewReplacementShaderForMeshInfo.Mode.VERTEXCOLOR),
                    new PreviewReplacementShaderForMeshInfo(PreviewReplacementShaderForMeshInfo.Mode.NORMALS),
                    new PreviewReplacementShaderForMeshInfo(PreviewReplacementShaderForMeshInfo.Mode.TANGENTS),
                    new PreviewReplacementShaderForMeshInfo(PreviewReplacementShaderForMeshInfo.Mode.BITANGENTS)
                });

                _replacementLabels = new string[_replacements.Count];
                for (var i = 0; i < _replacements.Count; ++i)
                {
                    _replacementLabels[i] = _replacements[i].displayName;
                }
            }

            public override void OnInspectorGUI()
            {
                GUILayout.BeginVertical();
                DrawHeader();
                base.OnInspectorGUI();
                GUILayout.EndVertical();
            }

            public void DefaultPreviewGUI(Rect rc)
            {
                DefaultPreviewGUI(rc, null);

                if (Event.current.type == EventType.ScrollWheel && rc.Contains(Event.current.mousePosition))
                {
                    _camera.fieldOfView += Event.current.delta.y * 0.1f;
                    Event.current.Use();
                }
            }

            public void PreviewSettingsGUI()
            {
                if (isPreviewTypeForPlane || !isSupportRenderingPreview)
                {
                    return;
                }

                DefaultPreviewSettingsGUI();

                var targetLights = lights;
                for (var i = 0; i < targetLights.Length; ++i)
                {
                    EditorGUILayout.BeginHorizontal("box", GUILayout.Height(18));
                    EditorGUILayout.LabelField($"Light{i}", GUILayout.Width(40), GUILayout.Height(14));
                    var color = targetLights[i].color;
                    targetLights[i].color = EditorGUILayout.ColorField(color, GUILayout.Width(28), GUILayout.Height(14));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal("box", GUILayout.Height(18));
                EditorGUILayout.LabelField("Bg", GUILayout.Width(20), GUILayout.Height(14));
                _camera.backgroundColor = EditorGUILayout.ColorField(_camera.backgroundColor, GUILayout.Width(28), GUILayout.Height(14));
                EditorGUILayout.EndHorizontal();

                _replacementIndex = EditorGUILayout.Popup(_replacementIndex, _replacementLabels, new[] { GUILayout.Width(128) });
            }

            public Texture2D? RenderStaticPreview(int width, int height)
            {
                if (!isSupportRenderingPreview)
                {
                    return null;
                }

                var mi = _type.GetMethod("Init", (BindingFlags.NonPublic | BindingFlags.Instance));
                mi.Invoke(this, null);

                var r = new Rect(0, 0, width, height);
                _previewRenderUtility.BeginPreview(r, null);

                mi = _type.GetMethod("DoRenderPreview", (BindingFlags.NonPublic | BindingFlags.Instance));
                mi.Invoke(this, new object[] { _previewRenderUtility, false });

                var sourceRT = (RenderTexture)_previewRenderUtility.EndPreview();
                var destRT = RenderTexture.GetTemporary(width, height, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
                var pi = typeof(EditorGUIUtility).GetProperty("GUITextureBlit2SRGBMaterial", BindingFlags.NonPublic | BindingFlags.Static);
                Graphics.Blit(sourceRT, destRT, (Material)pi.GetValue(null));

                var texture = new Texture2D(width, height, TextureFormat.ARGB32, false, false);
                var tmpRT = RenderTexture.active;
                RenderTexture.active = destRT;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();

                RenderTexture.ReleaseTemporary(destRT);
                RenderTexture.active = tmpRT;

                return texture;
            }

            public void OnPreCullNotify(Camera target)
            {
                if (target.GetInstanceID() != _camera.GetInstanceID())
                {
                    return;
                }

                _currentReplacement.Draw(_camera);
            }

            public void OnPostRenderNotify(Camera target)
            {
                if (target.GetInstanceID() != _camera.GetInstanceID())
                {
                    return;
                }

                _currentReplacement.Reset(_camera);
            }

            public void Release()
            {
                foreach (var r in _replacements)
                {
                    r.Release();
                }
                _replacements.Clear();
            }
        }

        Vector2 _scrollPosition;
        [SerializeField] Shader? _shader;
        [SerializeField] Texture2D? _sourceTexture;
        [NonSerialized] Material? _material;
        [NonSerialized] Texture2D? _textureBg;
        [NonSerialized] Texture2D? _destTexture;
        [NonSerialized] CustomMaterialEditor? _materialEditor;

        [MenuItem("Tools/jwellone/window/Material Preview")]
        static void ShowWindow()
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

            Camera.onPreCull += OnPreCullNotify;
            Camera.onPostRender += OnPostRenderNotify;
        }

        void OnDisable()
        {
            _textureBg = null;
            DestroyDestTexture();
            DestroyMaterial();
            DestroyMaterialEditor();

            Camera.onPreCull -= OnPreCullNotify;
            Camera.onPostRender -= OnPostRenderNotify;
        }


        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal();

            _materialEditor?.PreviewSettingsGUI();

            GUILayout.FlexibleSpace();

            _sourceTexture = (Texture2D)EditorGUILayout.ObjectField(_sourceTexture, typeof(Texture2D), false);

            if (GUILayout.Button("Reset", GUILayout.Width(54)))
            {
                _shader = _material?.shader ?? Shader.Find("UI/Default");
                DestroyDestTexture();
                DestroyMaterial();
                DestroyMaterialEditor();
            }

            if (GUILayout.Button("Save", GUILayout.Width(54)))
            {
                var filePath = EditorUtility.SaveFilePanel("Preview Save", Application.dataPath + "/../", "", ".png");
                Save(filePath);
            }

            GUILayout.EndHorizontal();

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

            CreateDestTextureIfNeeded();

            EditorGUILayout.BeginHorizontal();

            var width = (int)(position.size.x / 1.5f);
            var height = (int)(position.size.y - 32);

            if ((!_materialEditor?.isPreviewTypeForPlane ?? false) || _destTexture == null)
            {
                var rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
                _materialEditor?.DefaultPreviewGUI(rect);
            }
            else if (_destTexture != null)
            {
                DrawDestTexture(width, height);
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

            _materialEditor?.OnInspectorGUI();

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

        void DrawDestTexture(int width, int height)
        {
            var texWidth = (float)((width > height) ? height : width);
            var texHeight = texWidth * texWidth / texWidth;

            if (_destTexture!.width > _destTexture.height)
            {
                texHeight *= _destTexture.height / (float)_destTexture.width;
            }
            else
            {
                texWidth *= _destTexture.width / (float)_destTexture.height;
            }

            var spaceWidth = (width - texWidth) / 2f;
            GUILayout.BeginHorizontal("box");
            GUILayout.Space(spaceWidth);

            var spaceHeight = (height - texHeight) / 2f;
            GUILayout.BeginVertical();
            GUILayout.Space(spaceHeight);

            var textureRect = GUILayoutUtility.GetRect(texWidth, texHeight, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

            GUI.BeginGroup(textureRect);
            {
                var xLoop = Mathf.RoundToInt(textureRect.width);
                var yLoop = Mathf.RoundToInt(textureRect.height);
                var bgWidth = _textureBg!.width;
                var bgHeight = _textureBg.height;
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

            GUILayout.Space(spaceHeight);
            GUILayout.EndVertical();

            GUILayout.Space(spaceWidth);
            GUILayout.EndHorizontal();
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
                _materialEditor.Release();
                DestroyImmediate(_materialEditor);
                _materialEditor = null;
            }
        }

        void OnPreCullNotify(Camera camera)
        {
            _materialEditor?.OnPreCullNotify(camera);
        }

        void OnPostRenderNotify(Camera camera)
        {
            _materialEditor?.OnPostRenderNotify(camera);
        }
    }
}