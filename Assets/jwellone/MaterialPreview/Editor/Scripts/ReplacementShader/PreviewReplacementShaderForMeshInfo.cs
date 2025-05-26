using System;
using UnityEngine;

#nullable enable

namespace jwelloneEditor
{
	public class PreviewReplacementShaderForMeshInfo : PreviewReplacementShader
	{
		public enum Mode
		{
			UV0,
			UV1,
			VERTEXCOLOR,
			NORMALS,
			TANGENTS,
			BITANGENTS,
			MAX
		}

		private static Mode _currentMode = Mode.MAX;
		private Mode _mode;
		[NonSerialized] Shader? _shader;

		public override string displayName => _mode.ToString();

		public PreviewReplacementShaderForMeshInfo(Mode mode)
		{
			_mode = mode;
		}

		public override void Draw(Camera camera)
		{
			base.Draw(camera);

			UpdateKeyword();

			_shader ??= Shader.Find("jwellone/Editor/MaterialPreview/DrawMeshInfo");
			camera.SetReplacementShader(_shader, null);
		}

		public override void Release()
		{
			_shader = null;
		}

		private void UpdateKeyword()
		{
			if (_mode == _currentMode)
			{
				return;
			}

			_currentMode = _mode;

			for (var i = 0; i < (int)Mode.MAX; ++i)
			{
				Shader.DisableKeyword("_MESHINFO_" + ((Mode)i).ToString());
			}

			Shader.EnableKeyword("_MESHINFO_" + _mode.ToString());
		}
	}
}