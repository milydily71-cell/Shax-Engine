using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using Shax;
using Xunit;

namespace Shax.Core.Tests;

public sealed class GameplayApiTests
{
    [Fact]
    public void FixedStepPhysicsAppliesGravityAndStopsAtStaticFloor()
    {
        var scene = new Scene("Physics");
        var floor = scene.CreateEntity("Floor");
        floor.Transform.LocalPosition = new Vector3(0, -0.5f, 0);
        floor.AddComponent(new BoxCollider { Size = new Vector3(10, 1, 10) });
        var body = scene.CreateEntity("Player");
        body.Transform.LocalPosition = new Vector3(0, 2, 0);
        body.AddComponent(new BoxCollider { Size = Vector3.One });
        body.AddComponent(new Rigidbody());
        for (var i = 0; i < 180; i++) scene.Tick(1f / 60f);
        Assert.InRange(body.Transform.LocalPosition.Y, 0.45f, 2f);
    }

    [Fact]
    public void PlayerControllerMovesWithInputAndExposesGravityAndJumpApi()
    {
        var scene = new Scene("Player test");
        var player = scene.CreateEntity("Player");
        var controller = player.AddComponent(new PlayerController());
        controller.SetMoveInput(1, 0);
        scene.Tick(1f / 60f);
        scene.Tick(1f / 60f);
        Assert.NotEqual(0f, player.Transform.LocalPosition.X);
        Assert.NotNull(player.GetComponent<CharacterController>());
        Assert.True(controller.Gravity < 0);
        Assert.True(controller.JumpHeight > 0);
    }

    [Fact]
    public void CameraSupportsFirstPersonThirdPersonAndProjection()
    {
        var target = new Entity("Player");
        target.Transform.LocalPosition = new Vector3(0, 1, 0);
        var cameraEntity = new Entity("Camera");
        var camera = cameraEntity.AddComponent(new CameraComponent { Target = target, Mode = CameraMode.FirstPerson });
        var first = camera.WorldPosition;
        camera.Mode = CameraMode.ThirdPerson;
        var third = camera.WorldPosition;
        Assert.NotEqual(first, third);
        Assert.True(camera.GetProjectionMatrix(16f / 9f).M11 > 0);
    }

    [Fact]
    public void SceneManagerSwitchesAndUnloadsNamedScenes()
    {
        var manager = new SceneManager();
        var first = manager.Create("Boot");
        var second = manager.Create("Level 2");
        Assert.Same(first, manager.ActiveScene);
        Assert.Same(second, manager.SetActive("Level 2"));
        Assert.True(manager.Unload("Level 2"));
        Assert.Same(first, manager.ActiveScene);
    }

    [Fact]
    public void AnimationTrackInterpolatesJointTranslation()
    {
        var track = new GltfAnimationTrack
        {
            NodeIndex = 0,
            Path = GltfAnimationPath.Translation,
            Interpolation = GltfInterpolation.Linear,
            Times = new[] { 0f, 2f },
            Values = new[] { Vector4.Zero, new Vector4(4, 2, 0, 0) }
        };
        var sample = track.Sample(1f);
        Assert.Equal(new Vector4(2, 1, 0, 0), sample);
    }

    [Fact]
    public void GlbDecoderLoadsVertexAccessorsIndicesAndSceneRoots()
    {
        var bin = new byte[44];
        var values = new[] { 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f, 0f };
        for (var i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteInt32LittleEndian(bin.AsSpan(i * 4, 4), BitConverter.SingleToInt32Bits(values[i]));
        BinaryPrimitives.WriteUInt16LittleEndian(bin.AsSpan(36, 2), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(bin.AsSpan(38, 2), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(bin.AsSpan(40, 2), 2);

        const string json = "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"byteLength\":42}],\"bufferViews\":[{\"buffer\":0,\"byteOffset\":0,\"byteLength\":36},{\"buffer\":0,\"byteOffset\":36,\"byteLength\":6}],\"accessors\":[{\"bufferView\":0,\"componentType\":5126,\"count\":3,\"type\":\"VEC3\"},{\"bufferView\":1,\"componentType\":5123,\"count\":3,\"type\":\"SCALAR\"}],\"meshes\":[{\"name\":\"Triangle\",\"primitives\":[{\"attributes\":{\"POSITION\":0},\"indices\":1}]}],\"nodes\":[{\"name\":\"TriangleNode\",\"mesh\":0}],\"scenes\":[{\"nodes\":[0]}],\"scene\":0}";
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var jsonLength = (jsonBytes.Length + 3) & ~3;
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(0x46546C67u); writer.Write(2u);
            writer.Write((uint)(12 + 8 + jsonLength + 8 + bin.Length));
            writer.Write((uint)jsonLength); writer.Write(0x4E4F534Au); writer.Write(jsonBytes);
            for (var i = jsonBytes.Length; i < jsonLength; i++) writer.Write((byte)0x20);
            writer.Write((uint)bin.Length); writer.Write(0x004E4942u); writer.Write(bin);
        }

        var asset = GltfAssetDecoder.Decode(stream.ToArray());
        var primitive = Assert.Single(Assert.Single(asset.Meshes).Primitives);
        Assert.Equal(3, primitive.VertexCount);
        Assert.Equal(1, primitive.TriangleCount);
        Assert.Equal(new Vector3(1, 0, 0), primitive.Positions[1]);
        Assert.Equal(new[] { 0, 1, 2 }.Select(i => (uint)i), primitive.Indices);
        Assert.Equal(new[] { 0 }, asset.SceneRoots);
    }
}
