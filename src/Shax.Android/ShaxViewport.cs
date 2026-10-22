using System.Runtime.InteropServices;
using Android.Content;
using Android.Opengl;
using Android.Views;
using Javax.Microedition.Khronos.Egl;
using EGLConfig = Javax.Microedition.Khronos.Egl.EGLConfig;
using EGLContext = Javax.Microedition.Khronos.Egl.EGLContext;
using EGLDisplay = Javax.Microedition.Khronos.Egl.EGLDisplay;
using IEGL10 = Javax.Microedition.Khronos.Egl.IEGL10;
using GLMatrix = Android.Opengl.Matrix;
using Javax.Microedition.Khronos.Opengles;
using Shax;

namespace Shax.Android;

/// <summary>Native C++ GLES viewport. Requests ES 3.2 and falls back directly to ES 3.0.</summary>
public sealed class ShaxViewport : GLSurfaceView
{
    private readonly ViewportRenderer _renderer;
    private readonly global::Android.Views.ScaleGestureDetector _scaleDetector;
    private readonly Es32OrEs30ContextFactory _contextFactory;
    private float _previousX, _previousY;
    private bool _wasScaling;

    public ShaxViewport(Context context, Scene scene, Entity cubeEntity, bool forceGles30 = false) : base(context)
    {
        _renderer = new ViewportRenderer(scene, cubeEntity, forceGles30);
        _scaleDetector = new global::Android.Views.ScaleGestureDetector(context, new ScaleGestureListener { OnScaleAction = factor => QueueEvent(() => _renderer.Zoom(factor)) });
        SetEGLContextClientVersion(3);
        SetEGLConfigChooser(8, 8, 8, 8, 24, 8);
        _contextFactory = new Es32OrEs30ContextFactory { ForceGles30 = forceGles30 };
        SetEGLContextFactory(_contextFactory);
        SetRenderer(_renderer);
        RenderMode = Rendermode.Continuously;
        PreserveEGLContextOnPause = false;
        KeepScreenOn = true;
    }

