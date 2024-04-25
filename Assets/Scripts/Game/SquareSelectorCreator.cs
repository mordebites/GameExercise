using System;
using System.Collections.Generic;
using UnityEngine;

public class SquareSelectorCreator : MonoBehaviour
{
    private class SelectorList
    {
        public readonly List<SelectorInfo> Selectors;
        public int Index;

        public SelectorList(List<SelectorInfo> selectors, int index)
        {
            Selectors = selectors;
            Index = index;
        }
    }

    private class SelectorInfo
    {
        public GameObject Selector;
        public Token Token;
        public SpriteRenderer Sprite;

        public SelectorInfo(GameObject selector, Token token, SpriteRenderer sprite)
        {
            Selector = selector;
            Token = token;
            Sprite = sprite;
        }
    }
    
    [SerializeField] private Color freeSquareColour;
    [SerializeField] private Color opponentSquareColour;
    [SerializeField] private Color defendedSquareColour;

    
    [SerializeField] private GameObject selectorPrefab;
    private readonly List<SelectorInfo> _moveSelectors = new();
    private readonly Dictionary<Player, SelectorList> _defenceSelectors = new();
    private int _moveSelectorIndex;

    public void ShowDefendedSquare(Vector3 position, Player player, Token token)
    {
        _defenceSelectors.TryGetValue(player, out SelectorList list);
        if (list == null)
        {
            list = new SelectorList(new List<SelectorInfo>(), 0);
            _defenceSelectors[player] = list;
        }

        ShowSquare(list.Selectors, ref list.Index, position, token, defendedSquareColour);
    }

    public void ClearDefendedSquares(Player player)
    {
        _defenceSelectors.TryGetValue(player, out SelectorList list);
        if (list != null)
        {
            ClearSquares(list.Selectors, ref list.Index);
        }
    }

    public void ClearDefendedSquare(Player player, Token token)
    {
        _defenceSelectors.TryGetValue(player, out SelectorList list);

        if (list == null) return;

        var selectorIndex = list.Selectors.FindIndex(info => info.Token == token);
        var info = list.Selectors[selectorIndex];
        list.Selectors.RemoveAt(selectorIndex);

        info.Selector.SetActive(false);
        info.Token = null;

        list.Selectors.Add(info);
        list.Index--;
    }

    public void ShowAvailableMoves(Dictionary<Vector3, bool> squareData, Token token)
    {
        ClearAvailableMoves();
        foreach (var (position, isEmpty) in squareData)
        {
            var colour = isEmpty ? freeSquareColour : opponentSquareColour;
            ShowSquare(_moveSelectors, ref _moveSelectorIndex, position, token, colour);
        }
    }

    public void ClearAvailableMoves()
    {
        ClearSquares(_moveSelectors, ref _moveSelectorIndex);
    }

    private void ShowSquare(
        List<SelectorInfo> selectors,
        ref int index,
        Vector3 position,
        Token token,
        Color colour
    )
    {
        if (index >= selectors.Count)
        {
            var newSelector = Instantiate(selectorPrefab, Vector3.zero, Quaternion.identity);
            var sprite = newSelector.transform.GetChild(0).GetComponent<SpriteRenderer>();
            selectors.Add(new SelectorInfo(newSelector, token, sprite));
        }

        var info = selectors[index];
        info.Token = token;
        info.Selector.transform.position = position;
        info.Selector.SetActive(true);
        info.Sprite.color = colour;

        index++;
    }

    private void ClearSquares(List<SelectorInfo> selectors, ref int index)
    {
        foreach (var info in selectors)
        {
            info.Selector.SetActive(false);
            info.Token = null;
        }

        index = 0;
    }
}