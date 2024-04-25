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
    [SerializeField] private Material selectedMaterial;

    private readonly Dictionary<string, GameObject> _namesToTokens = new();

    private void Awake()
    {
        foreach (var tokenPrefab in tokenPrefabs)
        {
            _namesToTokens.Add(tokenPrefab.name, tokenPrefab);
        }
    }

    public GameObject CreateToken(TokenType type)
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
    
    public Material GetSelectedTokenMaterial()
    {
        return selectedMaterial;
    }
}
