using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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

struct SelectedCodexSections
{
    public string category;
    public string topic;
    public string entry;
}

public class UIManagerScript : MonoBehaviour
{
    //codex contents
    private const string CodexPath = "./Assets/Data/codex.json";
    private Codex _codex;

    private readonly Dictionary<Toggle, GameObject> _topicTogglesToEntrySections = new();
    private readonly Dictionary<Toggle, int> _categoryTogglesToCategoryIndices = new();
    private SelectedCodexSections _selectedCodexSections;

    //UI sections and elements references
    private GameObject _gameSection;
    private TextMeshProUGUI _titleSectionText;

    public GameObject codexSection;
    public GameObject topicsSection;
    public GameObject entrySection;
    public GameObject topicsSectionPrefab;
    public GameObject codexEntryButtonPrefab;
    public TextMeshProUGUI winnerTextComponent;

    //UI element interaction events
    public UnityEvent moveButtonPressed;
    public UnityEvent defendButtonPressed;
    public UnityEvent endTurnButtonPressed;

    private void Awake()
    {
        _gameSection = GetGameSectionObject();
        if (!_gameSection)
        {
            Debug.LogError("Could not find codex section title");
            return;
        }

        _titleSectionText = GetTitleSectionText();
        if (!_titleSectionText)
        {
            Debug.LogError("Could not find codex section title");
            return;
        }

        var codex = LoadCodexData();
        if (codex?.categories == null)
        {
            Debug.LogError("Could not find categories in parsed codex");
            return;
        }

        _codex = codex.Value;

        SetupCodexCategoryToggles();
        LoadCategory(0);
    }

    private GameObject GetGameSectionObject()
    {
        var canvas = gameObject.scene.GetRootGameObjects().First(o => o.name == "Canvas");
        return canvas.transform.Find("GameSection").gameObject;
    }

    private TextMeshProUGUI GetTitleSectionText()
    {
        var codexTitleSection = codexSection.transform.Find("TitleSection");
        var textSection = codexTitleSection?.Find("TitleText").gameObject;
        return textSection?.GetComponent<TextMeshProUGUI>();
    }

    private static Codex? LoadCodexData()
    {
        var jsonCodex = ReadFile(CodexPath);
        if (string.IsNullOrEmpty(jsonCodex))
        {
            throw new FormatException("Could not read codex json");
        }

        try
        {
            return JsonUtility.FromJson<Codex>(jsonCodex);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
        }

        return null;
    }

    private void SetupCodexCategoryToggles()
    {
        var navigationSectionTransform = codexSection.transform.Find("NavigationSection");
        if (navigationSectionTransform == null)
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
    }

    private void LoadCategory(int categoryIndex)
    {
        if (categoryIndex >= _codex.categories.Count)
        {
            Debug.LogError("Category index out of range");
            return;
        }

        var codexCategory = _codex.categories[categoryIndex];
        _selectedCodexSections.category = codexCategory.name;
        _titleSectionText.text = codexCategory.name;

        var topicsSectionTransform = topicsSection.transform;
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

        foreach (var topic in codexCategory.topics)
        {
            SetupCodexTopic(topicsSectionTransform, topic);
        }
    }

    private void SetupCodexTopic(Transform topicsSectionTransform, Topic topic)
    {
        var topicSection = Instantiate(topicsSectionPrefab, topicsSectionTransform, true);
        var topicToggleTransform = topicSection.transform.Find("TopicToggle");

        var toggleText = topicToggleTransform.Find("Text");
        var textComponent = toggleText.GetComponent<TextMeshProUGUI>();
        textComponent.text = topic.name;

        var toggleComponent = topicToggleTransform.gameObject.GetComponent<Toggle>();
        var entriesSection = topicSection.transform.Find("EntriesSection");
        if (!_topicTogglesToEntrySections.ContainsKey(toggleComponent))
        {
            _topicTogglesToEntrySections.Add(toggleComponent,
                entriesSection.gameObject);
        }

        toggleComponent.onValueChanged.AddListener(delegate { TopicToggleChanged(toggleComponent); });

        foreach (var entry in topic.entries)
        {
            SetupCodexEntry(entriesSection, entry);
        }
    }

