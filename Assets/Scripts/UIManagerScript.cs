using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
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

public class UIManagerScript : MonoBehaviour
{
    private const string CodexPath = "./Assets/Data/codex.json";

    private TextMeshProUGUI _codexSectionTitleText;
    private readonly Dictionary<Toggle, GameObject> _topicTogglesToEntrySections = new();
    private readonly Dictionary<Toggle, int> _categoryTogglesToCategoryIndices = new();

    public GameObject codexSection;
    public GameObject topicsSectionPrefab;

    private void Awake()
    {
        var codexTitleSection = codexSection.transform.Find("CategoryTitleSection");
        var textSection = codexTitleSection?.Find("TitleText").gameObject;
        _codexSectionTitleText = textSection?.GetComponent<TextMeshProUGUI>();

        var jsonCodex = ReadFile(CodexPath);

        //TODO handle parsing errors
        var codex = JsonUtility.FromJson<Codex>(jsonCodex);
        
        if (codex.categories != null && _codexSectionTitleText)
            _codexSectionTitleText.text = codex.categories[0].name;

        if (codex.categories == null)
        {
            Debug.LogError("Could not find categories");
            return;
        }
            
        LoadCategory(codex.categories[0]);
    }

    private void LoadCategory(Category codexCategory)
    {
        var topicsSectionTransform = codexSection.transform.Find("TopicsContainerSection").Find("TopicsSection");
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
            childTransform.gameObject.SetActive(false);
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
            _topicTogglesToEntrySections.Add(toggle, entriesSection.gameObject);

            toggle.onValueChanged.AddListener(delegate {
                TopicToggleChanged(toggle);
            });
                
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
            }
        }

    }


    public void CodexToggleChanged(Toggle change)
    {
        Debug.Log("toggle:" + change.isOn);
        codexSection.SetActive(!codexSection.activeSelf);
    }

    private void TopicToggleChanged(Toggle toggle)
    {
        var found = _topicTogglesToEntrySections.TryGetValue(toggle, out GameObject section);

        if (found && section)
        {
            section.SetActive(!section.activeSelf);
        }
    }

    private static string ReadFile(string path)
    {
        using StreamReader stream = new StreamReader(path);
        var fileContents = stream.ReadToEnd();
        stream.Close();
        return fileContents;
    }
}