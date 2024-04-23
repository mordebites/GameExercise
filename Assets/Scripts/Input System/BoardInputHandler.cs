using System;
using UnityEngine;

[RequireComponent(typeof(Board))]
public class BoardInputHandler : MonoBehaviour, IInputHandler
{
    private Board _board;

    private void Awake()
    {
        _board = GetComponent<Board>();
    }

    public void ProcessInput(Vector3? inputPosition, IInputHandler.InputType inputType)
    {
        switch (inputType)
        {
            case IInputHandler.InputType.Hover:
                _board.OnSquareHovered(inputPosition);
                break;
            case IInputHandler.InputType.LeftClick:
                _board.OnSquareSelected(inputPosition);
                break;
            case IInputHandler.InputType.RightClick:
                _board.OnCancelMove();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(inputType), inputType, null);
        }
    }
}