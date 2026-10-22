namespace Shax;

/// <summary>A lightweight scene container with deterministic behaviour lifecycle callbacks.</summary>
public class Scene
{
    private readonly List<Entity> _entities = new();
    private readonly HashSet<ShaxBehaviour> _started = new();
    public PhysicsWorld Physics { get; } = new();
    public string Name { get; set; }
    public IReadOnlyList<Entity> Entities => _entities;
    public Scene(string name = "Scene") => Name = name;

    public virtual Entity CreateEntity(string name = "Entity")
    {
        var entity = new Entity(name);
        RegisterEntity(entity);
        return entity;
    }

    public void AddEntity(Entity entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (_entities.Any(e => e.Id == entity.Id && !ReferenceEquals(e, entity)))
            throw new InvalidOperationException($"Duplicate entity ID {entity.Id}.");
        RegisterEntity(entity);
    }

    internal void RegisterEntity(Entity entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (!_entities.Contains(entity)) _entities.Add(entity);
    }

    public Entity? FindEntity(Guid id) => _entities.FirstOrDefault(e => e.Id == id);

    public bool RemoveEntity(Entity entity)
    {
        if (!_entities.Remove(entity)) return false;
        entity.SetParent(null);
        foreach (var child in entity.Children.ToArray()) child.SetParent(null);
        foreach (var behaviour in entity.Components.OfType<ShaxBehaviour>())
        {
            if (entity.Active) behaviour.OnDisable();
            behaviour.OnDestroy();
            _started.Remove(behaviour);
        }
        foreach (var renderer in entity.Components.OfType<IDisposable>()) renderer.Dispose();
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        Physics.Bind(this);
        foreach (var entity in _entities.ToArray())
        {
            if (!entity.Active) continue;
            foreach (var behaviour in entity.Components.OfType<ShaxBehaviour>().ToArray())
            {
                if (behaviour.Entity != entity) continue;
                if (_started.Add(behaviour))
                {
                    behaviour.Awake(); behaviour.OnEnable(); behaviour.Start();
                }
                Physics.Bind(this);
                behaviour.Update(deltaTime); behaviour.LateUpdate(deltaTime);
            }
        }
        Physics.Step(this, deltaTime);
    }
}
