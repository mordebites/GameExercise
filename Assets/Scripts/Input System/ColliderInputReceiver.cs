using System;
using System.Collections.Generic;
using UnityEngine;

public class ColliderInputReceiver : InputReceiver
{
    private Vector3 _clickedPosition;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        
        //Returns a ray going from camera through a screen point
        var ray = Camera.main?.ScreenPointToRay(Input.mousePosition);
            
        //Casts a ray against all colliders in the Scene
        if (ray.HasValue && Physics.Raycast(ray.Value, out RaycastHit hit))
        {
            _clickedPosition = hit.point;
            OnInputReceived();
        }
    }

    public override void OnInputReceived()
    {
        foreach (var inputHandler in inputHandlers)
        {
            inputHandler.ProcessInput(_clickedPosition, null, null);
        }
    }
}
