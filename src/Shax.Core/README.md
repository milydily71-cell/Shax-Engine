# Shax.Core

Engine-neutral C# API targeting .NET Standard 2.1 and .NET 10.

Available contracts include Entity, Transform, ShaxBehaviour lifecycle hooks, Scene entity management and frame ticking, PrimitiveGeometry.CreateCube(), ScriptDocument, structured ScriptDiagnostic, and entry points in both Shax and ShaxEngine namespaces.

Example using the Shax namespace:

    using Shax;
    var scene = Engine.CreateScene("Main");
    var cube = scene.CreateEntity("Cube");
    cube.Transform.LocalPosition = new System.Numerics.Vector3(0, 1, 0);
    scene.Tick(1f / 60f);

The ShaxEngine namespace provides matching Engine, Scene, Entity, ShaxBehaviour, Transform, and Primitives entry points for projects that prefer that spelling.

The .NET 10 Android host references this library directly. Dynamic compilation, project sandboxing, persistence, and a full editor-grade compiler pipeline remain separate milestones; the document model is not itself a compiler.
