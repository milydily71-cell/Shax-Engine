namespace Shax;

/// <summary>Base class for user-authored gameplay scripts.</summary>
public abstract class ShaxBehaviour
{
    public Entity? Entity { get; internal set; }
    public Transform Transform => Entity?.Transform
        ?? throw new InvalidOperationException("Behaviour is not attached to an entity.");

    public virtual void Awake() { }
    public virtual void OnEnable() { }
    public virtual void Start() { }
    public virtual void Update(float deltaTime) { }
    public virtual void LateUpdate(float deltaTime) { }
    public virtual void OnDisable() { }
    public virtual void OnDestroy() { }
}
