namespace Shax;

/// <summary>Stable-ID scene entity with component attachment and hierarchy.</summary>
public class Entity
{
    private readonly List<object> _components = new();
    private readonly List<Entity> _children = new();
    private Entity? _parent;
    public Guid Id { get; }
    public string Name { get; set; }
    public bool Active { get; set; } = true;
    public Transform Transform { get; } = new();
    public Entity? Parent => _parent;
    public IReadOnlyList<Entity> Children => _children;

    public Entity(string name = "Entity") : this(name, Guid.NewGuid()) { }
    public Entity(string name, Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Entity ID cannot be empty.", nameof(id));
        Id = id; Name = name;
    }

    public void SetParent(Entity? parent)
    {
        if (ReferenceEquals(parent, this)) throw new InvalidOperationException("An entity cannot parent itself.");
        for (var cursor = parent; cursor is not null; cursor = cursor.Parent)
            if (ReferenceEquals(cursor, this)) throw new InvalidOperationException("Entity parenting cannot create a cycle.");
        if (ReferenceEquals(_parent, parent)) return;
        _parent?._children.Remove(this);
        _parent = parent;
        Transform.SetParent(parent?.Transform);
        if (parent is not null && !parent._children.Contains(this)) parent._children.Add(this);
    }

    public T AddComponent<T>(T component) where T : class
    {
        if (component is null) throw new ArgumentNullException(nameof(component));
        if (component is ShaxBehaviour behaviour)
        {
            if (behaviour.Entity is not null) throw new InvalidOperationException("Behaviour is already attached to an entity.");
            behaviour.Entity = this;
        }
        _components.Add(component);
        if (component is IEntityComponent attached) attached.OnAttached(this);
        return component;
    }

    public T? GetComponent<T>() where T : class => _components.OfType<T>().FirstOrDefault();
    public IReadOnlyList<object> Components => _components;
}
