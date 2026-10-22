using Shax;
using System.Numerics;

public sealed class PlayerScript : ShaxBehaviour
{
    public override void Update(float deltaTime)
    {
        Transform.Rotate(Vector3.UnitY, 45f * deltaTime);
    }
}
