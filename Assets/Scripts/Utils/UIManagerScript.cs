using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using GameObject = UnityEngine.GameObject;

[Serializable]
struct Codex
{
    public List<Category> categories;
}

[Serializable]
struct Category
{
    public string name;
    public List<Topic> topics;

    public Category(string name, List<Topic> topics)
    {
        this.name = name;
        this.topics = topics;
    }
}

[Serializable]
struct Topic
{
    public string name;
    public List<Entry> entries;

    public Topic(string name, List<Entry> entries)
    {
        this.name = name;
        this.entries = entries;
    }
}

[Serializable]
struct Entry
{
    public string name;

    //TODO
    public string image;
    public string text;

    public Entry(string name, string text)
    {
        this.name = name;
        this.text = text;
        image = null;
    }
}

struct CurrentSections
{
    public string category;
    public string topic;
    public string entry;
}

public class UIManagerScript : MonoBehaviour
{
    private const string CodexPath = "./Assets/Data/codex.json";

    private TextMeshProUGUI _titleSectionText;
    private readonly Dictionary<Toggle, GameObject> _topicTogglesToEntrySections = new();
    private readonly Dictionary<Toggle, int> _categoryTogglesToCategoryIndices = new();
    private Codex _codex;
    private CurrentSections _currentSections;
    private GameObject _gameSection;

    public GameObject codexSection;
    public GameObject topicsSectionPrefab;

    public UnityEvent moveButtonPressed;
    public UnityEvent defendButtonPressed;
    public UnityEvent endTurnButtonPressed;

    private void Awake()
    {
        _gameSection = gameObject.scene.GetRootGameObjects()
            .First(o => o.name == "Canvas")
            .transform.Find("GameSection").gameObject;
        var codexTitleSection = codexSection.transform.Find("TitleSection");
        var textSection = codexTitleSection?.Find("TitleText").gameObject;
        _titleSectionText = textSection?.GetComponent<TextMeshProUGUI>();

        if (!_titleSectionText)
        {
            Debug.LogError("Could not find codex section title");
            return;
        }

        //TODO handle parsing errors
        var jsonCodex = ReadFile(CodexPath);
        _codex = JsonUtility.FromJson<Codex>(jsonCodex);

        if (_codex.categories == null)
        {
            Debug.LogError("Could not find categories in parsed codex");
            return;
        }

        var navigationSectionTransform = codexSection.transform.Find("NavigationSection");
        if (!navigationSectionTransform)
        {
            Debug.LogError("Could not find category section");
            return;
        }

        for (var i = 0; i < _codex.categories.Count; i++)
        {
            var category = _codex.categories[i];
            var toggleTransform = navigationSectionTransform.Find("CategoriesSection").Find(category.name + "Toggle");
            if (!toggleTransform)
            {
                Debug.LogError($"Could not find toggle for category {category.name}");
                return;
            }

            var toggle = toggleTransform.GetComponent<Toggle>();
            if (!toggle)
            {
                Debug.LogError($"Could not find toggle component for category {category.name}");
                return;
            }

            _categoryTogglesToCategoryIndices.Add(toggle, i);

            toggle.onValueChanged.AddListener(delegate { CategoryToggleChanged(toggle); });
        }

        LoadCategory(0);
    }

