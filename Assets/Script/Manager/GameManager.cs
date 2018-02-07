﻿using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    [SerializeField]
    private Transform positionDeathPlayer;

    [SerializeField]
    private Transform positionFirstPlatform;

    [SerializeField]
    private PlayerController playerInstance;

    [SerializeField]
    private float speedPlatformStart;

    [SerializeField]
    private float minDistanceBetweenPlatform;

    [SerializeField]
    private float maxDistanceBetweenPlatform;

    [SerializeField]
    private int flagPlatformStart;

    [SerializeField]
    private int stepPlatform;

    [SerializeField]
    private GameObject platformDefault;

    [SerializeField]
    private GameObject platformEnvironment;

    [SerializeField]
    private InterfaceManager interfaceManagerInstance;

    [SerializeField]
    private PlatformManager platformManagerInstance;

    [SerializeField]
    private AudioClip startGameSound;

    [SerializeField]
    private AudioClip deathSound;

    [SerializeField]
    private AudioClip stepSound;

    private float ratePlatform;
    private AudioSource audioSource;
    private float lastInstanceTime;
    private float startTime;
    private float initTime;

    private float speedPlatform;
    private float currentSpeedPlatform;
    private int currentFlagPlatform;
    private int flagPlatform;
    private int currentStepPlatform;
    private float distanceBetweenPlatform;
    private float currentMinDistanceBetweenPlatform;
    private float currentMaxDistanceBetweenPlatform;
    private bool haveToRestartAfterBackground;

    private bool isPlayerDead;
    private bool isPaused;

    private Coroutine restartAfterBackground;

    private static GameManager instance;

    private PathPlatform[] pathPlatform;

    public delegate void GameAction();
    public static event GameAction OnDeath;

    public delegate void PathAction(GameObject obj, e_posPlatform pos);
    public static event PathAction OnPath;

    public enum e_dirRotation { LEFT, RIGHT };
    public enum e_posPlatform { BOT, BOTRIGHT, RIGHT, TOPRIGHT, TOP, TOPLEFT, LEFT, BOTLEFT };

    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Debug.LogWarning("Singleton " + this.name + " : instance here already");
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        pathPlatform = new PathPlatform[3];
    }

    public static GameManager Instance
    {
        get { return instance; }
    }

    void Start()
    {
        GameStarted = false;
        isPaused = false;
        isPlayerDead = false;

        PlayerController.OnJump += PlayerJumped;
        PlayerController.OnLand += PlayerLanded;

        pathPlatform[0] = new PathPlatform(0, true, false);
        pathPlatform[1] = new PathPlatform(1);
        pathPlatform[2] = new PathPlatform(2);
        InitGame();
    }


    void Update()
    {
        if (GameStarted)
        {
            if (!Pause)
            {
                if (VerifPlayerDeath())
                    return;

                if (lastInstanceTime + ratePlatform < MyTimer.Instance.TotalTimeSecond)
                {
                    float scaleRandom = Random.Range(4, 8);
                    for (int count = 0; count < 3; count++)
                    {
                        if (pathPlatform[count].used && !pathPlatform[count].justCreated)
                        {
                            GameObject obj = platformManagerInstance.CreatePlatform(pathPlatform[count].nextPosPlatform, currentSpeedPlatform, Vector3.zero, scaleRandom);

                            if (count == 0)
                                if (OnPath != null)
                                    OnPath(obj, pathPlatform[count].nextPosPlatform);

                            pathPlatform[count].lastPosPlatform = pathPlatform[count].currentPosPlatform;
                            pathPlatform[count].currentPosPlatform = pathPlatform[count].nextPosPlatform;

                            if (MyRandom.ThrowOfDice(30))
                            {
                                AddPath(count);
                            }
                        }
                    }
                    
                    for (int count = 0; count < 3; count++)
                    {
                        if (pathPlatform[count].used == true)
                        {
                            GetNewPosPlatform(count);
                            if (pathPlatform[count].justCreated)
                            {
                                pathPlatform[count].justCreated = false;
                            }
                        }
                    }
                    
                    VerifIfSimilarPath();
                    distanceBetweenPlatform = Random.Range(currentMinDistanceBetweenPlatform, currentMaxDistanceBetweenPlatform);
                    ratePlatform = (distanceBetweenPlatform + scaleRandom) / currentSpeedPlatform;
                    lastInstanceTime = MyTimer.Instance.TotalTimeSecond;
                }
            }
        }
        else
        {
            ManageInput();
        }
    }

    void OnApplicationFocus(bool pauseStatus)
    {
        if (pauseStatus) // regain focus
        {
            if (GameStarted)
            {
                if (haveToRestartAfterBackground)
                {
                    interfaceManagerInstance.ShowRestartBackground(true);
                    restartAfterBackground = StartCoroutine(RestartAfterbackground());
                }
            }
        }
        else // losing focus
        {
            if (haveToRestartAfterBackground)
                StopCoroutine(restartAfterBackground);
            if (!Pause && GameStarted)
            {
                interfaceManagerInstance.ShowIngameUI(false);
                Pause = true;
                haveToRestartAfterBackground = true;
            }
        }
    }

    void ManageInput()
    {
#if UNITY_IOS
 if (Input.touchCount > 0)
        {
            if (Input.GetTouch(0).phase == TouchPhase.Began)
            {
               ManageGame();
            }
        }
#endif

#if UNITY_ANDROID

        if (Input.touchCount > 0)
        {
            if (Input.GetTouch(0).phase == TouchPhase.Began)
            {
                ManageGame();
            }
        }
#endif
#if UNITY_EDITOR
        if (Input.GetKeyDown("a"))
        {
            ManageGame();
        }
#endif
    }

    void ManageGame()
    {
        if (isPlayerDead)
        {
            if (initTime + 1f < MyTimer.Instance.TotalTimeSecond)
            {
                isPlayerDead = false;
                platformEnvironment.transform.eulerAngles = Vector3.zero;
                playerInstance.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
                interfaceManagerInstance.ShowMenu(true);
                InitGame();
            }
        }
        else
        {
            if (initTime + 1f < MyTimer.Instance.TotalTimeSecond)
                StartGame();
        }
    }

    void InitGame()
    {
        Debug.Log("InitGame");
        playerInstance.InitPlayer();
        interfaceManagerInstance.InitInterface();

        ratePlatform = 0;
        initTime = MyTimer.Instance.TotalTimeSecond;
        ScorePoint = 0;
        currentFlagPlatform = 0;
        currentStepPlatform = 0;
        flagPlatform = flagPlatformStart;

        platformManagerInstance.InitPlatform();
        platformManagerInstance.ClearInstancesPlatform();

        pathPlatform[0].currentPosPlatform = e_posPlatform.BOT;
        pathPlatform[0].lastPosPlatform = e_posPlatform.BOT;
        pathPlatform[1].used = false;
        pathPlatform[2].used = false;
        pathPlatform[1].justCreated = false;
        pathPlatform[2].justCreated = false;

        currentMinDistanceBetweenPlatform = minDistanceBetweenPlatform;
        currentMaxDistanceBetweenPlatform = maxDistanceBetweenPlatform;

        UpdatecurrentSpeedPlatform(speedPlatformStart);

        InitTerrain();
        platformManagerInstance.CurrentPlatformPlayer = platformManagerInstance.InstancesPlatform[0];
    }

    void InitTerrain()
    {
        Debug.Log("InitTerrain");
        float offset = -45;
        Vector3 posPlatform = Vector3.forward * offset;

        posPlatform += Vector3.up * 70;
        platformManagerInstance.CreatePlatform(pathPlatform[0].currentPosPlatform, currentSpeedPlatform, posPlatform, 6, false, false);
        GetNewPosPlatform(0);

        distanceBetweenPlatform = Random.Range(currentMinDistanceBetweenPlatform, currentMaxDistanceBetweenPlatform);
        offset += (6 + distanceBetweenPlatform);
        posPlatform = Vector3.forward * offset;
        float scaleRandom;
        while (offset <= 0)
        {
            scaleRandom = Random.Range(4, 8);
            
            for (int count = 0; count < 3; count++)
            {
                if (pathPlatform[count].used && !pathPlatform[count].justCreated)
                {
                    GameObject obj = platformManagerInstance.CreatePlatform(pathPlatform[count].nextPosPlatform, currentSpeedPlatform, posPlatform, scaleRandom);

                    if (count == 0)
                        if (OnPath != null)
                            OnPath(obj, pathPlatform[count].nextPosPlatform);

                    pathPlatform[count].lastPosPlatform = pathPlatform[count].currentPosPlatform;
                    pathPlatform[count].currentPosPlatform = pathPlatform[count].nextPosPlatform;

                    if (MyRandom.ThrowOfDice(30))
                        AddPath(count);
                }
            }
            
            for (int count = 0; count < 3; count++)
            {
                if (pathPlatform[count].used == true)
                {
                    GetNewPosPlatform(count);
                    if (pathPlatform[count].justCreated)
                    {
                        pathPlatform[count].justCreated = false;
                    }
                }
            }
            
            VerifIfSimilarPath();
            lastInstanceTime = (distanceBetweenPlatform + scaleRandom + offset) / currentSpeedPlatform;
            offset += (scaleRandom + distanceBetweenPlatform);
            posPlatform = Vector3.forward * offset;
        }
    }


    bool VerifPlayerDeath()
    {
        if (playerInstance.transform.position.y < 0.6f)
        {
            if (OnDeath != null)
                OnDeath();
            Debug.Log("Dead");

            if (PlayerPrefs.GetInt("bestScore") < ScorePoint)
                PlayerPrefs.SetInt("bestScore", ScorePoint);

            GameStarted = false;
            interfaceManagerInstance.ShowIngameUI(false);
            interfaceManagerInstance.UpdateStats(ScorePoint);
            interfaceManagerInstance.ShowStats(true);
            isPlayerDead = true;
            initTime = MyTimer.Instance.TotalTimeSecond;
            audioSource.PlayOneShot(deathSound);
            playerInstance.StopVelocity();
            return true;
        }

        return false;
    }

    void RotatePlatformEnvironment(e_dirRotation dir)
    {
        StartCoroutine(DoRotatePlatformEnvironment(dir));
    }

    void PlayerLanded(PlayerController.e_jump jump)
    {
        if (currentStepPlatform < stepPlatform && currentFlagPlatform >= flagPlatform)
        {
            UpdatecurrentSpeedPlatform(currentSpeedPlatform + 0.8f);
            platformManagerInstance.UpdateColorPlatform();
            currentStepPlatform++;
            flagPlatform += 10;
            currentFlagPlatform = 0;
            audioSource.PlayOneShot(stepSound);
            currentMinDistanceBetweenPlatform += 0.1f;
            currentMaxDistanceBetweenPlatform += 0.3f;
        }

        currentFlagPlatform++;
    }

    void PlayerJumped(PlayerController.e_jump jump)
    {
        if (jump == PlayerController.e_jump.LEFT)
        {
            RotatePlatformEnvironment(e_dirRotation.LEFT);
        }
        else
        {
            RotatePlatformEnvironment(e_dirRotation.RIGHT);
        }
    }

    void GetNewPosPlatform(int pathID)
    {
        if (MyRandom.ThrowOfDice(50))
            pathPlatform[pathID].nextPosPlatform = pathPlatform[pathID].currentPosPlatform == e_posPlatform.BOTLEFT ? e_posPlatform.BOT : pathPlatform[pathID].currentPosPlatform + 1;
        else
            pathPlatform[pathID].nextPosPlatform = pathPlatform[pathID].currentPosPlatform == e_posPlatform.BOT ? e_posPlatform.BOTLEFT : pathPlatform[pathID].currentPosPlatform - 1;
    }

    void VerifIfSimilarPath()
    {
        for (int countPath = 2; countPath >= 0; countPath--)
        {
            for (int countComparePath = 2; countComparePath >= 0; countComparePath--)
            {
                if (countPath != countComparePath
                    && pathPlatform[countPath].used
                    && pathPlatform[countComparePath].used
                    && pathPlatform[countPath].nextPosPlatform == pathPlatform[countComparePath].nextPosPlatform)
                {
                    pathPlatform[countPath].used = false;
                }
            }
        }
    }

    void AddPath(int pathId)
    {
        for (int count = 1; count < 3; count++)
        {
            if (pathPlatform[count].used == false)
            {
                pathPlatform[count].used = true;
                pathPlatform[count].justCreated = true;
                pathPlatform[count].currentPosPlatform = pathPlatform[pathId].currentPosPlatform;
                pathPlatform[count].lastPosPlatform = pathPlatform[pathId].lastPosPlatform;
                return;
            }
        }
    }

    void UpdatecurrentSpeedPlatform(float addedSpeed)
    {
        currentSpeedPlatform = addedSpeed;
        speedPlatform = currentSpeedPlatform;
        platformManagerInstance.UpdatecurrentSpeedPlatform(currentSpeedPlatform);
    }

    public void StartGame()
    {
        Debug.Log("StartGame");
        startTime = MyTimer.Instance.TotalTimeSecond;
        lastInstanceTime += MyTimer.Instance.TotalTimeSecond;
        interfaceManagerInstance.ShowMenu(false);
        interfaceManagerInstance.ShowIngameUI(true);
        GameStarted = true;
        audioSource.PlayOneShot(startGameSound);
    }

    public void DestroyPlatform(GameObject thisPlatform)
    {
        platformManagerInstance.DestroyPlatform(thisPlatform);
    }

    public void AddScore(int point)
    {
        ScorePoint += point;
        interfaceManagerInstance.UpdateScore(ScorePoint);
    }

    public void DoPause()
    {
        if (!Pause)
            Pause = true;
        else
            Pause = false;
    }

    public bool Pause
    {
        get
        {
            return isPaused;
        }
        set
        {
            isPaused = value;
            interfaceManagerInstance.Pause = value;
            playerInstance.Pause = value;
            platformManagerInstance.Pause = value;
            MyTimer.Instance.Pause = value;
        }
    }

    public bool GameStarted { get; set; }
    public int ScorePoint { get; set; }
    public float ScoreTime
    {
        get
        {
            return MyTimer.Instance.TotalTimeSecond - startTime;
        }

    }

    public PlatformManager PlatformManagerInstance
    {
        get
        {
            return platformManagerInstance;
        }
    }

    public InterfaceManager InterfaceManagerInstance
    {
        get
        {
            return interfaceManagerInstance;
        }
    }

    IEnumerator RestartAfterbackground()
    {
        int count = 3;

        while (count != 0)
        {
            interfaceManagerInstance.UpdateRestartBackground(count.ToString());
            count--;
            yield return new WaitForSeconds(1);
        }

        interfaceManagerInstance.UpdateRestartBackground("GO !");
        yield return new WaitForSeconds(0.2f);
        interfaceManagerInstance.ShowRestartBackground(false);
        interfaceManagerInstance.ShowIngameUI(true);
        Pause = false;
        haveToRestartAfterBackground = false;

        float start = 0;
        float end = speedPlatform;

        float elapsedTime = 0.0f;

        while (elapsedTime < 1.0f)
        {
            if (!Pause)
            {
                UpdatecurrentSpeedPlatform(Mathf.Lerp(start, end, elapsedTime));
                elapsedTime += Time.deltaTime * 2;
            }

            yield return null;
        }
    }

    IEnumerator DoRotatePlatformEnvironment(e_dirRotation dir)
    {
        float angle = 0.0f;
        float degree;
        Vector3 saveRotation = platformEnvironment.transform.eulerAngles;

        while (angle < 45.0f)
        {
            if (!Pause)
            {
                degree = Time.deltaTime * 250;

                if (dir == e_dirRotation.RIGHT)
                {
                    platformEnvironment.transform.Rotate(Vector3.back * degree);
                }
                else
                {
                    platformEnvironment.transform.Rotate(Vector3.forward * degree);

                }

                if (angle + degree > 45.0f)
                {
                    if (dir == e_dirRotation.RIGHT)
                    {
                        platformEnvironment.transform.eulerAngles = saveRotation + (Vector3.back * 45);
                    }
                    else
                    {
                        platformEnvironment.transform.eulerAngles = saveRotation + (Vector3.forward * 45);
                    }
                }

                angle += degree;
            }

            yield return null;
        }
    }
}