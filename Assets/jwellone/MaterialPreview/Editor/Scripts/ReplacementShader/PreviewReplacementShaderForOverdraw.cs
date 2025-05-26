using UnityEngine;
using UnityEditor;
using System;

#nullable enable

namespace jwelloneEditor
{
    public class PreviewReplacementShaderForOverdraw : PreviewReplacementShader
    {
        [NonSerialized] Shader? _shader;

        protected override bool canMainCameraClearFlagsSolid => true;

        public override string displayName => "Overdraw";

        public override void Draw(Camera camera)
        {
            base.Draw(camera);

            _shader ??= EditorGUIUtility.LoadRequired("SceneView/SceneViewShowOverdraw.shader") as Shader;

            camera.SetReplacementShader(_shader, "RenderType");
        }

        public override void Release()
        {
            _shader = null;
        }
    }
}