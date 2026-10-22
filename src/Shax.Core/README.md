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

The .NET 10 Android app references this library directly and passes the cube world matrix to native `libshax.so`. The editor's current script buffer does not dynamically compile arbitrary user source; the visible cube behaviour is compiled into the application. An AOT-compatible, permission-limited script execution strategy remains a separate milestone.
