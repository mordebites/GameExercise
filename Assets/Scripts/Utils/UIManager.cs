using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

public class UIManager : MonoBehaviour
{
    //codex contents
    private const string CodexPath = "./Assets/Data/codex.json";
    private Codex _codex;

    private readonly Dictionary<Toggle, GameObject> _topicTogglesToEntrySections = new();
    private readonly Dictionary<Toggle, int> _categoryTogglesToCategoryIndices = new();
    private SelectedCodexSections _selectedCodexSections;

    //UI sections and elements references
    [SerializeField] private GameObject gameSection;
    [SerializeField] private GameObject codexSection;
    [SerializeField] private GameObject topicsSection;
    [SerializeField] private GameObject entrySection;
    [SerializeField] private GameObject categoriesSection;
    [SerializeField] private GameObject entryNavSection;
    [SerializeField] private GameObject tokenInfoPanel;
    [SerializeField] private GameObject actionButtons;

    [SerializeField] private TextMeshProUGUI titleSectionText;
    [SerializeField] private TextMeshProUGUI winnerTextComponent;
    [SerializeField] private TextMeshProUGUI tokenHealthText;
    [SerializeField] private TextMeshProUGUI tokenAttackText;
    [SerializeField] private TextMeshProUGUI tokenDefenceText;
    [SerializeField] private TextMeshProUGUI actionPointsText;
    [SerializeField] private TextMeshProUGUI currentTurnText;
    [SerializeField] private TextMeshProUGUI currentPlayerText;
    [SerializeField] private TextMeshProUGUI entryText;

    [SerializeField] private Image entryImage;
    
    [SerializeField] private Toggle backNavToggle;

    [SerializeField] private GameObject topicsScrollView;
    [SerializeField] private GameObject entryScrollView;

    [SerializeField] private Canvas codexCanvas;
    
    //camera
    [SerializeField] private Camera cameraMain;
    
    //UI prefabs
    [SerializeField] private GameObject topicSectionPrefab;
    [SerializeField] private GameObject codexEntryButtonPrefab;

    //UI element interaction events
    public UnityEvent moveButtonPressed;
    public UnityEvent defendButtonPressed;
    public UnityEvent endTurnButtonPressed;

    private void Awake()
    {
        var codex = LoadCodexData();
        if (codex?.categories == null)
        {
            Debug.LogError("Could not find categories in parsed codex");
            return;
        }

        _codex = codex.Value;

        SetupCodexCategoryToggles();
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
        //TODO create toggles based on codex
        for (var i = 0; i < _codex.categories.Count; i++)
        {
            var category = _codex.categories[i];
            var toggleTransform = categoriesSection.transform.Find(category.name + "Toggle");
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

        //get category from codex
        var codexCategory = _codex.categories[categoryIndex];
        _selectedCodexSections.category = codexCategory.name;
        
        //set category name as title
        titleSectionText.text = codexCategory.name;

        //empty topics section
        var topicsSectionTransform = topicsSection.transform;
        var count = topicsSectionTransform.childCount;
        for (var i = 0; i < count; i++)
        {
            var topic = topicsSectionTransform.GetChild(i);
            Destroy(topic.gameObject);
        }

        var index = 0;
        foreach (var topic in codexCategory.topics)
        {
            SetupCodexTopic(topic, index);
            index++;
        }
    }

    private void SetupCodexTopic(Topic topic, int index)
    {
        var topicSection = 
            Instantiate(topicSectionPrefab, topicsSection.transform, true);
        
        var topicToggleTransform = topicSection.transform.Find("TopicToggle");

        var textComponent = topicToggleTransform.Find("Text").GetComponent<TextMeshProUGUI>();
        textComponent.text = topic.name;

        var toggleComponent = topicToggleTransform.GetComponent<Toggle>();
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

        var textComponent = button.transform.Find("Text");
        var textElem = textComponent.GetComponent<TextMeshProUGUI>();
        textElem.text = entry.name;

        var buttonComponent = button.GetComponent<Button>();
        buttonComponent.onClick.AddListener(delegate { EntryButtonClicked(entry.name); });
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
        winnerTextComponent.gameObject.SetActive(false);
    }

    public void HideTokenInfoPanel()
    {
        tokenInfoPanel.SetActive(false);
    }

    public void ShowTokenInfoPanel(Vector3 position, int health, int attack, int defence)
    {
        //TODO refactor
        tokenHealthText.text = "Health: " + health;
        tokenAttackText.text = "Attack: " + attack;
        tokenDefenceText.text = "Defence: " + defence;

        var screenPosition = cameraMain.WorldToScreenPoint(position);
        tokenInfoPanel.transform.position = screenPosition;
        tokenInfoPanel.SetActive(true);
    }

    public void UpdateActionPoints(int newPoints)
    {
        //TODO refactor
        actionPointsText.text = "Action points: " + newPoints;
    }

    public void UpdateTurnCounter(int newCounter)
    {
        //TODO improve
        currentTurnText.text = "Turn: " + newCounter;
    }

    public void UpdateCurrentPlayer(string currentPlayer)
    {
        //TODO improve
        currentPlayerText.text = "Player: " + currentPlayer;
    }

    public void ShowActionButtonsAtPosition(Vector3 position)
    {
        var screenPosition = cameraMain.WorldToScreenPoint(position);
        actionButtons.transform.position = screenPosition;
        actionButtons.SetActive(true);
    }

    public void HideActionButtons()
    {
        actionButtons.SetActive(false);
    }

    public void CodexToggleChanged(Toggle change)
    {
        codexCanvas.gameObject.SetActive(change.isOn);

        if (change.isOn)
        {
            LoadCategory(0);
        }
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
        topicsScrollView.SetActive(false);

        var category = _codex.categories.Find(cat => cat.name == _selectedCodexSections.category);
        var topic = category.topics.Find(topic => topic.name == _selectedCodexSections.topic);
        var entry = topic.entries.Find(entry => entry.name == entryName);

        titleSectionText.text = entry.name;
        entryText.text = entry.text;
        
        var result = LoadImage(entry.image, out Texture2D tex);
        if (result)
        {
            entryImage.sprite =
                Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        
        categoriesSection.SetActive(false);
        entryNavSection.SetActive(true);
        
        backNavToggle.onValueChanged.AddListener(delegate { OnBackNavToggle(); });

        _selectedCodexSections.entry = entry.name;
        entryScrollView.SetActive(true);
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
        topicsScrollView.SetActive(true);
        entryScrollView.SetActive(false);

        //TODO refactor logic to get category
        var categoryToIndex =
            _categoryTogglesToCategoryIndices.First(
                pair => pair.Key.name.Replace("Toggle", "") == _selectedCodexSections.category
            );

        LoadCategory(categoryToIndex.Value);

        //TODO not working
        TopicToggleChanged(categoryToIndex.Key);

        categoriesSection.SetActive(true);
        entryNavSection.SetActive(false);
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