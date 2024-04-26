using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
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
    public int CategoryIndex;
    public string Topic;
}

public class UIManager : MonoBehaviour
{
    //codex contents
    private const string CodexPath = "./Assets/Data/codex.json";
    private Codex _codex;

    private readonly Dictionary<Toggle, GameObject> _topicTogglesToEntrySections = new();
    private readonly Dictionary<Toggle, int> _categoryTogglesToIndices = new();
    private SelectedCodexSections _selectedCodexSections;

    //UI sections and elements references
    [SerializeField] private GameObject gameSection;
    [SerializeField] private GameObject codexSection;
    [SerializeField] private GameObject topicContainerSection;
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

    [SerializeField] private Button endTurnButton;
    
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
    [SerializeField] private GameObject codexTogglePrefab;

    //pools
    private ObjectPool<GameObject> _topicSectionPool;
    private ObjectPool<GameObject> _entrySectionPool;
    
    //UI element interaction events
    public UnityEvent moveButtonPressed;
    public UnityEvent defendButtonPressed;
    public UnityEvent endTurnButtonPressed;
    public UnityEvent<bool> codexToggleChanged;

    private void Awake()
    {
        _selectedCodexSections.CategoryIndex = -1;

        InitializeTopicSectionPool();
        InitializeEntrySectionPool();
        
        var codex = LoadCodexData();
        if (codex == null)
        {
            Debug.LogError("Null codex");
            ApplicationManager.QuitGame();
        }

        _codex = codex.Value;

        SetupCodexCategoryToggles();
    }

    private void InitializeEntrySectionPool()
    {
        _entrySectionPool = new ObjectPool<GameObject>(
            () => Instantiate(codexEntryButtonPrefab),
            button => button.SetActive(true),
            button => button.SetActive(false),
            Destroy,
            true,
            50,
            100
        );
    }

    private void InitializeTopicSectionPool()
    {
        _topicSectionPool = new ObjectPool<GameObject>(
            () => Instantiate(topicSectionPrefab, topicContainerSection.transform, true),
            section => section.SetActive(true),
            ReleaseTopicSectionFromPool,
            Destroy,
            true,
            20,
            50
        );
    }

    private void ReleaseTopicSectionFromPool(GameObject section)
    {
        section.SetActive(false);
        var entriesSection = section.transform.Find("EntriesSection");

        var count = entriesSection.childCount;
        for (var i = 0; i < count; i++)
        {
            var entry = entriesSection.GetChild(i);
            if (entry.gameObject.activeSelf)
            {
                _entrySectionPool.Release(entry.gameObject);
            }
        }
    }

    private static Codex? LoadCodexData()
    {
        var jsonCodex = ReadFile(CodexPath);
        if (string.IsNullOrEmpty(jsonCodex))
        {
            Debug.LogError($"Could not read codex json at path: {CodexPath}");
            ApplicationManager.QuitGame();
        }

        try
        {
            return JsonUtility.FromJson<Codex>(jsonCodex);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not parse codex json at path: {CodexPath} - got exception: {exception}");
            ApplicationManager.QuitGame();
        }

        return null;
    }

    private void SetupCodexCategoryToggles()
    {
        if (_codex.categories == null || !_codex.categories.Any())
        {
            Debug.LogError($"No categories found in codex");
            ApplicationManager.QuitGame();
        }
        
        for (var i = 0; i < _codex.categories?.Count; i++)
        {
            var category = _codex.categories[i];
            
            if (!CheckValidCategory(category)) continue;
            
            //create category toggle
            var toggle = Instantiate(codexTogglePrefab, categoriesSection.transform, false);
            toggle.SetActive(true);
            toggle.name = category.name + "Toggle";
            
            //set category name
            var textComponent = toggle.transform.Find("Background").Find("Text").GetComponent<TextMeshProUGUI>();
            textComponent.text = category.name;
            
            var toggleComponent = toggle.GetComponent<Toggle>();
            _categoryTogglesToIndices.Add(toggleComponent, i);
            
            //listen to category toggle changes
            toggleComponent.onValueChanged.AddListener(delegate { CategoryToggleChanged(toggleComponent); });
        }
    }

