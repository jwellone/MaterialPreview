using System;
using UnityEngine;

#nullable enable

namespace jwelloneEditor
{
	public class PreviewReplacementShaderForNormals : PreviewReplacementShader
	{
		[NonSerialized] Shader? _shader = null;

		public override string displayName => "Normals";

		public override void Draw(Camera camera)
		{
			base.Draw(camera);

			_shader ??= Shader.Find("jwellone/Editor/MaterialPreview/DrawNormals");

			camera.SetReplacementShader(_shader, null);
		}

		public override void Release()
		{
			_shader = null;
		}
	}
}