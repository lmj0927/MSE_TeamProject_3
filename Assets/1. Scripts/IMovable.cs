using UnityEngine;

public interface IMovable
{
    void Move(Vector3 worldDirection, float speed, float deltaTime);
}