    private static bool CheckValidCategory(Category category)
    {
        if (string.IsNullOrEmpty(category.name) || category.topics == null || !category.topics.Any())
        {
            Debug.LogError($"Category {category.name} is malformed");
            return false;
        }

        return true;
    }

    private void LoadCategory(int categoryIndex)
    {
        if (categoryIndex >= _codex.categories.Count)
        {
            Debug.LogError("Category index out of range");
            return;
        }

        UpdateCategoryToggleInteractableState(_selectedCodexSections.CategoryIndex, true);
        UpdateCategoryToggleInteractableState(categoryIndex, false);

        //get category from codex
        var codexCategory = _codex.categories[categoryIndex];
        _selectedCodexSections.CategoryIndex = categoryIndex;
        
        //set category name as title
        titleSectionText.text = codexCategory.name;

        //empty topics section
        var topicContainer = topicContainerSection.transform;
        var count = topicContainer.childCount;
        for (var i = 0; i < count; i++)
        {
            var topic = topicContainer.GetChild(i);
            if (topic.gameObject.activeSelf)
            {
                _topicSectionPool.Release(topic.gameObject);
            }
        }

        foreach (var topic in codexCategory.topics)
        {
            SetupCodexTopic(topic, codexCategory);
        }
    }

    private void UpdateCategoryToggleInteractableState(int categoryIndex, bool interactable)
    {
        var (foundToggle, _) = _categoryTogglesToIndices
            .FirstOrDefault(pair => pair.Value == categoryIndex);
        if (foundToggle)
        {
            foundToggle.interactable = interactable;
        }
    }

    private void SetupCodexTopic(Topic topic, Category category)
    {
        if (!CheckValidTopic(topic, category)) return;
        
        var topicSection = _topicSectionPool.Get();
        var topicToggleTransform = topicSection.transform.Find("TopicToggle");

        //set topic name
        var textComponent = topicToggleTransform.Find("Text").GetComponent<TextMeshProUGUI>();
        textComponent.text = topic.name;

        //set toggle off
        var toggleComponent = topicToggleTransform.GetComponent<Toggle>();
        toggleComponent.isOn = false;
        
        //deactivate entries section for the topic
        var entriesSection = topicSection.transform.Find("EntriesSection");
        entriesSection.gameObject.SetActive(false);
        
        if (!_topicTogglesToEntrySections.ContainsKey(toggleComponent))
        {
            _topicTogglesToEntrySections.Add(toggleComponent,
                entriesSection.gameObject);
        }
        
        //listen to changes in topic toggle state
        toggleComponent.onValueChanged.RemoveAllListeners();
        toggleComponent.onValueChanged.AddListener(delegate { TopicToggleChanged(toggleComponent); });

        foreach (var entry in topic.entries)
        {
            SetupCodexEntry(entriesSection, entry, topic);
        }
    }

    private static bool CheckValidTopic(Topic topic, Category category)
    {
        if (string.IsNullOrEmpty(topic.name) || topic.entries == null || !topic.entries.Any())
        {
            Debug.LogError($"Topic {topic.name} in category {category.name} is malformed");
            return false;
        }

        return true;
    }

    private void SetupCodexEntry(Transform entriesSection, Entry entry, Topic topic)
    {
        if(!CheckValidEntry(entry, topic)) return;
        
        //add entry button to topic entries section
        GameObject button = _entrySectionPool.Get();
        button.transform.SetParent(entriesSection);
        
        //set button text
        var textComponent = button.transform.Find("Text");
        var textElem = textComponent.GetComponent<TextMeshProUGUI>();
        textElem.text = entry.name;

        //listen to entry button clicks
        var buttonComponent = button.GetComponent<Button>();
        buttonComponent.onClick.RemoveAllListeners();
        buttonComponent.onClick.AddListener(delegate { EntryButtonClicked(entry.name); });
    }

