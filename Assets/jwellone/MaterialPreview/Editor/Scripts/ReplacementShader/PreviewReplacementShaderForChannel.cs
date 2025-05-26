using System;
using UnityEngine;

#nullable enable

namespace jwelloneEditor
{
	public class PreivewReplacementShaderForChannel : IPreivewReplacementShader
	{
		public enum eMode
		{
			R,
			G,
			B,
			A,
			MAX
		}

		eMode _mode;
		[NonSerialized] Shader? _shader;

		public string displayName => $"Channel {_mode}";

		public PreivewReplacementShaderForChannel(eMode mode)
		{
			_mode = mode;
		}

		public void Draw(Camera camera)
		{
			_shader ??= Shader.Find("jwellone/Editor/MaterialPreview/DrawChannel");

			for (var i = 0; i < (int)eMode.MAX; ++i)
			{
				Shader.DisableKeyword("_CHANNEL_" + ((eMode)i).ToString());
			}

			Shader.EnableKeyword("_CHANNEL_" + _mode.ToString());

			camera.SetReplacementShader(_shader, "RenderType");
		}

		public void Reset(Camera camera)
		{
		}

		public void Release()
		{
			_shader = null;
		}
	}
}