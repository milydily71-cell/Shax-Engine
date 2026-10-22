# Shax.Core

Engine-neutral C# API targeting .NET Standard 2.1 and .NET 10.

The core provides Entity, stable GUID IDs, component attachment, parent/child hierarchy, local/world transforms and matrices, ShaxBehaviour lifecycle hooks, Scene entity management and ticking, primitive cube data, Camera, Light, Material, MeshRenderer resource-ownership contracts, ScriptDocument/ScriptDiagnostic, and versioned JSON scene serialization.

Example:

    using Shax;
    using System.Numerics;

    var scene = Engine.CreateScene("Main");
    var parent = scene.CreateEntity("Root");
    var cube = scene.CreateEntity("Cube");
    cube.SetParent(parent);
    cube.Transform.LocalPosition = new Vector3(0, 1, 0);
    cube.AddComponent(new MyBehaviour());
    scene.Tick(1f / 60f);

Both `using Shax;` and the compatibility entry points under `using ShaxEngine;` are available.

The .NET 10 Android app references this library directly and passes the cube world matrix to native `libshax.so`. `CSharpSubsetRuntime` interprets a strict allow-list of behaviour statements (rotation and local position/scale assignments) without IL generation, JIT, assembly loading, reflection, filesystem access or network access. Unsupported statements are rejected with line/column diagnostics. This is not a general C# compiler or full-language runtime; expanding the subset requires new tests and explicit permission boundaries.