    private void LoadCategory(int categoryIndex)
    {
        //TODO check index
        var codexCategory = _codex.categories[categoryIndex];
        _currentSections.category = codexCategory.name;
        _titleSectionText.text = codexCategory.name;

        var topicsSectionTransform = codexSection.transform.Find("MainSection").Find("TopicsSection");
        if (!topicsSectionTransform)
        {
            Debug.LogError("Could not find topics section");
            return;
        }

        //empty topics section
        var count = topicsSectionTransform.childCount;
        for (var i = 0; i < count; i++)
        {
            var childTransform = topicsSectionTransform.GetChild(i);
            //TODO reuse UI elements
            // childTransform.gameObject.SetActive(false);

            Destroy(childTransform.gameObject);
        }

        //populate topics section based on codex
        foreach (var topic in codexCategory.topics)
        {
            var newSection = Instantiate(topicsSectionPrefab, topicsSectionTransform, true);
            var topicToggle = newSection.transform.Find("TopicToggle");
            var toggleText = topicToggle.Find("Text");
            var text = toggleText.GetComponent<TextMeshProUGUI>();

            text.text = topic.name;

            var entriesSection = newSection.transform.Find("EntriesSection");
            var entryButton = entriesSection.Find("EntryButton");

            var toggle = topicToggle.gameObject.GetComponent<Toggle>();
            if (!_topicTogglesToEntrySections.ContainsKey(toggle))
            {
                _topicTogglesToEntrySections.Add(toggle, entriesSection.gameObject);
            }

            toggle.onValueChanged.AddListener(delegate { TopicToggleChanged(toggle); });

            var first = true;

            foreach (var entry in topic.entries)
            {
                GameObject button = entryButton.gameObject;
                if (first)
                {
                    first = false;
                }
                else
                {
                    button = Instantiate(button, entriesSection, false);
                }

                var entryText = button.transform.Find("Text");
                var textElem = entryText.GetComponent<TextMeshProUGUI>();
                textElem.text = entry.name;

                var component = button.GetComponent<Button>();
                component.onClick.AddListener(delegate { EntryButtonClicked(entry.name); });
            }
        }
    }

    public void OnMoveButtonPressed()
    {
        HideActionButtons();
        moveButtonPressed.Invoke();
    }

    public void OnDefendButtonPressed()
    {
        HideActionButtons();
        defendButtonPressed.Invoke();
    }

    public void OnEndTurnButtonPressed()
    {
        endTurnButtonPressed.Invoke();
    }

    public void ShowWinnerText(string winner)
    {
        var textTransform = _gameSection.transform.Find("WinnerText");
        var textComponent = textTransform.GetComponent<TextMeshProUGUI>();
        
        //TODO improve
        textComponent.text = winner + " wins!";
        textTransform.gameObject.SetActive(true);
    }

    public void HideWinnerText()
    {
        var textTransform = _gameSection.transform.Find("WinnerText");
        textTransform.gameObject.SetActive(false);
    }

    public void HideTokenInfoPanel()
    {
        var tokenInfoPanelTransform = _gameSection.transform.Find("TokenInfoPanel");
        tokenInfoPanelTransform.gameObject.SetActive(false);
    }

    public void ShowTokenInfoPanel(Vector3 position, int health, int attack, int defence)
    {
        var tokenInfoPanelTransform = _gameSection.transform.Find("TokenInfoPanel");

        //TODO improve
        var healthText = tokenInfoPanelTransform.Find("HealthText").GetComponent<TextMeshProUGUI>();
        healthText.text = "Health: " + health;
        
        var attackText = tokenInfoPanelTransform.Find("AttackText").GetComponent<TextMeshProUGUI>();
        attackText.text = "Attack: " + attack;
        
        var defenceText = tokenInfoPanelTransform.Find("DefenceText").GetComponent<TextMeshProUGUI>();
        defenceText.text = "Defence: " + defence;
        
        var pos = Camera.main.WorldToScreenPoint(position);
        tokenInfoPanelTransform.position = pos;
        tokenInfoPanelTransform.gameObject.SetActive(true);
    }

    public void UpdateActionPoints(int newPoints)
    {
        var textComponent = _gameSection.transform.Find("ActionPointsText").GetComponent<TextMeshProUGUI>();
        //TODO improve
        textComponent.text = "Action points: " + newPoints;
    }

    public void UpdateTurnCounter(int newCounter)
    {
        var textComponent = _gameSection.transform.Find("TurnText").GetComponent<TextMeshProUGUI>();
        //TODO improve
        textComponent.text = "Turn: " + newCounter;
    }

    public void UpdateCurrentPlayer(string currentPlayer)
    {
        var textComponent = _gameSection.transform.Find("PlayerText").GetComponent<TextMeshProUGUI>();
        //TODO improve
        textComponent.text = "Player: " + currentPlayer;
    }

