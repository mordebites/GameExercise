using System;
using System.Collections.Generic;
using UnityEngine;

public class ColliderInputReceiver : InputReceiver
{
    [SerializeField] private Camera mainCamera;
    private Vector3 _hoveredPosition;
    private Vector3 _leftClickedPosition;
    private bool _leftClicked;
    private bool _rightClicked;

    private void Update()
    {
        //right click
        if (Input.GetMouseButtonDown(1))
        {
            _rightClicked = true;
            OnInputReceived();
        }
        else
        {
            _rightClicked = false;
        }
        
        //Returns a ray going from camera through a screen point
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
        //Casts a ray against all colliders in the Scene
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;
        
        _hoveredPosition = hit.point;
        if (Input.GetMouseButtonDown(0))
        {
            _leftClicked = true;
            _leftClickedPosition = hit.point;
        }
        else
        {
            _leftClicked = false;
        }

        OnInputReceived();
    }

    public override void OnInputReceived()
    {
        foreach (var inputHandler in InputHandlers)
        {
            if (_rightClicked)
            {
                inputHandler.ProcessInput(null, IInputHandler.InputType.RightClick);
                continue;
            }
            
            inputHandler.ProcessInput(_hoveredPosition, IInputHandler.InputType.Hover);
            
            if (_leftClicked)
            {
                inputHandler.ProcessInput(_leftClickedPosition, IInputHandler.InputType.LeftClick);
            }
        }
    }
}
