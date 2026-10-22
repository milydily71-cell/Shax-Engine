namespace Shax;

/// <summary>Minimal scene entity with component attachment.</summary>
public class Entity
{
    private readonly List<object> _components = new();
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; }
    public bool Active { get; set; } = true;
    public Transform Transform { get; } = new();
    public Entity(string name = "Entity") => Name = name;

    public T AddComponent<T>(T component) where T : class
    {
        if (component is null) throw new ArgumentNullException(nameof(component));
        if (component is ShaxBehaviour behaviour)
        {
            if (behaviour.Entity is not null)
                throw new InvalidOperationException("Behaviour is already attached to an entity.");
            behaviour.Entity = this;
        }
        _components.Add(component);
        return component;
    }

    public T? GetComponent<T>() where T : class => _components.OfType<T>().FirstOrDefault();
    public IReadOnlyList<object> Components => _components;
}
