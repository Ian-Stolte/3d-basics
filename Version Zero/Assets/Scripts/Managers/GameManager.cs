    using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    [Header("Bools")]
    public bool doubleSpeed;
    public bool scifiNames;
    private bool fullArea;
    public bool skipDialogue;
    [HideInInspector] public bool pauseGame;
    [HideInInspector] public bool playerPaused;
    
    [Header("Rooms")]
    [SerializeField] private Room[] rooms;
    private int levelNum = 1;
    [SerializeField] private TextMeshProUGUI areaText;
    [SerializeField] private TextMeshProUGUI areaIntroText;
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private int[] bossRooms;
    private int bossIndex;
    
    [Header("Enemy Spawn")]
    public bool staticSpawn;
    public int numEnemies;
    [SerializeField] private List<string> enemyPrefabs; //TODO: change to struct w/ spawn pct, weight, etc
    [SerializeField] private string[] enemyTypes;
    private string enemyType = "Logic";
    [SerializeField] private Transform nodeParent;
    [SerializeField] private Transform enemyParent;
    [SerializeField] private List<int> waves = new List<int>();
    private float minSpawn = 15;
    private float maxSpawn = 25;

    [Header("Terminals")]
    [SerializeField] private GameObject terminalBar;
    [HideInInspector] public Image bar;
    [HideInInspector] public Terminal currentTerminal;
    [HideInInspector] public int numTerminals;
    public KeyCode terminalBind;
    [SerializeField] private Transform terminalIcons;
    [SerializeField] private GameObject terminalIcon;

    [Header("Barrier")]
    [SerializeField] private Color unlockTextColor;
    [SerializeField] private Material barrierGreen;
    [SerializeField] private Material barrierUnlockBlue;

    [Header("Dialogue")]
    [SerializeField] private string[] reyaDialogue;
    [SerializeField] private GameObject dialogue;
    [SerializeField] private GameObject[] portraits;

    [Header("Misc")]
    [SerializeField] private GameObject rewardPrefab;
    private Transform player;
    [SerializeField] private GameObject bossTxt;
    [SerializeField] private GameObject loadingText;

    
    void Start()
    {
        if (SceneManager.GetActiveScene().name.Contains("Level"))
            fullArea = true;
        player = GameObject.Find("Player").transform;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Playtest Options")
            Destroy(gameObject);
        else
        {
            enemyParent = GameObject.Find("Enemies").transform;
            //nodeParent = GameObject.Find("Spawn Nodes").transform;
            /*if (levelNum != 1 && !scene.name.Contains("Boss"))
            {
                enemyType = enemyTypes[Random.Range(0, enemyTypes.Length)];
                if (staticSpawn)
                    SetupEnemies(levelNum + Random.Range(1, 4));
                else
                {
                    if (levelNum == 2)
                        SetupWaves(3);
                    else
                        SetupWaves(levelNum + Random.Range(1, 4));
                }
            }
            else
            {*/
                numEnemies = Physics.OverlapSphere(Vector2.zero, 9999, LayerMask.GetMask("Enemy")).Length;
            //}

            if (scene.name.Contains("Level"))
            {
                foreach (Transform child in terminalIcons)
                    Destroy(child.gameObject);
                
                //create an icon for each terminal in the level
                numTerminals = 0;
                foreach (GameObject g in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (g.layer == LayerMask.NameToLayer("Terminal") && g.hideFlags == HideFlags.None && g.scene.IsValid())
                        numTerminals++;
                }
                for (int i = 0; i < numTerminals; i++)
                {
                    GameObject icon = Instantiate(terminalIcon, Vector2.zero, terminalIcon.transform.rotation, terminalIcons);
                    icon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-822, 400 - 130*i);
                }

                //set spawn pct & enemies available by level
                if (scene.name == "Level 2" || scene.name == "Level 3")
                {
                    minSpawn = 15;
                    maxSpawn = 25;
                }
                if (scene.name == "Level 4")
                {
                    enemyPrefabs.Add("Artillerist");
                    minSpawn = 10;
                    maxSpawn = 20;
                }
                
                if (scene.name != "Level 1" && scene.name != "Level 2")
                    StartCoroutine(SpawnInfiniteWaves());
            }
        }

        if (scene.name == "Level 1")
        {
            StartCoroutine(IntroDialogue());
        }
    }

    private IEnumerator IntroDialogue()
    {
        dialogue.SetActive(true);
        TextMeshProUGUI txt = dialogue.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        if (!skipDialogue)
        {
            for (int i = 0; i < reyaDialogue.Length; i++)
            {
                float slowDown = (i < 2) ? 1.5f : 1f;
                portraits[0].SetActive(reyaDialogue[i][0] != '~');
                portraits[1].SetActive(reyaDialogue[i][0] == '~');
                txt.text = "";
                foreach (char c in reyaDialogue[i])
                {
                    if (c == '*')
                        yield return new WaitForSeconds(0.15f);
                    else if (c != '~')
                    {
                        txt.text += c;
                        if (c == '.' || c == ',')
                            yield return new WaitForSeconds(0.10f * slowDown);
                        else if (c == ' ')
                            yield return new WaitForSeconds(0.10f * slowDown);
                        else
                            yield return new WaitForSeconds(0.05f * slowDown);
                    }
                }
                if (i == reyaDialogue.Length-2)
                {
                    Fader.Instance.FadeOut(14);
                    AudioManager.Instance.Play("Area 1");
                    StartCoroutine(AudioManager.Instance.StartFade("Area 1", 0.5f, 0.2f));
                }
                else if (i == reyaDialogue.Length-1)
                {
                    player.GetComponent<PlayerMovement>().enabled = true;
                    pauseGame = false;
                }
                yield return new WaitForSeconds(2);
            }
            dialogue.SetActive(false);
        }
        else
        {
            dialogue.SetActive(false);
            AudioManager.Instance.Play("Area 1");
            StartCoroutine(AudioManager.Instance.StartFade("Area 1", 0.5f, 0.2f));
            Fader.Instance.FadeOut(0.5f);
            yield return new WaitForSeconds(0.5f);
            player.GetComponent<PlayerMovement>().enabled = true;
            pauseGame = false;
        }
        
        //show area intro text
        float waitTime = 0.1f;
        foreach (char ch in "Abandoned Rooftop, Hightower District\n(Virtual Layer)")
        {
            if (ch == '(')
                yield return new WaitForSeconds(1);
            areaIntroText.text += ch;
            yield return new WaitForSeconds(waitTime);
            if (ch == 'p')
            {
                yield return new WaitForSeconds(0.3f);
                waitTime = 0.05f;
            }
        }
        yield return new WaitForSeconds(1);
        Color col = areaIntroText.color;
        for (float i = 1; i > 0; i -= 0.01f)
        {
            yield return new WaitForSeconds(0.01f);
            areaIntroText.color = new Color(col.r, col.g, col.b, i);
        }
        Destroy(areaIntroText.gameObject);
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            StartCoroutine(LoadNextRoom());
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            int killed = enemyParent.childCount;
            foreach (Transform child in enemyParent)
                Destroy(child.gameObject);
            UpdateEnemyNum(-killed);
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("Spawning more enemies!");
            if (staticSpawn)
                SetupEnemies(levelNum + Random.Range(1, 4));
            else
                SetupWaves(levelNum*2 + Random.Range(1, 4), true);
        }
    }


    private void SetupEnemies(int n)
    {
        foreach (Transform child in enemyParent)
        {
            Destroy(child.gameObject);
        }
        numEnemies = n;
        
        while (n > 0)
        {
            n -= RandomEnemies(n);
        }
    }

    private void SetupWaves(int n, bool skip=false)
    {
        int maxPerWave = Mathf.Max(2, (int)Mathf.Round(n*3/5));
        waves.Clear();
        foreach (Transform child in enemyParent)
        {
            Destroy(child.gameObject);
        }
        if (!skip)
        {
            numEnemies = RandomEnemies(n, maxPerWave);
            n -= numEnemies;
        }
        while (n > 0)
        {
            int numToAdd = Random.Range(2, Mathf.Min(n, maxPerWave)+1);
            if (n - numToAdd == 1)
                numToAdd--;
            waves.Add(numToAdd);
            n -= numToAdd;
        }
        if (skip)
        {
            UpdateEnemyNum(0);
        }
    }

    private int RandomEnemies(int n, int max=5)
    {
        int numToAdd = Random.Range(2, Mathf.Min(n, max)+1);
        if (n - numToAdd == 1)
            numToAdd--;
        int nodeNum = Random.Range(0, nodeParent.childCount);
        for (int i = 0; i < numToAdd; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-2, 2), 1, Random.Range(-2, 2));
            int attempts = 0;
            while (Physics.OverlapSphere(nodeParent.GetChild(nodeNum).position + offset, 0.5f, LayerMask.GetMask("Enemy")).Length > 0)
            {
                offset = new Vector3(Random.Range(-2, 2), 1, Random.Range(-2, 2));
                attempts++;
                if (attempts == 10) //fail to find open spot
                    break;
            }
            if (attempts < 10)
            {
                string name = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)] + "_" + enemyType;
                GameObject prefab = Resources.Load<GameObject>("Prefabs/Enemies/" + name);
                if (prefab != null)
                    Instantiate(prefab, nodeParent.GetChild(nodeNum).position + offset, Quaternion.identity, enemyParent);
            }
        }
        return numToAdd;
    }

    public IEnumerator WaveEnemies(int n, Vector3 setPos = default)
    {
        numEnemies += n;
        yield return new WaitForSeconds(1);
        for (int i = 0; i < n; i++)
        {
            string name = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)] + "_" + enemyType;
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Enemies/" + name);
            if (prefab != null)
            {
                int repeats = name.Contains("Fast") ? 2 : 1;
                numEnemies += repeats-1;
                for (int j = 0; j < repeats; j++)
                {
                    if (setPos != Vector3.zero)
                    {
                        GameObject enemy = Instantiate(prefab, setPos + new Vector3(0, 15, 0), Quaternion.identity, enemyParent);
                        enemy.GetComponent<Rigidbody>().velocity = new Vector3(0, -100, 0);
                    }
                    else
                    {
                        float minDist = 5;
                        float maxDist = 10;
                        Vector3 offset = new Vector3(Random.Range(-1, 1), 0, Random.Range(-1, 1)).normalized * Random.Range(5, 10) + new Vector3(0, 1, 0);
                        int attempts = 0;
                        while (Physics.OverlapSphere(player.position + offset, 0.5f).Length > 0)
                        {
                            offset = new Vector3(Random.Range(-1, 1), 0, Random.Range(-1, 1)).normalized * Random.Range(minDist, maxDist) + new Vector3(0, 1, 0);
                            attempts++;
                            if (attempts == 10) //fail to find open spot
                            {
                                minDist++;
                                maxDist++;
                                attempts = 0;
                                if (maxDist > 20)
                                {
                                    Debug.Log("NO OPEN SPOT :(");
                                    numEnemies--;
                                    break;
                                }
                            }
                        }
                        if (maxDist < 20)
                        {
                            GameObject enemy = Instantiate(prefab, player.position + offset + new Vector3(0, 15, 0), Quaternion.identity, enemyParent);
                            enemy.GetComponent<Rigidbody>().velocity = new Vector3(0, -100, 0);
                        }
                    }
                    yield return new WaitForSeconds(0.5f);
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
        if (numEnemies <= 0)
            UpdateEnemyNum(0);
    }

    public IEnumerator SpawnInfiniteWaves(bool wait=true)
    {
        int maxTerminals = numTerminals;
        yield return new WaitUntil(() => numTerminals != maxTerminals || !wait);
        string sceneName = SceneManager.GetActiveScene().name;
        while (sceneName == SceneManager.GetActiveScene().name)
        {
            StartCoroutine(WaveEnemies(1));
            yield return new WaitForSeconds(Random.Range(minSpawn, maxSpawn));
        }
    }


    public void UpdateEnemyNum(int n)
    {
        numEnemies += n;
        /*if (numEnemies <= 0 && !fullArea)
        {
            if (!staticSpawn && waves.Count > 0) //spawn more waves!
            {
                StartCoroutine(WaveEnemies(waves[0]));
                numEnemies = waves[0];
                waves.Remove(waves[0]);
            }
            else
               FinishLevel();
        }*/
    }

    private void FinishLevel()
    {
        float rot = Random.Range(0, 360);
        Vector3 rewardPos = player.position + Quaternion.Euler(0, rot, 0) * player.forward * 5;
        int attempts = 0;
        while (Physics.OverlapSphere(rewardPos, 1, terrainLayer).Length > 0)
        {
            rot = Random.Range(0, 360);
            rewardPos = player.position + Quaternion.Euler(0, rot, 0) * player.forward * 5;
            attempts++;
            if (attempts >= 40) //quit out after some max # of attempts
            {
                Debug.LogError("No valid location!");
                break;
            }
        } 
        GameObject reward = Instantiate(rewardPrefab, rewardPos + new Vector3(0, 20, 0), Quaternion.identity);
        reward.GetComponent<Rigidbody>().velocity = new Vector3(0, -100, 0);
        reward.GetComponent<Reward>().numOptions = 3;
    }


    public IEnumerator UseTerminal()
    {
        playerPaused = true;
        bar = Instantiate(terminalBar, player.transform.position + new Vector3(0, 1.3f, 0), Quaternion.identity).transform.GetChild(1).GetComponent<Image>();
        AudioManager.Instance.Play("Terminal Charge");
        float elapsed = 0;
        while (elapsed < 4)
        {
            if (bar == null)
                yield break;
            bar.fillAmount = elapsed/4f;
            yield return null;
            elapsed += Time.deltaTime;
        }
        currentTerminal.complete = true;
        Transform iconToChange = terminalIcons.GetChild(terminalIcons.childCount - numTerminals);
        iconToChange.GetComponent<CanvasGroup>().alpha = 0.5f;
        iconToChange.GetChild(0).gameObject.SetActive(true);
        Destroy(bar.transform.parent.gameObject);
        playerPaused = false;
        AudioManager.Instance.Play("Terminal Activate");
        AudioManager.Instance.Stop("Terminal Charge");
        StartCoroutine(PlayMultipleDialogues(currentTerminal.dialogue));
        numTerminals--;
        
        //disable barrier &/or show hidden room
        if (currentTerminal.barrier != null)
            UnlockBarrier(currentTerminal.barrier);
        if (currentTerminal.hiddenRoom != null)
            foreach (GameObject g in currentTerminal.hiddenRoom)
                g.SetActive(!g.activeSelf);
    }

    public IEnumerator FirstAccessPt()
    {
        playerPaused = true;
        ProgramManager.Instance.buildSelect.SetActive(true);
        ProgramManager.Instance.programUI.gameObject.SetActive(true);
        yield return new WaitUntil(() => !playerPaused);
        UnlockBarrier(GameObject.Find("Barrier").transform);
        Transform iconToChange = terminalIcons.GetChild(terminalIcons.childCount - numTerminals);
        iconToChange.GetComponent<CanvasGroup>().alpha = 0.5f;
        iconToChange.GetChild(0).gameObject.SetActive(true);
        
        yield return new WaitForSeconds(2);
        StartCoroutine(WaveEnemies(1, new Vector3(31, 0, -5)));
        yield return new WaitForSeconds(1.2f);
        StartCoroutine(PlayMultipleDialogues(new string[]{"Shit... they found us already.", "~oh my it appears we may be unwelcome here"}));
        player.GetComponent<PlayerMovement>().hpBar.gameObject.SetActive(true);
    }

    private void UnlockBarrier(Transform barrier)
    {
        int numLocks = 0;
        foreach (Transform child in barrier.GetChild(0))
        {
            if (child.GetComponent<MeshRenderer>().material.name.Contains("Red"))
                numLocks++;
        }
        barrier.GetChild(0).GetChild(barrier.GetChild(0).childCount - numLocks).GetComponent<MeshRenderer>().material = barrierUnlockBlue;

        if (numLocks <= 1)
        {
            barrier.GetChild(0).gameObject.SetActive(false);
            barrier.GetChild(1).gameObject.SetActive(false);
            barrier.GetChild(2).GetComponent<MeshRenderer>().material = barrierGreen;
            barrier.GetChild(3).GetComponent<MeshRenderer>().material = barrierGreen;
            TextMeshProUGUI txt = barrier.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>();
            txt.text = "Welcome, AUTH_USER!";
            txt.color = unlockTextColor;
        }
    }


    public IEnumerator PlayMultipleDialogues(string[] lines)
    {
        foreach (string s in lines)
        {
            yield return PlayDialogue(s, 1f); 
        }
    }

    public IEnumerator PlayDialogue(string line, float waitTime=3f)
    {
        portraits[0].SetActive(line[0] != '~');
        portraits[1].SetActive(line[0] == '~');
        TextMeshProUGUI txt = dialogue.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        txt.text = "";
        dialogue.SetActive(true);
        foreach (char c in line)
        {
            if (c=='*')
                yield return new WaitForSeconds(0.15f);
            else if (c != '~')
            {
                txt.text += c;
                if (c=='.' || c==',')
                    yield return new WaitForSeconds(0.15f);
                else if (c==' ')
                    yield return new WaitForSeconds(0.08f);
                else
                    yield return new WaitForSeconds(0.04f);
            }
        }
        if (line[line.Length-1] == '—')
            waitTime *= 0.5f;
        yield return new WaitForSeconds(waitTime);
        dialogue.SetActive(false);
        txt.text = "";
    }


    public IEnumerator LoadNextLevel(string nextArea)
    {
        AudioManager.Instance.Play("Elevator Down");
        foreach (Transform child in enemyParent)
            Destroy(child.gameObject);
        yield return new WaitForSeconds(0.5f);
        Fader.Instance.FadeIn(1.2f, true);
        yield return new WaitForSeconds(1.2f);
        yield return new WaitForSeconds(1.5f);
        loadingText.GetComponent<TextMeshProUGUI>().text = "Now approaching: \n" + nextArea;
        loadingText.SetActive(true);
        Color c = loadingText.GetComponent<TextMeshProUGUI>().color;
        loadingText.GetComponent<TextMeshProUGUI>().color = new Color(c.r, c.g, c.b, 1);
        yield return new WaitForSeconds(2f);

        float elapsed = 1;
        StartCoroutine(ElevatorSounds());
        while (elapsed > 0)
        {
            elapsed -= Time.deltaTime;
            yield return null;
            loadingText.GetComponent<TextMeshProUGUI>().color = new Color(c.r, c.g, c.b, elapsed);
        }
        loadingText.SetActive(false);
        int levelNum = int.Parse(SceneManager.GetActiveScene().name.Substring(6))+1;
        //string areaStr = (levelNum < 10) ? "0" + levelNum : "" + levelNum;
        //areaText.text = "Area_" + areaStr;
        areaText.text = nextArea;
        SceneManager.LoadScene("Level " + levelNum);
    }

    private IEnumerator ElevatorSounds()
    {
        yield return new WaitForSeconds(0.3f);
        StartCoroutine(AudioManager.Instance.StartFade("Elevator Down", 0, 0.5f));
        yield return new WaitForSeconds(0.5f);
        AudioManager.Instance.Play("Elevator Stop");
        AudioManager.Instance.Stop("Elevator Down");
    }

    public IEnumerator LoadNextRoom()
    {
        Fader.Instance.FadeIn(1);
        yield return new WaitForSeconds(1);
        levelNum++;
        string areaStr = (levelNum < 10) ? "0" + levelNum : "" + levelNum;
        areaText.text = "Area_" + areaStr;
        if (levelNum == bossRooms[bossIndex])
        {
            SceneManager.LoadScene("Boss " + (bossIndex+1));
            if (bossIndex < bossRooms.Length-1)
                bossIndex++;
            bossTxt.SetActive(true);
        }
        else
        {
            float totalWeight = 0;
            foreach (Room r in rooms)
            {
                if (!r.active)
                    totalWeight += r.weight;
            }
            float rand = Random.Range(0f, totalWeight);
            Room chosen = null;
            foreach (Room r in rooms)
            {
                if (!r.active)
                {
                    rand -= r.weight;
                    if (rand < 0 && chosen == null)
                    {
                        chosen = r;
                    }
                    else
                    {
                        r.weight += 1;
                    }
                }
                r.active = false;
            }
            if (chosen == null)
            {
                Debug.LogError("Could not find a scene to load!");
            }
            else
            {
                SceneManager.LoadScene(chosen.name);
                chosen.active = true;
                chosen.weight *= 0.5f;
            }
        }
    }
}


[System.Serializable]
public class Room
{
    public string name;
    public bool active;
    public float weight;
    //tags like encounter type, etc.
}