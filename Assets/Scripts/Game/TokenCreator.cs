using System;
using System.Collections;
using System.Collections.Generic;
using Enums;
using UnityEngine;

public class TokenCreator : MonoBehaviour
{
    [SerializeField] private GameObject[] tokenPrefabs;
    [SerializeField] private Material blackMaterial;
    [SerializeField] private Material whiteMaterial;

    private readonly Dictionary<string, GameObject> _namesToTokens = new();

    private void Awake()
    {
        foreach (var tokenPrefab in tokenPrefabs)
        {
            _namesToTokens.Add(tokenPrefab.GetComponent<Token>().GetType().ToString(), tokenPrefab);
        }
    }

    public GameObject CreateToken(Type type)
    {
        var prefab = _namesToTokens[type.ToString()];
        if (!prefab) return null;
        
        var newToken = Instantiate(prefab);
        return newToken;
    }

    public Material GetTeamMaterial(TeamColour colour)
    {
        return colour == TeamColour.White ? whiteMaterial : blackMaterial;
    }
}
