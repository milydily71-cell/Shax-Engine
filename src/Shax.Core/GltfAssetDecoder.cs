using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace Shax;

public sealed class GltfAsset
{
    public string Version { get; init; } = "2.0";
    public List<GltfMesh> Meshes { get; } = new();
    public List<GltfNode> Nodes { get; } = new();
    public List<GltfSkin> Skins { get; } = new();
    public List<GltfMaterial> Materials { get; } = new();
    public List<GltfAnimationClip> Animations { get; } = new();
    public List<GltfImage> Images { get; } = new();
    public List<GltfTexture> Textures { get; } = new();
    public List<int> SceneRoots { get; } = new();
    public int ActiveSceneIndex { get; init; }
    public long SourceBytes { get; init; }
}

public sealed class GltfMesh
{
    public string Name { get; init; } = "Mesh";
    public List<GltfPrimitive> Primitives { get; } = new();
}

public sealed class GltfPrimitive
{
    public Vector3[] Positions { get; init; } = Array.Empty<Vector3>();
    public Vector3[] Normals { get; init; } = Array.Empty<Vector3>();
    public Vector2[] TexCoords0 { get; init; } = Array.Empty<Vector2>();
    public Vector4[] Tangents { get; init; } = Array.Empty<Vector4>();
    public Vector4[] Weights0 { get; init; } = Array.Empty<Vector4>();
    public Vector4[] Joints0 { get; init; } = Array.Empty<Vector4>();
    public uint[] Indices { get; init; } = Array.Empty<uint>();
    public int MaterialIndex { get; init; } = -1;
    public int Mode { get; init; } = 4;
    public int VertexCount => Positions.Length;
    public int TriangleCount => Mode == 4 ? Indices.Length / 3 : 0;
}

public sealed class GltfNode
{
    public string Name { get; set; } = "Node";
    public int MeshIndex { get; init; } = -1;
    public int SkinIndex { get; init; } = -1;
    public List<int> Children { get; } = new();
    public Vector3 Translation { get; set; }
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public Vector3 Scale { get; set; } = Vector3.One;
    public Matrix4x4 LocalMatrix => Matrix4x4.CreateScale(Scale) *
        Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(Rotation)) *
        Matrix4x4.CreateTranslation(Translation);
}

public sealed class GltfSkin
{
    public string Name { get; init; } = "Skin";
    public int SkeletonRoot { get; init; } = -1;
    public int[] Joints { get; init; } = Array.Empty<int>();
    public Matrix4x4[] InverseBindMatrices { get; init; } = Array.Empty<Matrix4x4>();

    public Matrix4x4[] BuildJointMatrices(IReadOnlyList<GltfNode> nodes)
    {
        var worlds = GltfAssetDecoder.BuildWorldMatrices(nodes);
        var result = new Matrix4x4[Joints.Length];
        for (var i = 0; i < Joints.Length; i++)
        {
            var node = Joints[i];
            var inverseBind = i < InverseBindMatrices.Length ? InverseBindMatrices[i] : Matrix4x4.Identity;
            result[i] = inverseBind * worlds[node];
        }
        return result;
    }
}

public sealed class GltfMaterial
{
    public string Name { get; init; } = "Material";
    public Vector4 BaseColor { get; init; } = Vector4.One;
    public float Metallic { get; init; } = 1f;
    public float Roughness { get; init; } = 1f;
    public int BaseColorTexture { get; init; } = -1;
    public int MetallicRoughnessTexture { get; init; } = -1;
    public int NormalTexture { get; init; } = -1;
    public int EmissiveTexture { get; init; } = -1;
    public Vector3 EmissiveFactor { get; init; }
    public bool DoubleSided { get; init; }
    public string AlphaMode { get; init; } = "OPAQUE";
}

public sealed class GltfTexture
{
    public string Name { get; init; } = "Texture";
    public int SourceImageIndex { get; init; } = -1;
    public int SamplerIndex { get; init; } = -1;
}

