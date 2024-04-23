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

    public void ProcessInput(Vector3 inputPosition, IInputHandler.InputType inputType)
    {
        if (inputType == IInputHandler.InputType.Click)
        {
            _board.OnSquareSelected(inputPosition);
        }
        else
        {
            //_board.OnSquareHovered(inputPosition);
        }
    }
}