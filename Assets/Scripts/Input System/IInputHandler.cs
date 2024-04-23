using System;
using UnityEngine;

public interface IInputHandler
{
    enum InputType
    {
        Hover,
        Click
    }
    
    void ProcessInput(Vector3 inputPosition,  InputType inputType);
}