    public void ShowActionButtonsAtPosition(Vector3 position)
    {
        var buttonsTransform = _gameSection.transform.Find("ActionButtons");
        var pos = Camera.main.WorldToScreenPoint(position);
        buttonsTransform.position = pos;
        buttonsTransform.gameObject.SetActive(true);
    }

    public void HideActionButtons()
    {
        var buttonsTransform = _gameSection.transform.Find("ActionButtons");
        buttonsTransform.gameObject.SetActive(false);
    }

    public void CodexToggleChanged(Toggle change)
    {
        Debug.Log("toggle:" + change.isOn);
        codexSection.SetActive(!codexSection.activeSelf);
    }

    private void TopicToggleChanged(Toggle toggle)
    {
        var found = _topicTogglesToEntrySections.TryGetValue(toggle, out GameObject section);

        if (!found || !section) return;

        var textComponent = toggle.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        _currentSections.topic = textComponent.text;
        section.SetActive(!section.activeSelf);
    }

    private void CategoryToggleChanged(Toggle toggle)
    {
        var found = _categoryTogglesToCategoryIndices.TryGetValue(toggle, out int index);
        if (found)
        {
            LoadCategory(index);
        }
    }

    private void EntryButtonClicked(string entryName)
    {
        //TODO error handling
        var mainSectionTransform = codexSection.transform.Find("MainSection");
        var topicsSectionTransform = mainSectionTransform.Find("TopicsSection");
        if (!topicsSectionTransform)
        {
            Debug.LogError("Could not find topics section");
            return;
        }

        topicsSectionTransform.gameObject.SetActive(false);

        var entrySectionTransform = mainSectionTransform.Find("EntrySection");

        var category = _codex.categories.Find(cat => cat.name == _currentSections.category);
        var topic = category.topics.Find(topic => topic.name == _currentSections.topic);
        var entry = topic.entries.Find(entry => entry.name == entryName);

        _titleSectionText.text = entry.name;

        var textComponent = entrySectionTransform.Find("Text").GetComponent<TextMeshProUGUI>();
        textComponent.text = entry.text;
        //TODO entry image

        var navSectionTransform = codexSection.transform.Find("NavigationSection");
        navSectionTransform.Find("CategoriesSection").gameObject.SetActive(false);
        var entryNavSectionTransform = navSectionTransform.Find("EntryNavSection");
        entryNavSectionTransform.gameObject.SetActive(true);

        var backNavToggle = entryNavSectionTransform.Find("BackNavToggle").GetComponent<Toggle>();
        backNavToggle.onValueChanged.AddListener(delegate { OnBackNavToggle(); });

        _currentSections.entry = entry.name;
        entrySectionTransform.gameObject.SetActive(true);
    }

    private void OnBackNavToggle()
    {
        var mainSectionTransform = codexSection.transform.Find("MainSection");
        var topicsSectionTransform = mainSectionTransform.Find("TopicsSection");
        if (!topicsSectionTransform)
        {
            Debug.LogError("Could not find topics section");
            return;
        }

        topicsSectionTransform.gameObject.SetActive(true);

        var entrySectionTransform = mainSectionTransform.Find("EntrySection");
        entrySectionTransform.gameObject.SetActive(false);

        //TODO refactor logic to get category
        var categoryToIndex = _categoryTogglesToCategoryIndices.First(pair =>
        {
            return pair.Key.name.Replace("Toggle", "") == _currentSections.category;
        });

        LoadCategory(categoryToIndex.Value);

        //TODO not working
        TopicToggleChanged(categoryToIndex.Key);

        var navSectionTransform = codexSection.transform.Find("NavigationSection");
        navSectionTransform.Find("CategoriesSection").gameObject.SetActive(true);
        var entryNavSectionTransform = navSectionTransform.Find("EntryNavSection");
        entryNavSectionTransform.gameObject.SetActive(false);
    }

    private static string ReadFile(string path)
    {
        using StreamReader stream = new StreamReader(path);
        var fileContents = stream.ReadToEnd();
        stream.Close();
        return fileContents;
    }
}