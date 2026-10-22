using Shax;
using System.Numerics;

public sealed class PlayerScript : ShaxBehaviour
{
    public override void Update(float deltaTime)
    {
        Transform.LocalScale = new Vector3(1.5f, 1.5f, 1.5f);
    }
}
