using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Civilisation : MonoBehaviour
{
    public JsonParser.Data data;
    [SerializeField ]private GameObject _resultPanel;
    [SerializeField] private Slider _religion;
    [SerializeField] private Slider _social;
    [SerializeField] private Slider _science;
    [SerializeField] private Slider _conquest;
    [SerializeField] private Slider _trustIndice;
    private Vector4 _currentValues;
    public Vector4 tmpSubject;
    public Vector4 tmpVerb;
    public Vector4 tmpObject;
    private GameObject _notifPanel;
    [FormerlySerializedAs("normalColors")] [SerializeField] private ColorBlock _normalColors;
    [FormerlySerializedAs("aboveColors")] [SerializeField] private ColorBlock _aboveColors;
    [FormerlySerializedAs("beneathColors")] [SerializeField] private ColorBlock _beneathColors;
    [FormerlySerializedAs("_exit")] [SerializeField] private GameObject _open;
    [SerializeField] private TextMeshProUGUI _autoTechText;
    [SerializeField]private GameObject _panelAbstract;
    [FormerlySerializedAs("_openButton")] public GameObject OpenButton;
    private Word[] _words;
    private bool _hitting = false;
    private int _indexTech = 0;
    [SerializeField] private GameObject _content;
    [SerializeField]private GameObject _prefab;
    private Subjects _subjects;
    private Verbs _verbs;
    private Objects _objects;
    private bool _techFound = false;
    [FormerlySerializedAs("_done")] public bool done = false;
    [SerializeField] private TechImageGenerator _imageGenerator = new TechImageGenerator();
    private readonly Dictionary<string, Sprite> _techSprites = new Dictionary<string, Sprite>();
    private bool _freshStart;
    private RectTransform _researchPanel;
    private readonly Dictionary<string, ResearchItem> _researchItems = new Dictionary<string, ResearchItem>();

    // Start is called before the first frame update

    public void CloseCivilisation()
    {
        OpenButton.SetActive(true);
        Player.Instance.Close();
    }

    public void OpenCivilisation()
    {
        if (_trustIndice.value < 10f || done)
        {
            Notifications.Instance.gameObject.SetActive(true);
            Notifications.Instance.TimeVisible += 2.5f;
            Notifications.Instance.AddText(gameObject.name +  " don't want to deal with you anymore!");
        }
        else
        {
            OpenButton.SetActive(false);
            Player.Instance.Open(this.gameObject);
            _open.SetActive(true);
        }
    }

    void Awake()
    {
        if (Player.Instance.debug || !File.Exists(Application.persistentDataPath + "/" + gameObject.name + ".save"))
        {
            _freshStart = true;
            _religion.value = Random.Range(30f, 50f);
            _social.value = Random.Range(30f, 50f);
            _science.value = Random.Range(30f, 50f);
            _conquest.value = Random.Range(30f, 50f);
            data = JsonParser.Instance.GetData();
            for (int i = 0; i < data.TechnologyTree.Count; i++)
            {
                if (data.TechnologyTree[i].dependances.Count == 0)
                {
                    JsonParser.Technos tmpStruct = data.TechnologyTree[i];
                    tmpStruct.useable = true;
                    tmpStruct.learned = true;
                    for (int j = 0; j < tmpStruct.words.objects.Count; j++)
                    {
                        JsonParser.Word tmpStructDescends = tmpStruct.words.objects[j];
                    
                        tmpStructDescends.useable = true;
                        tmpStruct.words.objects[j] = tmpStructDescends;
                    }
                    for (int j = 0; j < tmpStruct.words.subjects.Count; j++)
                    {
                        JsonParser.Word tmpStructDescends = tmpStruct.words.subjects[j];
                       
                        tmpStructDescends.useable = true;
                        tmpStruct.words.subjects[j] = tmpStructDescends;
                    }
                    for (int j = 0; j < tmpStruct.words.verbs.Count; j++)
                    {
                        JsonParser.Word tmpStructDescends = tmpStruct.words.verbs[j];
                       
                        tmpStructDescends.useable = true;
                        tmpStruct.words.verbs[j] = tmpStructDescends;
                    }
                    data.TechnologyTree[i] = tmpStruct;
                }
            }
        }
        else
        {
            if (PlayerPrefs.HasKey("religion" + gameObject.name))
                _religion.value = PlayerPrefs.GetFloat("religion" + gameObject.name);
            if (PlayerPrefs.HasKey("science" + gameObject.name))
                _science.value = PlayerPrefs.GetFloat("science" + gameObject.name);
            if (PlayerPrefs.HasKey("social" + gameObject.name))
                _social.value = PlayerPrefs.GetFloat("social" + gameObject.name);
            if (PlayerPrefs.HasKey("conquest" + gameObject.name))
                _conquest.value = PlayerPrefs.GetFloat("conquest" + gameObject.name);
            if (File.Exists(Application.persistentDataPath + "/" + gameObject.name + ".save"))
            {
                data = JsonUtility.FromJson<JsonParser.Data>(File.ReadAllText(Application.persistentDataPath + "/" + gameObject.name + ".save"));
            }
        }
        _currentValues = new Vector4(_religion.value, _social.value, _science.value, _conquest.value);
        _trustIndice.value = 100f - (Mathf.Max(Mathf.Max(_religion.value, _social.value),
                                         Mathf.Max(_science.value, _conquest.value)) -
                                     Mathf.Min(Mathf.Min(_religion.value, _social.value), Mathf.Min(_science.value, _conquest.value)));

        _subjects = GetComponentInChildren<Subjects>();
        _objects = GetComponentInChildren<Objects>();
        _verbs = GetComponentInChildren<Verbs>();
        _notifPanel = GameObject.FindWithTag("Notifications");
        _panelAbstract.SetActive(false);
        _open.SetActive(false);

    }

    void Start()
    {
        // The spawner names the civilisation after Awake, so its image folder is only known here.
        // A new game starts from the shared images again.
        string folder = TechImageGenerator.ImageFolder(gameObject.name);
        if (_freshStart && Directory.Exists(folder))
            Directory.Delete(folder, true);

        TechImageLibrary.Changed += OnTechImagesChanged;
        if (!TechImageLibrary.StartImagesRequested)
        {
            // The starting techs' and words' images are the same for every civilisation,
            // so only the first one asks for them.
            TechImageLibrary.StartImagesRequested = true;
            Player.Instance.StartCoroutine(
                _imageGenerator.GenerateSharedImages(JsonParser.Instance.GetData().TechnologyTree));
        }

        BuildResearchPanel();
        UpdateResearchPanel();
    }

    void OnDestroy()
    {
        TechImageLibrary.Changed -= OnTechImagesChanged;
    }

    private void OnTechImagesChanged()
    {
        RefreshWordLists();
        UpdateResearchPanel();
    }

    /// <summary>
    /// This civilisation's own image for a technology, or the shared starting one if it has none yet.
    /// </summary>
    public Sprite GetTechSprite(string techName)
    {
        return TryGetOwnTechSprite(techName, out Sprite sprite) ? sprite : TechImageLibrary.GetStartSprite(techName);
    }

    private bool TryGetOwnTechSprite(string techName, out Sprite sprite)
    {
        if (_techSprites.TryGetValue(techName, out sprite) && sprite != null)
            return true;
        string path = TechImageGenerator.ImagePath(gameObject.name, techName);
        if (File.Exists(path))
        {
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(File.ReadAllBytes(path)))
            {
                sprite = TechImageLibrary.ToSprite(texture);
                _techSprites[techName] = sprite;
                return true;
            }
        }
        sprite = null;
        return false;
    }

    private void RefreshWordLists()
    {
        _subjects.OnEnable();
        _objects.OnEnable();
        _verbs.OnEnable();
    }

    /// <summary>
    /// Row in the lower left of the civilisation panel showing the techs being researched,
    /// with their image as soon as it's generated.
    /// </summary>
    private void BuildResearchPanel()
    {
        GameObject panel = new GameObject("Research", typeof(RectTransform));
        _researchPanel = panel.GetComponent<RectTransform>();
        _researchPanel.SetParent(_open.transform, false);
        _researchPanel.anchorMin = new Vector2(0.035f, 0.05f);
        _researchPanel.anchorMax = new Vector2(0.335f, 0.45f);
        _researchPanel.offsetMin = Vector2.zero;
        _researchPanel.offsetMax = Vector2.zero;
        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.LowerLeft;
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private ResearchItem CreateResearchItem(string techName)
    {
        GameObject root = new GameObject(techName, typeof(RectTransform));
        root.transform.SetParent(_researchPanel, false);
        VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        GameObject imageObject = new GameObject("Image", typeof(RectTransform));
        imageObject.transform.SetParent(root.transform, false);
        Image image = imageObject.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        LayoutElement imageSize = imageObject.AddComponent<LayoutElement>();
        imageSize.preferredWidth = 120f;
        imageSize.preferredHeight = 120f;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(root.transform, false);
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = _autoTechText.font;
        label.color = _autoTechText.color;
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        LayoutElement labelSize = labelObject.AddComponent<LayoutElement>();
        labelSize.preferredWidth = 120f;
        labelSize.preferredHeight = 44f;

        return new ResearchItem { root = root, image = image, label = label };
    }

    private void UpdateResearchPanel()
    {
        if (_researchPanel == null)
            return;
        HashSet<string> researching = new HashSet<string>();
        foreach (JsonParser.Technos tech in data.TechnologyTree)
        {
            if (!tech.useable || tech.learned)
                continue;
            researching.Add(tech.name);
            if (!_researchItems.TryGetValue(tech.name, out ResearchItem item))
            {
                item = CreateResearchItem(tech.name);
                _researchItems[tech.name] = item;
            }
            // Until its image is generated, the slot stays a faint placeholder.
            bool hasImage = TryGetOwnTechSprite(tech.name, out Sprite sprite);
            item.image.sprite = sprite;
            item.image.color = hasImage ? Color.white : new Color(1f, 1f, 1f, 0.15f);
            item.label.text = tech.name + "\n" + Mathf.Max(0, Mathf.CeilToInt(tech.time)) + " years";
        }

        List<string> finished = new List<string>();
        foreach (KeyValuePair<string, ResearchItem> pair in _researchItems)
        {
            if (!researching.Contains(pair.Key))
                finished.Add(pair.Key);
        }
        foreach (string techName in finished)
        {
            Destroy(_researchItems[techName].root);
            _researchItems.Remove(techName);
        }
    }

    private class ResearchItem
    {
        public GameObject root;
        public Image image;
        public TextMeshProUGUI label;
    }

    /// <summary>
    /// Starts generating this civilisation's image for a technology it begins to research,
    /// from the words used and the images of the technologies it comes from.
    /// </summary>
    private void RequestTechImage(JsonParser.Technos tech, List<string> parentTechs, string phrase)
    {
        tech.imagePhrase = phrase;
        if (!_imageGenerator.enabled)
            return;

        List<string> parentPhrases = new List<string>();
        List<Sprite> parentImages = new List<Sprite>();
        foreach (string parentName in new HashSet<string>(parentTechs))
        {
            JsonParser.Technos parent = data.TechnologyTree.Find(x => x.name == parentName);
            if (parent != null && !string.IsNullOrEmpty(parent.imagePhrase))
                parentPhrases.Add(parent.imagePhrase);
            Sprite parentImage = GetTechSprite(parentName);
            if (parentImage != null)
                parentImages.Add(parentImage);
        }

        string techName = tech.name;
        string prompt = _imageGenerator.BuildPrompt(techName, phrase, parentPhrases, _currentValues,
            parentImages.Count > 0);
        // Run on the Player: civilisations get deactivated while another one is open, which would stop the coroutine.
        Player.Instance.StartCoroutine(_imageGenerator.Generate(techName, prompt, parentImages,
            TechImageGenerator.ImagePath(gameObject.name, techName),
            texture =>
            {
                Sprite sprite = TechImageLibrary.ToSprite(texture);
                if (this != null)
                    _techSprites[techName] = sprite;
                // Refreshes every civilisation's lists and research row, and the word keeper's next popup.
                TechImageLibrary.SetLatestSprite(techName, sprite);
            }));
    }

    public void AddTurn()
    {
        for (int i = 0; i < data.TechnologyTree.Count; i++)
        {
            if (data.TechnologyTree[i].useable && !data.TechnologyTree[i].learned)
            {
                JsonParser.Technos tech = data.TechnologyTree[i];
                tech.time -= 1;
                if (tech.time <= 0)
                {
                    JsonParser.Technos tmpStructPlayer = Player.Instance.data.TechnologyTree[i];
                    Notifications.Instance.gameObject.SetActive(true);
                    Notifications.Instance.TimeVisible += 2.5f;
                    Notifications.Instance.AddText("New Technology " + gameObject.name + " : " + tech.name);
                    tech.learned = true;
                    tmpStructPlayer.learned = true;
                    Player.Instance.data.TechnologyTree[i] = tmpStructPlayer;
                    data.TechnologyTree[i] = tech;
                    RefreshWordLists();
                    if (tech.name == "aerospatial")
                        Player.Instance.Done("End");
                }
                else
                {
                    data.TechnologyTree[i] = tech;
                }
            }
        }
        UpdateResearchPanel();
    }

    public void ExecuteResult()
    {
        _words = _resultPanel.GetComponentsInChildren<Word>();
        if (_words.Length != 3)
            return;

        // The words are always heard by the civilisation, whether or not they lead to a new technology.
        List<string> wordTechs = GetWordTechs();
        int techIndex = FindNewTech(wordTechs, out string knownTech);
        _techFound = techIndex >= 0;

        Notifications.Instance.gameObject.SetActive(true);
        Notifications.Instance.TimeVisible += 2.5f;
        if (_techFound)
            Notifications.Instance.AddText(gameObject.name + " start to research : " + data.TechnologyTree[techIndex].name);
        else if (knownTech != null)
            Notifications.Instance.AddText(knownTech + " already known!");
        else
            Notifications.Instance.AddText(gameObject.name + " : no new technology.");

        JsonParser.Technos tmpStruct = default;
        JsonParser.Technos tmpStructPlayer = default;
        if (_techFound)
        {
            tmpStruct = data.TechnologyTree[techIndex];
            tmpStructPlayer = Player.Instance.data.TechnologyTree[techIndex];
            tmpStruct.useable = true;
            tmpStruct.time = ComputeTurnTech(tmpStruct);
        }

        _currentValues += Vector4.Scale((tmpObject + tmpVerb), tmpSubject * (_trustIndice.value / 100f));
        _religion.value = _currentValues.x;
        _social.value = _currentValues.y;
        _science.value = _currentValues.z;
        _conquest.value = _currentValues.w;
        _trustIndice.value = ComputeNextTrustIndice();
        tmpObject = Vector4.zero;
        tmpSubject = Vector4.zero;
        tmpVerb = Vector4.zero;

        if (_techFound)
        {
            if (_trustIndice.value > 50f)
            {
                if (tmpStruct.words.subjects.Count > 0)
                    tmpStruct = GetNewWord(tmpStruct.words.subjects, tmpStruct, Word.wordType.subject,
                        ref tmpStructPlayer);
                else if (tmpStruct.words.verbs.Count > 0)
                    tmpStruct = GetNewWord(tmpStruct.words.verbs, tmpStruct, Word.wordType.verb,
                        ref tmpStructPlayer);
                else if (tmpStruct.words.objects.Count > 0)
                    tmpStruct = GetNewWord(tmpStruct.words.objects, tmpStruct, Word.wordType.obj,
                        ref tmpStructPlayer);
            }
            data.TechnologyTree[techIndex] = tmpStruct;
            Player.Instance.data.TechnologyTree[techIndex] = tmpStructPlayer;
            // Uses the values just updated by these words, so the image reflects how the tech was learned.
            RequestTechImage(tmpStruct, wordTechs, GetWordPhrase());
            UpdateResearchPanel();
        }

        if (_trustIndice.value < 2f)
        {
            CloseCiv("trust");
        }
        if (_religion.value >= 100f || _religion.value <= 2f)
        {
            CloseCiv("religion");
        }
        if (_social.value >= 100f || _social.value <= 2f)
        {
            CloseCiv("social");
        }
        if (_science.value >= 100f || _science.value <= 2f)
        {
            CloseCiv("science");
        }
        if (_conquest.value >= 100f || _conquest.value <= 2f)
        {
            CloseCiv("conquest");
        }

        for (int i = 0; i < _words.Length; i++)
        {
            Destroy(_words[i].gameObject);
        }
    }

    /// <summary>
    /// The words in the result panel as "subject verb object".
    /// </summary>
    private string GetWordPhrase()
    {
        string subject = "", verb = "", obj = "";
        foreach (Word word in _words)
        {
            if (word.type == Word.wordType.subject)
                subject = word.word.name;
            else if (word.type == Word.wordType.verb)
                verb = word.word.name;
            else
                obj = word.word.name;
        }
        return (subject + " " + verb + " " + obj).Trim();
    }

    /// <summary>
    /// Technologies of the verb and object words currently in the result panel.
    /// </summary>
    private List<string> GetWordTechs()
    {
        List<string> techs = new List<string>();
        for (int i = 0; i < _words.Length; i++)
        {
            if (_words[i].type != Word.wordType.subject)
                techs.Add(_words[i].technos.name);
        }
        return techs;
    }

    /// <summary>
    /// Index of the first technology not yet researched whose dependencies contain all the given techs, or -1.
    /// knownTech receives the name of a matching technology the civilisation already has, if any.
    /// </summary>
    private int FindNewTech(List<string> techs, out string knownTech)
    {
        knownTech = null;
        if (techs.Count == 0)
            return -1;
        for (int i = 0; i < data.TechnologyTree.Count; i++)
        {
            List<string> dependances = data.TechnologyTree[i].dependances;
            if (!techs.TrueForAll(dependances.Contains))
                continue;
            if (!data.TechnologyTree[i].useable)
                return i;
            if (knownTech == null)
                knownTech = data.TechnologyTree[i].name;
        }
        return -1;
    }

    private float ComputeNextTrustIndice()
    {
        return (100f - (Mathf.Max(Mathf.Max(_currentValues.x, _currentValues.y),
                            Mathf.Max(_currentValues.z, _currentValues.w)) -
                        Mathf.Min(Mathf.Min(_currentValues.x, _currentValues.y),
                            Mathf.Min(_currentValues.z, _currentValues.w))));
    }

    void CloseCiv(string reason)
    {
        done = true;
        Player.Instance.Done(reason);
        Player.Instance.Close();
        _open.SetActive(false);
        OpenButton.SetActive(true);
    }

    
    
    float ComputeTurnTech(JsonParser.Technos tmpStruct)
    {
        float time = tmpStruct.time;
        time *= Vector4.Distance(tmpStruct.limits,
            new Vector4(_religion.value, _social.value, _science.value, _conquest.value)) / 25f;
                    
        if (tmpStruct.type == "religion")
            time *= 1 - (_religion.value / 100f);
        else if (tmpStruct.type == "social") 
            time *= 1 - (_social.value / 100f);
        else if (tmpStruct.type == "science") 
            time *= 1 - (_science.value / 100f);
        else if (tmpStruct.type == "conquest")
            time *= 1 - (_conquest.value / 100f);
        return (time);
    }

    private JsonParser.Technos GetNewWord(List<JsonParser.Word> words, JsonParser.Technos techs,
        Word.wordType type, ref JsonParser.Technos playerStruct)
    {
        int index = 0;
        for (int i = 0; i < words.Count; i++)
        {
            if ((type == Word.wordType.subject && !playerStruct.words.subjects[index].useable) ||
                (type == Word.wordType.verb && !playerStruct.words.verbs[index].useable) ||
                (type == Word.wordType.obj && !playerStruct.words.objects[index].useable))
            {
                index = i;
                break;
            }
        }
        if (type == Word.wordType.subject)
        {
            JsonParser.Word tmpWordsPlayer = playerStruct.words.subjects[index];
            if (!tmpWordsPlayer.useable)
            {
                Notifications.Instance.gameObject.SetActive(true);
                Notifications.Instance.TimeVisible += 2.5f;
                Notifications.Instance.AddText("New Word : " + tmpWordsPlayer.name);
            }
    
            tmpWordsPlayer.useable = true;
            playerStruct.words.subjects[index] = tmpWordsPlayer;
            JsonParser.Word tmpWords = techs.words.subjects[index];
            tmpWords.useable = true;
            techs.words.subjects[index] = tmpWords;
        }
        else if(type == Word.wordType.verb)
        {
            JsonParser.Word tmpWordsPlayer = playerStruct.words.verbs[index];
            if (!tmpWordsPlayer.useable)
            {
                Notifications.Instance.TimeVisible += 2.5f;
                Notifications.Instance.AddText("New Word : " + tmpWordsPlayer.name);
            }
            tmpWordsPlayer.useable = true;
            playerStruct.words.verbs[index] = tmpWordsPlayer;
            JsonParser.Word tmpWords = techs.words.verbs[index];
            tmpWords.useable = true;
            techs.words.verbs[index] = tmpWords;
        }
        else if(type == Word.wordType.obj)
        {
            JsonParser.Word tmpWordsPlayer = playerStruct.words.objects[index];
            if (!tmpWordsPlayer.useable)
            {
                Notifications.Instance.TimeVisible += 2.5f;
                Notifications.Instance.AddText("New Word : " + tmpWordsPlayer.name);
            }
            tmpWordsPlayer.useable = true;
            playerStruct.words.objects[index] = tmpWordsPlayer;
            JsonParser.Word tmpWords = techs.words.objects[index];
            tmpWords.useable = true;
            techs.words.objects[index] = tmpWords;
        }
        return (techs);
    }

    private void OnApplicationQuit()
    {
        if (!Player.Instance.debug)
        {
            File.WriteAllText(Application.persistentDataPath + "/" + gameObject.name + ".save", JsonUtility.ToJson(data));
            PlayerPrefs.SetFloat("religion" + gameObject.name, _religion.value);
            PlayerPrefs.SetFloat("science" + gameObject.name, _science.value);
            PlayerPrefs.SetFloat("social" + gameObject.name, _social.value);
            PlayerPrefs.SetFloat("conquest" + gameObject.name, _conquest.value);
            PlayerPrefs.Save();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // RaycastHit2D hit = Physics2D.Raycast(Input.mousePosition, Vector2.zero);
        // if (hit && hit.collider.CompareTag("Civilisation") && (hit.collider.gameObject == _openButton || hit.collider.gameObject == _panelAbstract))
        // {
        //     Debug.Log(hit.collider.tag);
        //     if (!_hitting)
        //     {
        //         for (int i = 0; i < data.TechnologyTree.Count; i++)
        //         {
        //             if (data.TechnologyTree[i].useable)
        //             {
        //                 for (int j = 0; j < data.TechnologyTree[i].autoTechs.Count; j++)
        //                 {
        //                     if (data.TechnologyTree[i].autoTechs[j].useable)
        //                     {
        //                         GameObject go = Instantiate<GameObject>(_prefab, Vector3.zero, Quaternion.identity, _content.transform);
        //                         go.GetComponentInChildren<TextMeshProUGUI>().text =
        //                             data.TechnologyTree[i].autoTechs[j].name;
        //                         if (Resources.Load<Sprite>(data.TechnologyTree[i].autoTechs[j].name))
        //                             go.GetComponentInChildren<Image>().sprite =
        //                                 Resources.Load<Sprite>(data.TechnologyTree[i].autoTechs[j].name);
        //                     }
        //                     _hitting = true;
        //                     _panelAbstract.SetActive(true);
        //                 }
        //             }
        //         }
        //         
        //     }
        // }
        // else
        // {
        //     foreach (Transform child in _content.transform)
        //         Destroy(child.gameObject);
        //     _panelAbstract.SetActive(false);
        //     _hitting = false;
        // }

        if (tmpObject != Vector4.zero || tmpVerb != Vector4.zero || tmpSubject != Vector4.zero)
        {
            float tmpValue = _trustIndice.value;
            _religion.value = _currentValues.x + (tmpObject.x + tmpVerb.x) * (tmpSubject.x * (tmpValue / 100f));
            _social.value = _currentValues.y + (tmpObject.y + tmpVerb.y) * (tmpSubject.y * (tmpValue / 100f));
            _science.value = _currentValues.z + (tmpObject.z + tmpVerb.z) * (tmpSubject.z * (tmpValue / 100f));
            _conquest.value = _currentValues.w + (tmpObject.w + tmpVerb.w) * (tmpSubject.w * (tmpValue / 100f));
            float tmpIndice = ComputeNextTrustIndice();
            if (tmpIndice > _trustIndice.value)
                _trustIndice.colors = _aboveColors;
            else if (tmpIndice < _trustIndice.value)
                _trustIndice.colors = _beneathColors;
            if (_religion.value > _currentValues.x)
                _religion.colors = _aboveColors;
            if (_religion.value < _currentValues.x)
                _religion.colors = _beneathColors;
            if (_social.value > _currentValues.y)
                _social.colors = _aboveColors;
            if (_social.value < _currentValues.y)
                _social.colors = _beneathColors;
            if (_science.value > _currentValues.z)
                _science.colors = _aboveColors;
            if (_science.value < _currentValues.z)
                _science.colors = _beneathColors;
            if (_conquest.value > _currentValues.w)
                _conquest.colors = _aboveColors;
            if (_conquest.value < _currentValues.w)
                _conquest.colors = _beneathColors;
        }
        else
        {
            _religion.value = _currentValues.x;
            _social.value = _currentValues.y;
            _science.value = _currentValues.z;
            _conquest.value = _currentValues.w;
            _religion.colors = _normalColors;
            _social.colors = _normalColors;
            _science.colors = _normalColors;
            _conquest.colors = _normalColors;
            _trustIndice.colors = _normalColors;
        }
        _words = _resultPanel.GetComponentsInChildren<Word>();
        if (_words.Length != 3)
        {
            _autoTechText.text = "";
            return;
        }

        int techIndex = FindNewTech(GetWordTechs(), out _);
        if (techIndex >= 0)
        {
            int turns = Mathf.FloorToInt(ComputeTurnTech(data.TechnologyTree[techIndex]));
            _autoTechText.text = "New Tech : " + data.TechnologyTree[techIndex].name + Environment.NewLine +
                                 "Years : " + turns;
        }
        else
        {
            _autoTechText.text = "No new technology.";
        }
    }
}
