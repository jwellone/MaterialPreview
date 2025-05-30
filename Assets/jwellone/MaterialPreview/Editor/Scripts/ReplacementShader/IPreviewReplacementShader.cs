using UnityEngine;

#nullable enable

namespace jwelloneEditor
{
	public interface IPreivewReplacementShader
	{
		string displayName { get; }
		void Draw(Camera camera);
		void Reset(Camera camera);
		void Release();
	}

	public struct CameraBackupParameters
	{
		private bool allowMSAA;
		private bool allowHDR;
		private bool useOcclusionCulling;
		private RenderingPath renderingPath;
		private Color backgroundColor;
		private CameraClearFlags clearFlags;
		private DepthTextureMode depthTextureMode;

		public void Backup(Camera camera)
		{
			allowMSAA = camera.allowMSAA;
			allowHDR = camera.allowHDR;
			useOcclusionCulling = camera.useOcclusionCulling;
			renderingPath = camera.renderingPath;
			backgroundColor = camera.backgroundColor;
			clearFlags = camera.clearFlags;
			depthTextureMode = camera.depthTextureMode;
		}

		public void Apply(Camera camera)
		{
			camera.allowMSAA = allowMSAA;
			camera.allowHDR = allowHDR;
			camera.useOcclusionCulling = useOcclusionCulling;
			camera.renderingPath = renderingPath;
			camera.backgroundColor = backgroundColor;
			camera.clearFlags = clearFlags;
			camera.depthTextureMode = depthTextureMode;
		}
	}

	public class PreviewReplacementShaderNothing : IPreivewReplacementShader
	{
		public string displayName => "Default";

		public void Draw(Camera camera)
		{
		}

		public void Reset(Camera camera)
		{
		}

		public void Release()
		{
		}
	}

	public abstract class PreviewReplacementShader : IPreivewReplacementShader
	{
		public abstract string displayName { get; }

		private CameraBackupParameters _backupParameters;
		protected virtual bool canMainCameraClearFlagsSolid => false;

		public virtual void Draw(Camera camera)
		{
			BackupParameter(camera);
		}

		public virtual void Reset(Camera camera)
		{
			ResetParamater(camera);
			camera.ResetReplacementShader();
		}

		public virtual void Release()
		{
		}

		protected void BackupParameter(Camera camera)
		{
			_backupParameters.Backup(camera);

			camera.allowMSAA = false;
			camera.allowHDR = false;
			camera.useOcclusionCulling = false;
			camera.renderingPath = RenderingPath.Forward;
			camera.backgroundColor = Color.black;
		}

		protected void ResetParamater(Camera camera)
		{
			_backupParameters.Apply(camera);
			camera.ResetReplacementShader();
		}
	}
}