    public void SetMaterial(float roughness, float metallic) => QueueEvent(() => _renderer.SetMaterial(roughness, metallic));
    public void SetCameraMode(CameraMode mode) => QueueEvent(() => _renderer.SetCameraMode(mode));
    public void SetImportedAsset(GltfAsset asset) => QueueEvent(() => _renderer.SetImportedAsset(asset));
    public void ClearImportedAsset() => QueueEvent(_renderer.ClearImportedAsset);

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e is null) return false;
        _scaleDetector.OnTouchEvent(e);
        float x = e.GetX(), y = e.GetY();
        if (e.ActionMasked == MotionEventActions.Move && e.PointerCount == 1 && !_scaleDetector.IsInProgress)
        {
            var dx = x - _previousX; var dy = y - _previousY;
            if (!_wasScaling) QueueEvent(() => _renderer.Orbit(dx, dy));
        }
        _wasScaling = _scaleDetector.IsInProgress;
        _previousX = x; _previousY = y;
        return true;
    }

    private sealed class Es32OrEs30ContextFactory : Java.Lang.Object, GLSurfaceView.IEGLContextFactory
    {
        public bool ForceGles30 { get; init; }
        public EGLContext? CreateContext(IEGL10? egl, EGLDisplay? display, EGLConfig? config)
        {
            if (egl is null || display is null || config is null)
                throw new InvalidOperationException("EGL is not ready.");

            EGLContext? TryCreate(int minor)
            {
                var attributes = new[] { 0x3098, 3, 0x30FB, minor, 0x3038 };
                var requested = egl.EglCreateContext(display, config, IEGL10.EglNoContext, attributes);
                if (requested is not null && !requested.Equals(IEGL10.EglNoContext)) return requested;
                // Only the automatic fallback may use legacy client-version attributes.
                if (minor == 0)
                    return egl.EglCreateContext(display, config, IEGL10.EglNoContext, new[] { 0x3098, 3, 0x3038 });
                return requested;
            }

            var context = ForceGles30 ? TryCreate(0) : TryCreate(2);
            if (context is not null && !context.Equals(IEGL10.EglNoContext)) return context;
            // Intentional direct fallback: ES 3.1 is not a supported target for this project.
            if (ForceGles30) throw new InvalidOperationException("Could not create a strict OpenGL ES 3.0 context.");
            context = TryCreate(0);
            if (context is null || context.Equals(IEGL10.EglNoContext))
                throw new InvalidOperationException("Shax requires OpenGL ES 3.2 or OpenGL ES 3.0.");
            return context;
        }

        public void DestroyContext(IEGL10? egl, EGLDisplay? display, EGLContext? context)
        {
            if (egl is not null && display is not null && context is not null && !egl.EglDestroyContext(display, context))
                global::Android.Util.Log.Error("ShaxEngine", "Failed to destroy EGL context.");
        }
    }

    private sealed class ViewportRenderer : Java.Lang.Object, GLSurfaceView.IRenderer
    {
        [DllImport("shax", EntryPoint = "shax_init")]
        private static extern int NativeInit();
        [DllImport("shax", EntryPoint = "shax_resize")]
        private static extern void NativeResize(int width, int height);
        [DllImport("shax", EntryPoint = "shax_set_camera")]
        private static extern void NativeSetCamera(float x, float y, float z);
        [DllImport("shax", EntryPoint = "shax_set_material")]
        private static extern void NativeSetMaterial(float roughness, float metallic);
        [DllImport("shax", EntryPoint = "shax_draw")]
        private static extern void NativeDraw([In] float[] viewProjection, [In] float[] inverseViewProjection, [In] float[] cubeModel, float timeSeconds);
        [DllImport("shax", EntryPoint = "shax_shutdown")]
        private static extern void NativeShutdown();
        [DllImport("shax", EntryPoint = "shax_upload_mesh")]
        private static extern int NativeUploadMesh([In] float[] vertices, int vertexCount, [In] uint[] indices, int indexCount);
        [DllImport("shax", EntryPoint = "shax_set_skin_matrices")]
        private static extern void NativeSetSkinMatrices([In] float[] matrices, int jointCount);
        [DllImport("shax", EntryPoint = "shax_clear_imported_mesh")]
        private static extern void NativeClearImportedMesh();
        [DllImport("shax", EntryPoint = "shax_upload_base_color_texture")]
        private static extern int NativeUploadBaseColorTexture([In] byte[] rgba, int width, int height);
        [DllImport("shax", EntryPoint = "shax_clear_base_color_texture")]
        private static extern void NativeClearBaseColorTexture();
        [DllImport("shax", EntryPoint = "shax_draw_imported")]
        private static extern void NativeDrawImported([In] float[] viewProjection, [In] float[] model, float roughness, float metallic);

        private readonly Entity _cubeEntity;
        private readonly Scene _scene;
        private readonly bool _forceGles30;
        private readonly float[] _projection = new float[16];
        private readonly float[] _view = new float[16];
        private readonly float[] _viewProjection = new float[16];
        private readonly float[] _inverseViewProjection = new float[16];
        private readonly float[] _cubeModel = new float[16];
        private int _width = 1, _height = 1;
        private float _yaw = 0.65f, _pitch = 0.35f, _distance = 7.5f;
        private float _roughness = 0.28f, _metallic = 0.08f;
        private CameraMode _cameraMode = CameraMode.Orbit;
        private readonly FpsCounter _fps = new();
        private long _startMilliseconds, _lastFrameMilliseconds;
        private int _frameCount;
        private GltfAsset? _importedAsset;
        private float _animationTime;
        private bool _hasSkinWeights;

        public ViewportRenderer(Scene scene, Entity cubeEntity, bool forceGles30) { _scene = scene; _cubeEntity = cubeEntity; _forceGles30 = forceGles30; }

        public void OnSurfaceCreated(IGL10? gl, EGLConfig? config)
        {
            try
            {
                var version = GLES30.GlGetString(GLES30.GlVersion) ?? string.Empty;
                var match = System.Text.RegularExpressions.Regex.Match(version, @"OpenGL ES (\d+)\.(\d+)");
                if (!match.Success) throw new InvalidOperationException("Unable to identify the OpenGL ES driver version: " + version);
                var major = int.Parse(match.Groups[1].Value);
                var minor = int.Parse(match.Groups[2].Value);
                if (major != 3 || (minor != 0 && minor != 2) || (_forceGles30 && minor != 0))
                    throw new InvalidOperationException("Shax accepts only OpenGL ES 3.2 or ES 3.0; active driver is " + version);
                if (NativeInit() == 0) throw new InvalidOperationException("Native GLES renderer initialization failed.");
                global::Android.Util.Log.Info("ShaxEngine", "Active OpenGL ES driver: " + version + ". Native libshax.so renderer initialized.");
                _startMilliseconds = Java.Lang.JavaSystem.CurrentTimeMillis();
                _lastFrameMilliseconds = _startMilliseconds;
                NativeResize(_width, _height);
                if (_importedAsset != null) UploadImportedAsset(_importedAsset);
            }
            catch (DllNotFoundException ex)
            {
                global::Android.Util.Log.Error("ShaxEngine", "libshax.so is missing from the APK: " + ex);
                throw;
            }
        }

        public void OnSurfaceChanged(IGL10? gl, int width, int height)
        {
            _width = Math.Max(1, width); _height = Math.Max(1, height);
            NativeResize(_width, _height);
            GLMatrix.PerspectiveM(_projection, 0, 55f, (float)_width / _height, 0.1f, 1000f);
        }

        public void OnDrawFrame(IGL10? gl)
        {
            var now = Java.Lang.JavaSystem.CurrentTimeMillis();
            var deltaTime = Math.Clamp((now - _lastFrameMilliseconds) / 1000f, 0.001f, 0.1f);
            _lastFrameMilliseconds = now;
            _scene.Tick(deltaTime);
            _fps.AddFrame(deltaTime);
            if ((++_frameCount % 300) == 0)
                global::Android.Util.Log.Info("ShaxEngine", $"Editor FPS={_fps.AverageFps:0.0}, frame={_fps.FrameMilliseconds:0.00} ms, physics collisions={_scene.Physics.CollisionCount}");

            var targetX = _cubeEntity.Transform.WorldPosition.X;
            var targetY = _cubeEntity.Transform.WorldPosition.Y;
            var targetZ = _cubeEntity.Transform.WorldPosition.Z;
            var cp = MathF.Cos(_pitch);
            float eyeX, eyeY, eyeZ, lookX, lookY, lookZ;
            if (_cameraMode == CameraMode.FirstPerson)
            {
                eyeX = targetX;
                eyeY = targetY + 1.55f;
                eyeZ = targetZ;
                lookX = eyeX - MathF.Sin(_yaw) * cp * 10f;
                lookY = eyeY - MathF.Sin(_pitch) * 10f;
                lookZ = eyeZ - MathF.Cos(_yaw) * cp * 10f;
            }
            else
            {
                var cameraDistance = _cameraMode == CameraMode.ThirdPerson ? MathF.Min(_distance, 4.5f) : _distance;
                eyeX = targetX + cameraDistance * cp * MathF.Sin(_yaw);
                eyeY = targetY + cameraDistance * MathF.Sin(_pitch);
                eyeZ = targetZ + cameraDistance * cp * MathF.Cos(_yaw);
                lookX = targetX; lookY = targetY; lookZ = targetZ;
            }
            GLMatrix.SetLookAtM(_view, 0, eyeX, eyeY, eyeZ, lookX, lookY, lookZ, 0f, 1f, 0f);
            GLMatrix.MultiplyMM(_viewProjection, 0, _projection, 0, _view, 0);
            if (!GLMatrix.InvertM(_inverseViewProjection, 0, _viewProjection, 0))
                Array.Copy(_viewProjection, _inverseViewProjection, _viewProjection.Length);
            var world = _cubeEntity.Transform.WorldMatrix;
            _cubeModel[0]=world.M11; _cubeModel[1]=world.M12; _cubeModel[2]=world.M13; _cubeModel[3]=world.M14;
            _cubeModel[4]=world.M21; _cubeModel[5]=world.M22; _cubeModel[6]=world.M23; _cubeModel[7]=world.M24;
            _cubeModel[8]=world.M31; _cubeModel[9]=world.M32; _cubeModel[10]=world.M33; _cubeModel[11]=world.M34;
            _cubeModel[12]=world.M41; _cubeModel[13]=world.M42; _cubeModel[14]=world.M43; _cubeModel[15]=world.M44;
            var time = (now - _startMilliseconds) / 1000f;
            NativeSetCamera(eyeX, eyeY, eyeZ);
            NativeSetMaterial(_roughness, _metallic);
            NativeDraw(_viewProjection, _inverseViewProjection, _cubeModel, time);
            if (_importedAsset != null)
            {
                UpdateImportedSkin(deltaTime);
                NativeDrawImported(_viewProjection, _cubeModel, _roughness, _metallic);
            }
        }

        public void SetCameraMode(CameraMode mode) => _cameraMode = mode;

        public void SetImportedAsset(GltfAsset asset)
        {
            _importedAsset = asset ?? throw new ArgumentNullException(nameof(asset));
            _animationTime = 0;
            UploadImportedAsset(asset);
        }

        public void ClearImportedAsset()
        {
            _importedAsset = null;
            _hasSkinWeights = false;
            NativeClearImportedMesh();
            NativeClearBaseColorTexture();
            NativeSetSkinMatrices(Array.Empty<float>(), 0);
        }

        private void UploadImportedAsset(GltfAsset asset)
        {
            var primitives = asset.Meshes.SelectMany(m => m.Primitives).Where(p => p.Positions.Length > 0).ToArray();
            if (primitives.Length == 0) { NativeClearImportedMesh(); return; }
            NativeClearBaseColorTexture();
            var vertices = new List<float>();
            var indices = new List<uint>();
            _hasSkinWeights = asset.Skins.Count > 0 && primitives.Any(p => p.Joints0.Length == p.VertexCount && p.Weights0.Length == p.VertexCount);
            foreach (var primitive in primitives)
            {
                var triangleIndices = Triangulate(primitive);
                if (triangleIndices.Length == 0) continue;
                var normals = primitive.Normals.Length == primitive.VertexCount ? primitive.Normals : GenerateNormals(primitive.Positions, triangleIndices);
                var material = (uint)primitive.MaterialIndex < (uint)asset.Materials.Count ? asset.Materials[primitive.MaterialIndex] : null;
                var color = material?.BaseColor ?? new Vector4(0.65f, 0.72f, 0.82f, 1f);
                var vertexBase = (uint)(vertices.Count / 19);
                for (var i = 0; i < primitive.VertexCount; i++)
                {
                    var p = primitive.Positions[i];
                    var n = normals[i];
                    var j = primitive.Joints0.Length == primitive.VertexCount ? primitive.Joints0[i] : Vector4.Zero;
                    var w = primitive.Weights0.Length == primitive.VertexCount ? primitive.Weights0[i] : Vector4.Zero;
                    var uv = primitive.TexCoords0.Length == primitive.VertexCount ? primitive.TexCoords0[i] : Vector2.Zero;
                    var sum = w.X + w.Y + w.Z + w.W;
                    if (sum > 1e-6f) w /= sum;
                    vertices.AddRange(new[] { p.X,p.Y,p.Z,n.X,n.Y,n.Z,color.X,color.Y,color.Z,j.X,j.Y,j.Z,j.W,w.X,w.Y,w.Z,w.W,uv.X,uv.Y });
                }
                foreach (var index in triangleIndices)
                {
                    if (index >= primitive.VertexCount) throw new InvalidDataException("GLB mesh index is outside its vertex accessor.");
                    indices.Add(vertexBase + index);
                }
            }
            if (vertices.Count == 0 || indices.Count == 0) { NativeClearImportedMesh(); return; }
            var uploaded = NativeUploadMesh(vertices.ToArray(), vertices.Count / 19, indices.ToArray(), indices.Count);
            if (uploaded == 0) throw new InvalidOperationException("Native GPU upload rejected the imported GLB mesh.");
            if (_hasSkinWeights) UpdateImportedSkin(0);
            else NativeSetSkinMatrices(Array.Empty<float>(), 0);
            UploadFirstBaseColorTexture(asset);
            global::Android.Util.Log.Info("ShaxEngine", $"Uploaded GLB geometry: {vertices.Count / 19} vertices, {indices.Count / 3} triangles.");
        }

        private static void UploadFirstBaseColorTexture(GltfAsset asset)
        {
            foreach (var material in asset.Materials)
            {
                if ((uint)material.BaseColorTexture >= (uint)asset.Textures.Count) continue;
                var imageIndex = asset.Textures[material.BaseColorTexture].SourceImageIndex;
                if ((uint)imageIndex >= (uint)asset.Images.Count) continue;
                var encoded = asset.Images[imageIndex].Bytes;
                if (encoded.Length == 0) continue;
                using var bitmap = global::Android.Graphics.BitmapFactory.DecodeByteArray(encoded, 0, encoded.Length);
                if (bitmap is null) continue;
                var width = bitmap.Width; var height = bitmap.Height;
                if (width <= 0 || height <= 0 || (long)width * height > 16_777_216) continue;
                var pixels = new int[width * height];
                bitmap.GetPixels(pixels, 0, width, 0, 0, width, height);
                var rgba = new byte[pixels.Length * 4];
                for (var i = 0; i < pixels.Length; i++)
                {
                    var color = pixels[i]; var o = i * 4;
                    rgba[o] = (byte)((color >> 16) & 255);
                    rgba[o+1] = (byte)((color >> 8) & 255);
                    rgba[o+2] = (byte)(color & 255);
                    rgba[o+3] = (byte)((color >> 24) & 255);
                }
                if (NativeUploadBaseColorTexture(rgba, width, height) != 0)
                    global::Android.Util.Log.Info("ShaxEngine", $"Uploaded GLB base-color texture {width}x{height}.");
                return;
            }
        }

        private void UpdateImportedSkin(float deltaTime)
        {
            if (!_hasSkinWeights || _importedAsset is null || _importedAsset.Skins.Count == 0) return;
            if (_importedAsset.Animations.Count > 0)
            {
                _animationTime += MathF.Max(0, deltaTime);
                _importedAsset.Animations[0].Apply(_animationTime, _importedAsset.Nodes);
            }
            var matrices = _importedAsset.Skins[0].BuildJointMatrices(_importedAsset.Nodes);
            var count = Math.Min(32, matrices.Length);
            var packed = new float[count * 16];
            for (var i = 0; i < count; i++)
            {
                var m = matrices[i]; var o = i * 16;
                packed[o]=m.M11; packed[o+1]=m.M12; packed[o+2]=m.M13; packed[o+3]=m.M14;
                packed[o+4]=m.M21; packed[o+5]=m.M22; packed[o+6]=m.M23; packed[o+7]=m.M24;
                packed[o+8]=m.M31; packed[o+9]=m.M32; packed[o+10]=m.M33; packed[o+11]=m.M34;
                packed[o+12]=m.M41; packed[o+13]=m.M42; packed[o+14]=m.M43; packed[o+15]=m.M44;
            }
            NativeSetSkinMatrices(packed, count);
        }

        private static uint[] Triangulate(GltfPrimitive primitive)
        {
            var src = primitive.Indices;
            if (primitive.Mode == 4) return src.Length % 3 == 0 ? src : Array.Empty<uint>();
            var output = new List<uint>();
            if (primitive.Mode == 5)
            {
                for (var i = 2; i < src.Length; i++)
                {
                    if ((i & 1) == 0) { output.Add(src[i-2]); output.Add(src[i-1]); output.Add(src[i]); }
                    else { output.Add(src[i-1]); output.Add(src[i-2]); output.Add(src[i]); }
                }
            }
            else if (primitive.Mode == 6)
            {
                for (var i = 2; i < src.Length; i++) { output.Add(src[0]); output.Add(src[i-1]); output.Add(src[i]); }
            }
            return output.ToArray();
        }

        private static Vector3[] GenerateNormals(Vector3[] positions, uint[] indices)
        {
            var normals = new Vector3[positions.Length];
            for (var i = 0; i + 2 < indices.Length; i += 3)
            {
                var a = checked((int)indices[i]); var b = checked((int)indices[i+1]); var d = checked((int)indices[i+2]);
                if ((uint)a >= (uint)positions.Length || (uint)b >= (uint)positions.Length || (uint)d >= (uint)positions.Length) continue;
                var n = Vector3.Cross(positions[b] - positions[a], positions[d] - positions[a]);
                normals[a] += n; normals[b] += n; normals[d] += n;
            }
            for (var i = 0; i < normals.Length; i++)
                normals[i] = normals[i].LengthSquared() > 1e-8f ? Vector3.Normalize(normals[i]) : Vector3.UnitY;
            return normals;
        }

        public void SetMaterial(float roughness, float metallic)
        {
            _roughness = Math.Clamp(roughness, 0.04f, 1f);
            _metallic = Math.Clamp(metallic, 0f, 1f);
        }

        public void Orbit(float dx, float dy)
        {
            _yaw -= dx * 0.008f;
            _pitch = Math.Clamp(_pitch + dy * 0.008f, -1.35f, 1.35f);
        }

        public void Zoom(float factor)
        {
            if (factor <= 0 || float.IsNaN(factor)) return;
            _distance = Math.Clamp(_distance / factor, 2.0f, 35.0f);
        }
    }

    private sealed class ScaleGestureListener : global::Android.Views.ScaleGestureDetector.SimpleOnScaleGestureListener
    {
        public Action<float>? OnScaleAction { get; init; }
        public override bool OnScale(global::Android.Views.ScaleGestureDetector detector)
        {
            OnScaleAction?.Invoke(detector.ScaleFactor);
            return true;
        }
    }
}
