using System;
using System.Collections.Generic;
using UnityEngine;

public class ColliderInputReceiver : InputReceiver
{
    private Vector3? _clickedPosition;
    private Vector3? _hoveredPosition;
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
        var ray = Camera.main?.ScreenPointToRay(Input.mousePosition);
            
        //Casts a ray against all colliders in the Scene
        if (ray.HasValue && Physics.Raycast(ray.Value, out RaycastHit hit))
        {
            _hoveredPosition = hit.point;
            if (Input.GetMouseButtonDown(0))
            {
                _clickedPosition = hit.point;
            }
            else
            {
                _clickedPosition = null;
            }

            OnInputReceived();
        }
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
            
            if (_clickedPosition.HasValue)
            {
                inputHandler.ProcessInput(_clickedPosition.Value, IInputHandler.InputType.LeftClick);
            }
        }
    }
}