public sealed class GltfImage
{
    public string Name { get; init; } = "Image";
    public string MimeType { get; init; } = "application/octet-stream";
    public byte[] Bytes { get; init; } = Array.Empty<byte>();
}

public enum GltfAnimationPath { Translation, Rotation, Scale, Weights }
public enum GltfInterpolation { Linear, Step, CubicSpline }

public sealed class GltfAnimationTrack
{
    public int NodeIndex { get; init; }
    public GltfAnimationPath Path { get; init; }
    public GltfInterpolation Interpolation { get; init; }
    public float[] Times { get; init; } = Array.Empty<float>();
    public Vector4[] Values { get; init; } = Array.Empty<Vector4>();
    public Vector4[] InTangents { get; init; } = Array.Empty<Vector4>();
    public Vector4[] OutTangents { get; init; } = Array.Empty<Vector4>();

    public Vector4 Sample(float time)
    {
        if (Times.Length == 0 || Values.Length == 0) return Vector4.Zero;
        if (time <= Times[0]) return Values[0];
        var last = Times.Length - 1;
        if (time >= Times[last]) return Values[last];
        var hi = Array.BinarySearch(Times, time);
        if (hi >= 0) return Values[hi];
        hi = ~hi;
        var lo = hi - 1;
        if (Interpolation == GltfInterpolation.Step) return Values[lo];
        var dt = MathF.Max(1e-6f, Times[hi] - Times[lo]);
        var t = Math.Clamp((time - Times[lo]) / dt, 0f, 1f);
        if (Interpolation == GltfInterpolation.CubicSpline &&
            InTangents.Length == Times.Length && OutTangents.Length == Times.Length)
        {
            var t2 = t * t; var t3 = t2 * t;
            var h00 = 2 * t3 - 3 * t2 + 1;
            var h10 = t3 - 2 * t2 + t;
            var h01 = -2 * t3 + 3 * t2;
            var h11 = t3 - t2;
            return h00 * Values[lo] + h10 * dt * OutTangents[lo] +
                   h01 * Values[hi] + h11 * dt * InTangents[hi];
        }
        if (Path == GltfAnimationPath.Rotation)
        {
            var qa = NormalizeQuaternion(Values[lo]);
            var qb = NormalizeQuaternion(Values[hi]);
            var q = Quaternion.Slerp(qa, qb, t);
            return new Vector4(q.X, q.Y, q.Z, q.W);
        }
        return Vector4.Lerp(Values[lo], Values[hi], t);
    }

    private static Quaternion NormalizeQuaternion(Vector4 v)
    {
        var q = new Quaternion(v.X, v.Y, v.Z, v.W);
        return q.LengthSquared() < 1e-8f ? Quaternion.Identity : Quaternion.Normalize(q);
    }
}

public sealed class GltfAnimationClip
{
    public string Name { get; init; } = "Animation";
    public List<GltfAnimationTrack> Tracks { get; } = new();
    public float Duration => Tracks.SelectMany(t => t.Times).DefaultIfEmpty(0).Max();

    public void Apply(float time, IList<GltfNode> nodes, bool loop = true)
    {
        if (nodes is null) throw new ArgumentNullException(nameof(nodes));
        var duration = Duration;
        if (loop && duration > 0) time = ((time % duration) + duration) % duration;
        else time = Math.Clamp(time, 0, duration);
        foreach (var track in Tracks)
        {
            if ((uint)track.NodeIndex >= (uint)nodes.Count) continue;
            var value = track.Sample(time);
            var node = nodes[track.NodeIndex];
            switch (track.Path)
            {
                case GltfAnimationPath.Translation: node.Translation = new Vector3(value.X, value.Y, value.Z); break;
                case GltfAnimationPath.Scale: node.Scale = new Vector3(value.X, value.Y, value.Z); break;
                case GltfAnimationPath.Rotation:
                    var q = new Quaternion(value.X, value.Y, value.Z, value.W);
                    node.Rotation = q.LengthSquared() < 1e-8f ? Quaternion.Identity : Quaternion.Normalize(q);
                    break;
            }
        }
    }
}

