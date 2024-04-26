using UnityEngine;

public abstract class InputReceiver : MonoBehaviour
{
    protected IInputHandler[] InputHandlers;

    public abstract void OnInputReceived();

    private void Awake()
    {
        InputHandlers = GetComponents<IInputHandler>();
    }
}
