using System.Numerics;
using Shax;
using Xunit;

namespace Shax.Core.Tests;

public sealed class SceneTests
{
    [Fact]
    public void ParentTransformContributesToWorldPosition()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        parent.Transform.LocalPosition = new Vector3(2, 0, 0);
        child.Transform.LocalPosition = new Vector3(1, 0, 0);
        child.SetParent(parent);
        Assert.Equal(new Vector3(3, 0, 0), child.Transform.WorldPosition);
        Assert.Throws<InvalidOperationException>(() => parent.SetParent(child));
    }

    [Fact]
    public void SceneJsonRoundTripPreservesIdsAndHierarchy()
    {
        var scene = new Scene("Test");
        var parent = scene.CreateEntity("Parent");
        var child = scene.CreateEntity("Child");
        child.SetParent(parent);
        child.Transform.LocalPosition = new Vector3(1, 2, 3);
        var restored = SceneSerializer.Deserialize(SceneSerializer.Serialize(scene));
        Assert.Equal(scene.Name, restored.Name);
        Assert.Equal(scene.Entities.Select(e => e.Id).OrderBy(x => x), restored.Entities.Select(e => e.Id).OrderBy(x => x));
        var restoredChild = Assert.Single(restored.Entities.Where(e => e.Name == "Child"));
        Assert.Equal("Parent", restoredChild.Parent?.Name);
        Assert.Equal(new Vector3(1, 2, 3), restoredChild.Transform.LocalPosition);
    }

    [Fact]
    public void SceneTickRunsAttachedBehaviourAgainstItsEntityTransform()
    {
        var scene = new Scene();
        var cube = scene.CreateEntity("Cube");
        cube.AddComponent(new SpinBehaviour());
        scene.Tick(0.5f);
        Assert.NotEqual(Quaternion.Identity, cube.Transform.LocalRotation);
    }

    private sealed class SpinBehaviour : ShaxBehaviour
    {
        public override void Update(float deltaTime) => Transform.Rotate(Vector3.UnitY, 90f * deltaTime);
    }
}
