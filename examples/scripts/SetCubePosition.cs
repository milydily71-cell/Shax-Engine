using Shax;
using System.Numerics;

public sealed class PlayerScript : ShaxBehaviour
{
    public override void Update(float deltaTime)
    {
        Transform.LocalPosition = new Vector3(0f, 0.5f, 0f);
    }
}
