using UnityEngine;
using UnityEditor;
using System;

#nullable enable

namespace jwelloneEditor
{
	public class PreviewReplacementShaderForTextureStreaming : PreviewReplacementShader
	{
		[NonSerialized] Shader? _shader;

		public override string displayName => "TextureStreaming";

		public override void Draw(Camera camera)
		{
			base.Draw(camera);

			_shader ??= EditorGUIUtility.LoadRequired("SceneView/SceneViewShowTextureStreaming.shader") as Shader;

			if (!_shader?.isSupported ?? false)
			{
				return;
			}

			Texture.SetStreamingTextureMaterialDebugProperties();

			camera.SetReplacementShader(_shader, "RenderType");
		}

		public override void Release()
		{
			_shader = null;
		}
	}
}