    private bool CheckValidEntry(Entry entry, Topic topic)
    {
        if (string.IsNullOrEmpty(entry.name) || string.IsNullOrEmpty(entry.text))
        {
            Debug.LogError($"Entry {entry.name} in topic {topic.name} is malformed");
            return false;
        }

        return true;
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
        tokenHealthText.text = health.ToString();
        tokenAttackText.text = attack.ToString();
        tokenDefenceText.text = defence.ToString();

        var screenPosition = cameraMain.WorldToScreenPoint(position);
        tokenInfoPanel.transform.position = screenPosition;
        tokenInfoPanel.SetActive(true);
    }

    public void UpdateTokenInfoPanel(int health, int attack, int defence)
    {
        tokenHealthText.text = health.ToString();
        tokenAttackText.text = attack.ToString();
        tokenDefenceText.text = defence.ToString();
    }

    public void UpdateActionPoints(int newPoints)
    {
        actionPointsText.text = newPoints.ToString();
    }

    public void UpdateTurnCounter(int newCounter)
    {
        currentTurnText.text = newCounter.ToString();
    }

    public void UpdateCurrentPlayer(string currentPlayer)
    {
        currentPlayerText.text = currentPlayer;
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

    public bool AreActionButtonsActive()
    {
        return actionButtons.activeSelf;
    }

    public void CodexToggleChanged(Toggle toggle)
    {
        codexToggleChanged.Invoke(toggle.isOn);
        endTurnButton.interactable = !toggle.isOn;
        codexCanvas.gameObject.SetActive(toggle.isOn);

        if (toggle.isOn)
        {
            LoadCategory(0);
        }
    }

    private void TopicToggleChanged(Toggle toggle)
    {
        var found = _topicTogglesToEntrySections.TryGetValue(toggle, out GameObject section);

        if (!found || !section) return;

        var textComponent = toggle.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        _selectedCodexSections.Topic = textComponent.text;
        section.SetActive(toggle.isOn);
    }

    private void CategoryToggleChanged(Toggle toggle)
    {
        var found = _categoryTogglesToIndices.TryGetValue(toggle, out int index);
        if (found)
        {
            LoadCategory(index);
        }
    }

    private void EntryButtonClicked(string entryName)
    {
        topicsScrollView.SetActive(false);

        //find entry in codex to populate UI elements
        var category = _codex.categories[_selectedCodexSections.CategoryIndex];
        var topic = category.topics.Find(topic => topic.name == _selectedCodexSections.Topic);
        var entry = topic.entries.Find(entry => entry.name == entryName);

        //set entry text fields
        titleSectionText.text = entry.name;
        entryText.text = entry.text;
        
        //optionally set the entry image
        var result = LoadImage(entry.image, out Texture2D tex);
        if (result)
        {
            entryImage.sprite =
                Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            entryImage.gameObject.SetActive(true);
        }
        else
        {
            entryImage.gameObject.SetActive(false);
        }
        
        //switch from category navigation to entry navigation
        categoriesSection.SetActive(false);
        entryNavSection.SetActive(true);
        
        //listen to back navigation toggle changes
        backNavToggle.onValueChanged.RemoveAllListeners();
        backNavToggle.onValueChanged.AddListener(delegate { OnBackNavToggle(); });
        
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
            Debug.LogWarning($"Could not load image at path {path}. " + e);
            return false;
        }

        return true;
    }

    private void OnBackNavToggle()
    {
        topicsScrollView.SetActive(true);
        entryScrollView.SetActive(false);

        LoadCategory(_selectedCodexSections.CategoryIndex);

        categoriesSection.SetActive(true);
        entryNavSection.SetActive(false);
    }

    private static string ReadFile(string path)
    {
        var fileContents = "";
        try
        {
            using var stream = new StreamReader(path);
            fileContents = stream.ReadToEnd();
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not read file at path: {path} - Got exception: {exception}");
            ApplicationManager.QuitGame();
        }

        return fileContents;
    }
}