    private void SetupCodexEntry(Transform entriesSection, Entry entry)
    {
        GameObject button =
            Instantiate(codexEntryButtonPrefab, entriesSection.transform, false);

        var entryText = button.transform.Find("Text");
        var textElem = entryText.GetComponent<TextMeshProUGUI>();
        textElem.text = entry.name;

        var component = button.GetComponent<Button>();
        component.onClick.AddListener(delegate { EntryButtonClicked(entry.name); });
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
        winnerTextComponent.text = winner + " wins!";
        winnerTextComponent.gameObject.SetActive(true);
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
        codexSection.SetActive(!codexSection.activeSelf);
    }

    private void TopicToggleChanged(Toggle toggle)
    {
        var found = _topicTogglesToEntrySections.TryGetValue(toggle, out GameObject section);

        if (!found || !section) return;

        var textComponent = toggle.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        _selectedCodexSections.topic = textComponent.text;
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
        var topicsSectionTransform = topicsSection.transform;
        if (!topicsSectionTransform)
        {
            Debug.LogError("Could not find topics section");
            return;
        }

        topicsSectionTransform.gameObject.SetActive(false);
        var entrySectionTransform = entrySection.transform;

        var category = _codex.categories.Find(cat => cat.name == _selectedCodexSections.category);
        var topic = category.topics.Find(topic => topic.name == _selectedCodexSections.topic);
        var entry = topic.entries.Find(entry => entry.name == entryName);

        _titleSectionText.text = entry.name;

        var textComponent = entrySectionTransform.Find("Text").GetComponent<TextMeshProUGUI>();
        textComponent.text = entry.text;

        var imageComponent = entrySectionTransform.Find("Image").GetComponent<Image>();
        var result = LoadImage(entry.image, out Texture2D tex);
        if (result)
        {
            imageComponent.sprite =
                Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        var navSectionTransform = codexSection.transform.Find("NavigationSection");
        navSectionTransform.Find("CategoriesSection").gameObject.SetActive(false);
        var entryNavSectionTransform = navSectionTransform.Find("EntryNavSection");
        entryNavSectionTransform.gameObject.SetActive(true);

        var backNavToggle = entryNavSectionTransform.Find("BackNavToggle").GetComponent<Toggle>();
        backNavToggle.onValueChanged.AddListener(delegate { OnBackNavToggle(); });

        _selectedCodexSections.entry = entry.name;
        entrySectionTransform.gameObject.SetActive(true);
    }

    private static bool LoadImage(string path, out Texture2D texture)
    {
        var loadTexture = new Texture2D(1, 1);
        texture = loadTexture;

        try
        {
            var bytes = File.ReadAllBytes(path);
            loadTexture.LoadImage(bytes);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not load image. " + e);
            return false;
        }

        return true;
    }

    private void OnBackNavToggle()
    {
        var topicsSectionTransform = topicsSection.transform;
        if (!topicsSectionTransform)
        {
            Debug.LogError("Could not find topics section");
            return;
        }

        topicsSectionTransform.gameObject.SetActive(true);

        var entrySectionTransform = entrySection.transform;
        entrySectionTransform.gameObject.SetActive(false);

        //TODO refactor logic to get category
        var categoryToIndex =
            _categoryTogglesToCategoryIndices.First(
                pair => pair.Key.name.Replace("Toggle", "") == _selectedCodexSections.category
            );

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
        var fileContents = "";
        StreamReader stream = null;
        try
        {
            stream = new StreamReader(path);
            fileContents = stream.ReadToEnd();
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
        }
        finally
        {
            stream?.Dispose();
        }

        return fileContents;
    }
}