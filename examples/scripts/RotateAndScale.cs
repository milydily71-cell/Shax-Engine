using Shax;
using System.Numerics;

public sealed class PlayerScript : ShaxBehaviour
{
    public override void Update(float deltaTime)
    {
        Transform.Rotate(Vector3.UnitY, 60f * deltaTime);
        Transform.LocalScale = new Vector3(1f, 1f, 1f);
    }
}