/// <summary>Decodes embedded GLB 2.0 geometry, indices, materials, textures, skins and animation tracks.</summary>
public static class GltfAssetDecoder
{
    private const uint GlbMagic = 0x46546C67, JsonChunk = 0x4E4F534A, BinChunk = 0x004E4942;
    private const int MaxBytes = 512 * 1024 * 1024;

    private sealed record BufferView(int Offset, int Length, int Stride);
    private sealed record Accessor(int View, int Offset, int Count, int ComponentType, string Type, bool Normalized);

    public static GltfAsset DecodeFile(string path) => Decode(File.ReadAllBytes(path));

    public static GltfAsset Decode(byte[] bytes)
    {
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length < 20 || bytes.Length > MaxBytes) throw new InvalidDataException("GLB size is outside supported bounds.");
        if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0, 4)) != GlbMagic ||
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4, 4)) != 2 ||
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8, 4)) != bytes.Length)
            throw new InvalidDataException("Expected a valid GLB 2.0 header and matching total length.");

        byte[]? jsonBytes = null;
        byte[] bin = Array.Empty<byte>();
        var offset = 12;
        while (offset + 8 <= bytes.Length)
        {
            var length = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4)));
            var type = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 4, 4));
            offset += 8;
            if (length < 0 || offset + (long)length > bytes.Length) throw new InvalidDataException("GLB chunk exceeds file bounds.");
            if (type == JsonChunk && jsonBytes is null) jsonBytes = bytes.AsSpan(offset, length).ToArray();
            else if (type == BinChunk && bin.Length == 0) bin = bytes.AsSpan(offset, length).ToArray();
            offset += length;
        }
        if (jsonBytes is null) throw new InvalidDataException("GLB has no JSON chunk.");

        using var doc = JsonDocument.Parse(jsonBytes);
        var root = doc.RootElement;
        var asset = new GltfAsset { SourceBytes = bytes.LongLength, ActiveSceneIndex = Int(root, "scene", 0) };
        var assetVersion = root.TryGetProperty("asset", out var assetNode) ? Str(assetNode, "version", "") : "";
        if (assetVersion != "2.0") throw new InvalidDataException("Only glTF 2.0 assets are supported.");

        var views = ReadArray(root, "bufferViews").Select(v => new BufferView(Int(v, "byteOffset", 0), Int(v, "byteLength", 0), Int(v, "byteStride", 0))).ToArray();
        var accessors = ReadArray(root, "accessors").Select(a => new Accessor(Int(a, "bufferView", -1), Int(a, "byteOffset", 0),
            Int(a, "count", 0), Int(a, "componentType", 5126), Str(a, "type", "SCALAR"), Bool(a, "normalized", false))).ToArray();

        float[] ReadFloats(int index)
        {
            var a = GetAccessor(accessors, index);
            var comps = Components(a.Type);
            var size = ComponentBytes(a.ComponentType);
            var view = GetView(views, a.View);
            var stride = view.Stride > 0 ? view.Stride : comps * size;
            var result = new float[checked(a.Count * comps)];
            for (var i = 0; i < a.Count; i++)
            for (var j = 0; j < comps; j++)
                result[i * comps + j] = ReadComponent(bin, checked(view.Offset + a.Offset + i * stride + j * size), a.ComponentType, a.Normalized);
            return result;
        }

        uint[] ReadIndices(int index)
        {
            var a = GetAccessor(accessors, index);
            if (a.Type != "SCALAR" || (a.ComponentType != 5121 && a.ComponentType != 5123 && a.ComponentType != 5125))
                throw new InvalidDataException("Index accessor must be unsigned byte, short or int SCALAR.");
            var view = GetView(views, a.View);
            var size = ComponentBytes(a.ComponentType);
            var stride = view.Stride > 0 ? view.Stride : size;
            var result = new uint[a.Count];
            for (var i = 0; i < a.Count; i++)
            {
                var at = checked(view.Offset + a.Offset + i * stride);
                result[i] = a.ComponentType switch
                {
                    5121 => bin[at],
                    5123 => BinaryPrimitives.ReadUInt16LittleEndian(bin.AsSpan(at, 2)),
                    _ => BinaryPrimitives.ReadUInt32LittleEndian(bin.AsSpan(at, 4))
                };
            }
            return result;
        }

        Matrix4x4[] ReadMatrices(int index)
        {
            var values = ReadFloats(index);
            if (values.Length % 16 != 0) throw new InvalidDataException("Inverse bind matrix accessor must be MAT4.");
            var result = new Matrix4x4[values.Length / 16];
            for (var i = 0; i < result.Length; i++)
            {
                var v = values.AsSpan(i * 16, 16);
                result[i] = new Matrix4x4(v[0],v[1],v[2],v[3], v[4],v[5],v[6],v[7],
                    v[8],v[9],v[10],v[11], v[12],v[13],v[14],v[15]);
            }
            return result;
        }

        foreach (var meshJson in ReadArray(root, "meshes"))
        {
            var mesh = new GltfMesh { Name = Str(meshJson, "name", "Mesh " + asset.Meshes.Count) };
            foreach (var prim in ReadArray(meshJson, "primitives"))
            {
                var attrs = prim.TryGetProperty("attributes", out var at) ? at : default;
                var positionIndex = Int(attrs, "POSITION", -1);
                if (positionIndex < 0) throw new InvalidDataException("Every mesh primitive requires POSITION.");
                var positions = ToVector3(ReadFloats(positionIndex));
                var normals = Int(attrs, "NORMAL", -1) is var ni && ni >= 0 ? ToVector3(ReadFloats(ni)) : Array.Empty<Vector3>();
                var uvs = Int(attrs, "TEXCOORD_0", -1) is var ui && ui >= 0 ? ToVector2(ReadFloats(ui)) : Array.Empty<Vector2>();
                var tangents = Int(attrs, "TANGENT", -1) is var ti && ti >= 0 ? ToVector4(ReadFloats(ti)) : Array.Empty<Vector4>();
                var joints = Int(attrs, "JOINTS_0", -1) is var ji && ji >= 0 ? ToVector4(ReadFloats(ji)) : Array.Empty<Vector4>();
                var weights = Int(attrs, "WEIGHTS_0", -1) is var wi && wi >= 0 ? ToVector4(ReadFloats(wi)) : Array.Empty<Vector4>();
                var indexAccessor = Int(prim, "indices", -1);
                var indices = indexAccessor >= 0 ? ReadIndices(indexAccessor) : Enumerable.Range(0, positions.Length).Select(i => (uint)i).ToArray();
                mesh.Primitives.Add(new GltfPrimitive { Positions = positions, Normals = normals, TexCoords0 = uvs,
                    Tangents = tangents, Joints0 = joints, Weights0 = weights, Indices = indices,
                    MaterialIndex = Int(prim, "material", -1), Mode = Int(prim, "mode", 4) });
            }
            asset.Meshes.Add(mesh);
        }

        foreach (var nodeJson in ReadArray(root, "nodes"))
        {
            var translation = FloatArray(nodeJson, "translation", new[] { 0f, 0f, 0f });
            var rotation = FloatArray(nodeJson, "rotation", new[] { 0f, 0f, 0f, 1f });
            var scale = FloatArray(nodeJson, "scale", new[] { 1f, 1f, 1f });
            var node = new GltfNode { Name = Str(nodeJson, "name", "Node " + asset.Nodes.Count),
                MeshIndex = Int(nodeJson, "mesh", -1), SkinIndex = Int(nodeJson, "skin", -1),
                Translation = new Vector3(translation[0], translation[1], translation[2]),
                Rotation = Normalize(new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3])),
                Scale = new Vector3(scale[0], scale[1], scale[2]) };
            foreach (var child in IntArray(nodeJson, "children")) node.Children.Add(child);
            asset.Nodes.Add(node);
        }

        foreach (var skinJson in ReadArray(root, "skins"))
        {
            var joints = IntArray(skinJson, "joints");
            var inverse = Int(skinJson, "inverseBindMatrices", -1);
            asset.Skins.Add(new GltfSkin { Name = Str(skinJson, "name", "Skin " + asset.Skins.Count),
                SkeletonRoot = Int(skinJson, "skeleton", -1), Joints = joints,
                InverseBindMatrices = inverse >= 0 ? ReadMatrices(inverse) : Enumerable.Repeat(Matrix4x4.Identity, joints.Length).ToArray() });
        }

        foreach (var materialJson in ReadArray(root, "materials"))
        {
            var pbr = materialJson.TryGetProperty("pbrMetallicRoughness", out var p) ? p : default;
            var color = FloatArray(pbr, "baseColorFactor", new[] { 1f, 1f, 1f, 1f });
            var emissive = FloatArray(materialJson, "emissiveFactor", new[] { 0f, 0f, 0f });
            asset.Materials.Add(new GltfMaterial { Name = Str(materialJson, "name", "Material " + asset.Materials.Count),
                BaseColor = new Vector4(color[0],color[1],color[2],color[3]), Metallic = Float(pbr,"metallicFactor",1f),
                Roughness = Float(pbr,"roughnessFactor",1f), BaseColorTexture = TextureIndex(pbr,"baseColorTexture"),
                MetallicRoughnessTexture = TextureIndex(pbr,"metallicRoughnessTexture"), NormalTexture = TextureIndex(materialJson,"normalTexture"),
                EmissiveTexture = TextureIndex(materialJson,"emissiveTexture"), EmissiveFactor = new Vector3(emissive[0],emissive[1],emissive[2]),
                DoubleSided = Bool(materialJson,"doubleSided",false), AlphaMode = Str(materialJson,"alphaMode","OPAQUE") });
        }

        foreach (var imageJson in ReadArray(root, "images"))
        {
            var name = Str(imageJson, "name", "Image " + asset.Images.Count);
            var viewIndex = Int(imageJson, "bufferView", -1);
            var mime = Str(imageJson, "mimeType", "application/octet-stream");
            byte[] imageBytes = Array.Empty<byte>();
            if (viewIndex >= 0)
            {
                var view = GetView(views, viewIndex);
                if (view.Offset < 0 || view.Length < 0 || view.Offset + (long)view.Length > bin.Length) throw new InvalidDataException("Image bufferView exceeds BIN chunk.");
                imageBytes = bin.AsSpan(view.Offset, view.Length).ToArray();
            }
            else if (imageJson.TryGetProperty("uri", out var uri) && uri.GetString() is string uriText && uriText.StartsWith("data:", StringComparison.Ordinal))
                imageBytes = DecodeDataUri(uriText, out mime);
            asset.Images.Add(new GltfImage { Name = name, MimeType = mime, Bytes = imageBytes });
        }

        foreach (var textureJson in ReadArray(root, "textures"))
            asset.Textures.Add(new GltfTexture { Name = Str(textureJson, "name", "Texture " + asset.Textures.Count),
                SourceImageIndex = Int(textureJson, "source", -1), SamplerIndex = Int(textureJson, "sampler", -1) });

        foreach (var animJson in ReadArray(root, "animations"))
        {
            var samplers = ReadArray(animJson, "samplers").ToArray();
            var clip = new GltfAnimationClip { Name = Str(animJson, "name", "Animation " + asset.Animations.Count) };
            foreach (var channel in ReadArray(animJson, "channels"))
            {
                var samplerIndex = Int(channel, "sampler", -1);
                if (samplerIndex < 0 || samplerIndex >= samplers.Length) continue;
                var sampler = samplers[samplerIndex];
                var target = channel.TryGetProperty("target", out var t) ? t : default;
                var nodeIndex = Int(target, "node", -1);
                if (nodeIndex < 0 || nodeIndex >= asset.Nodes.Count) continue;
                var path = Str(target, "path", "");
                if (!Enum.TryParse<GltfAnimationPath>(path, true, out var animationPath)) continue;
                var inputIndex = Int(sampler, "input", -1);
                var outputIndex = Int(sampler, "output", -1);
                if (inputIndex < 0 || outputIndex < 0) continue;
                var times = ReadFloats(inputIndex);
                var raw = ReadFloats(outputIndex);
                var interpolation = Str(sampler, "interpolation", "LINEAR") switch
                {
                    "STEP" => GltfInterpolation.Step,
                    "CUBICSPLINE" => GltfInterpolation.CubicSpline,
                    _ => GltfInterpolation.Linear
                };
                var width = animationPath == GltfAnimationPath.Rotation ? 4 : animationPath == GltfAnimationPath.Weights ? 1 : 3;
                var values = new Vector4[times.Length];
                var ins = new Vector4[times.Length]; var outs = new Vector4[times.Length];
                for (var i = 0; i < times.Length; i++)
                {
                    if (interpolation == GltfInterpolation.CubicSpline)
                    {
                        ins[i] = ReadVector(raw, (i * 3) * width, width);
                        values[i] = ReadVector(raw, (i * 3 + 1) * width, width);
                        outs[i] = ReadVector(raw, (i * 3 + 2) * width, width);
                    }
                    else values[i] = ReadVector(raw, i * width, width);
                }
                clip.Tracks.Add(new GltfAnimationTrack { NodeIndex = nodeIndex, Path = animationPath,
                    Interpolation = interpolation, Times = times, Values = values, InTangents = ins, OutTangents = outs });
            }
            asset.Animations.Add(clip);
        }

        var scenes = ReadArray(root, "scenes").ToArray();
        if (asset.ActiveSceneIndex >= 0 && asset.ActiveSceneIndex < scenes.Length)
            foreach (var node in IntArray(scenes[asset.ActiveSceneIndex], "nodes")) asset.SceneRoots.Add(node);
        return asset;
    }

    public static Matrix4x4[] BuildWorldMatrices(IReadOnlyList<GltfNode> nodes)
    {
        var world = new Matrix4x4[nodes.Count];
        var visiting = new bool[nodes.Count];
        Matrix4x4 Visit(int i)
        {
            if ((uint)i >= (uint)nodes.Count) throw new InvalidDataException("Node hierarchy index is out of range.");
            if (visiting[i]) throw new InvalidDataException("Node hierarchy contains a cycle.");
            if (world[i] != default) return world[i];
            visiting[i] = true;
            var parent = FindParent(nodes, i);
            world[i] = parent < 0 ? nodes[i].LocalMatrix : nodes[i].LocalMatrix * Visit(parent);
            visiting[i] = false;
            return world[i];
        }
        for (var i = 0; i < nodes.Count; i++) Visit(i);
        return world;
    }

    private static int FindParent(IReadOnlyList<GltfNode> nodes, int child)
    {
        for (var i = 0; i < nodes.Count; i++) if (nodes[i].Children.Contains(child)) return i;
        return -1;
    }
    private static Vector3[] ToVector3(float[] v) { if (v.Length % 3 != 0) throw new InvalidDataException("Expected VEC3 accessor."); return Enumerable.Range(0,v.Length/3).Select(i => new Vector3(v[i*3],v[i*3+1],v[i*3+2])).ToArray(); }
    private static Vector2[] ToVector2(float[] v) { if (v.Length % 2 != 0) throw new InvalidDataException("Expected VEC2 accessor."); return Enumerable.Range(0,v.Length/2).Select(i => new Vector2(v[i*2],v[i*2+1])).ToArray(); }
    private static Vector4[] ToVector4(float[] v) { if (v.Length % 4 != 0) throw new InvalidDataException("Expected VEC4 accessor."); return Enumerable.Range(0,v.Length/4).Select(i => new Vector4(v[i*4],v[i*4+1],v[i*4+2],v[i*4+3])).ToArray(); }
    private static Vector4 ReadVector(float[] raw, int offset, int width) => new(raw[offset], width > 1 ? raw[offset+1] : 0, width > 2 ? raw[offset+2] : 0, width > 3 ? raw[offset+3] : 0);
    private static Quaternion Normalize(Quaternion q) => q.LengthSquared() < 1e-8f ? Quaternion.Identity : Quaternion.Normalize(q);
    private static int Components(string type) => type switch { "SCALAR" => 1, "VEC2" => 2, "VEC3" => 3, "VEC4" => 4, "MAT4" => 16, _ => throw new InvalidDataException("Unsupported accessor type: " + type) };
    private static int ComponentBytes(int type) => type switch { 5120 or 5121 => 1, 5122 or 5123 => 2, 5125 or 5126 => 4, _ => throw new InvalidDataException("Unsupported accessor component type.") };
    private static float ReadComponent(byte[] bytes, int at, int type, bool normalized)
    {
        if (at < 0 || at + ComponentBytes(type) > bytes.Length) throw new InvalidDataException("Accessor exceeds embedded BIN data.");
        return type switch
        {
            5126 => BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(at,4))),
            5120 => normalized ? MathF.Max(-1f, (sbyte)bytes[at] / 127f) : (sbyte)bytes[at],
            5121 => normalized ? bytes[at] / 255f : bytes[at],
            5122 => normalized ? MathF.Max(-1f, BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(at,2)) / 32767f) : BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(at,2)),
            5123 => normalized ? BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(at,2)) / 65535f : BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(at,2)),
            5125 => normalized ? BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(at,4)) / 4294967295f : BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(at,4)),
            _ => throw new InvalidDataException("Unsupported accessor component type.")
        };
    }
    private static Accessor GetAccessor(Accessor[] list, int index) => (uint)index < (uint)list.Length ? list[index] : throw new InvalidDataException("Accessor index is out of range.");
    private static BufferView GetView(BufferView[] list, int index) => (uint)index < (uint)list.Length ? list[index] : throw new InvalidDataException("Buffer view index is out of range.");
    private static IEnumerable<JsonElement> ReadArray(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array ? v.EnumerateArray() : Enumerable.Empty<JsonElement>();
    private static int Int(JsonElement e, string name, int fallback) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.TryGetInt32(out var n) ? n : fallback;
    private static float Float(JsonElement e, string name, float fallback) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.TryGetSingle(out var n) ? n : fallback;
    private static string Str(JsonElement e, string name, string fallback) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? fallback : fallback;
    private static bool Bool(JsonElement e, string name, bool fallback) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) ? v.GetBoolean() : fallback;
    private static int[] IntArray(JsonElement e, string name) => ReadArray(e,name).Select(x => x.TryGetInt32(out var n) ? n : -1).ToArray();
    private static float[] FloatArray(JsonElement e, string name, float[] fallback)
    {
        var items = ReadArray(e, name).ToArray();
        return items.Length == 0 ? fallback : items.Select(x => x.TryGetSingle(out var f) ? f : 0f).ToArray();
    }
    private static int TextureIndex(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var texture) ? Int(texture,"index",-1) : -1;
    private static byte[] DecodeDataUri(string uri, out string mime)
    {
        var comma = uri.IndexOf(',');
        if (comma < 0) throw new InvalidDataException("Malformed data URI.");
        var header = uri.Substring(5, comma - 5);
        mime = header.Split(';')[0];
        return header.EndsWith(";base64", StringComparison.OrdinalIgnoreCase)
            ? Convert.FromBase64String(uri.Substring(comma + 1))
            : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(uri.Substring(comma + 1)));
    }
}
