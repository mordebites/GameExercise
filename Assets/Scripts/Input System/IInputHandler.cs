using System;
using UnityEngine;

public interface IInputHandler
{
    enum InputType
    {
        Hover,
        LeftClick,
        RightClick
    }
    
    void ProcessInput(Vector3? inputPosition,  InputType inputType);
}
