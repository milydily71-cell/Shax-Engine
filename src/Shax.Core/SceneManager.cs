namespace Shax;

/// <summary>Owns named scenes and provides explicit active-scene switching for editor/runtime workflows.</summary>
public class SceneManager
{
    private readonly Dictionary<string, Scene> _scenes = new(StringComparer.OrdinalIgnoreCase);
    public Scene? ActiveScene { get; private set; }
    public IReadOnlyCollection<Scene> Scenes => _scenes.Values;

    public Scene Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Scene name is required.", nameof(name));
        if (_scenes.ContainsKey(name)) throw new InvalidOperationException($"Scene '{name}' already exists.");
        var scene = Engine.CreateScene(name);
        _scenes.Add(name, scene);
        ActiveScene ??= scene;
        return scene;
    }

    public void Register(Scene scene, bool makeActive = false)
    {
        if (scene is null) throw new ArgumentNullException(nameof(scene));
        if (_scenes.TryGetValue(scene.Name, out var existing) && !ReferenceEquals(existing, scene))
            throw new InvalidOperationException($"A different scene named '{scene.Name}' is already registered.");
        _scenes[scene.Name] = scene;
        if (makeActive || ActiveScene is null) ActiveScene = scene;
    }

    public bool TryGet(string name, out Scene? scene) => _scenes.TryGetValue(name, out scene);

    public Scene SetActive(string name)
    {
        if (!_scenes.TryGetValue(name, out var scene)) throw new KeyNotFoundException($"Scene '{name}' is not registered.");
        ActiveScene = scene;
        return scene;
    }

    public bool Unload(string name)
    {
        if (!_scenes.TryGetValue(name, out var scene)) return false;
        _scenes.Remove(name);
        if (ReferenceEquals(ActiveScene, scene)) ActiveScene = _scenes.Values.FirstOrDefault();
        return true;
    }
}
