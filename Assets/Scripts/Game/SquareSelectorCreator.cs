using System;
using System.Collections.Generic;
using UnityEngine;

public class SquareSelectorCreator : MonoBehaviour
{
    [SerializeField] private Material freeSquareMaterial;
    [SerializeField] private Material opponentSquareMaterial;
    [SerializeField] private GameObject selectorPrefab;
    private readonly List<GameObject> _instantiatedSelectors = new();
    private int _currentIndex;
    
    private void Awake()
    {
        _currentIndex = 0;
        for (int i = 0; i < 14; i++)
        {
            var selector = Instantiate(selectorPrefab, Vector3.zero, Quaternion.identity);
            selector.SetActive(false);
            _instantiatedSelectors.Add(selector);   
        }
    }

    public void ShowSelection(Dictionary<Vector3, bool> squareData)
    {
        ClearSelection();
        foreach (var (position, isEmpty) in squareData)
        {
            var selector = _instantiatedSelectors[_currentIndex];
            selector.transform.position = position;
            selector.SetActive(true);
            foreach (var setter in selector.GetComponentsInChildren<MaterialSetter>())
            {
                setter.SetSingleMaterial(isEmpty ? freeSquareMaterial : opponentSquareMaterial);
            }

            _currentIndex++;
        }
    }

    public void ClearSelection()
    {
        foreach (var selector in _instantiatedSelectors)
        {
            selector.SetActive(false);
        }

        _currentIndex = 0;
    }
}
