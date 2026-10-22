namespace Shax;

/// <summary>A lightweight scene container with deterministic behaviour lifecycle callbacks.</summary>
public class Scene
{
    private readonly List<Entity> _entities = new();
    private readonly HashSet<ShaxBehaviour> _started = new();
    public string Name { get; set; }
    public IReadOnlyList<Entity> Entities => _entities;
    public Scene(string name = "Scene") => Name = name;

    public virtual Entity CreateEntity(string name = "Entity")
    {
        var entity = new Entity(name);
        RegisterEntity(entity);
        return entity;
    }

    internal void RegisterEntity(Entity entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (!_entities.Contains(entity)) _entities.Add(entity);
    }

    public bool RemoveEntity(Entity entity)
    {
        if (!_entities.Remove(entity)) return false;
        foreach (var behaviour in entity.Components.OfType<ShaxBehaviour>())
        {
            if (entity.Active) behaviour.OnDisable();
            behaviour.OnDestroy();
            _started.Remove(behaviour);
        }
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        foreach (var entity in _entities)
        {
            if (!entity.Active) continue;
            foreach (var behaviour in entity.Components.OfType<ShaxBehaviour>())
            {
                if (behaviour.Entity != entity) continue;
                if (_started.Add(behaviour))
                {
                    behaviour.Awake();
                    behaviour.OnEnable();
                    behaviour.Start();
                }
                behaviour.Update(deltaTime);
                behaviour.LateUpdate(deltaTime);
            }
        }
    }
}
