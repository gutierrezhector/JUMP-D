﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

    [SerializeField]
    private Transform[] positionPlatformStart;

    [SerializeField]
    private Transform[] positionPlatformCreate;

    [SerializeField]
    private Transform positionPlatformEnd;

    [SerializeField]
    private PlayerController playerInstance;

    [SerializeField]
    private float speedPlatform;

    [SerializeField]
    private float distanceBetweenPlatform;

    [SerializeField]
    private GameObject platformDefault;

    private int score;
    private float ratePlatform;

    private float lastInstanceTime;
    private e_posPlatform currentPosPlatform;

    private static GameManager instance;

    public enum e_posPlatform { BOT, BOTRIGHT, RIGHT, TOPRIGHT, TOP, TOPLEFT, LEFT, BOTLEFT };

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Debug.LogWarning("Singleton " + this.name + " : instance here already");
            Destroy(gameObject);
            return;
        }
    }

    public static GameManager Instance
    {
        get { return instance; }
    }

    void Start ()
    {
        GameStarted = true;
        InitGame();
    }
	
	void Update ()
    {
        if (GameStarted)
        {
            if (!Pause)
            {
                if (lastInstanceTime + ratePlatform < MyTimer.Instance.TotalTime)
                {
                    Debug.Log("INSTANCE time: " + MyTimer.Instance.TotalTime);
                    lastInstanceTime = MyTimer.Instance.TotalTime;
                    GameObject currentInsance = Instantiate(platformDefault, positionPlatformStart[(int)currentPosPlatform]);
                    currentInsance.GetComponent<Platform>().Speed = speedPlatform;
                    currentInsance.GetComponent<Platform>().PosPlatform = currentPosPlatform;
                    GetNewPosPlatform();
                }
            }
        }
	}

    void InitGame()
    {
        playerInstance.SetToStartPos();
        lastInstanceTime = -100;
        ratePlatform = distanceBetweenPlatform / speedPlatform;
        currentPosPlatform = e_posPlatform.BOT;
    }

    void RestartGame()
    {

    }

    void GetNewPosPlatform()
    {
        if (MyRandom.ThrowOfDice(50))
            currentPosPlatform = currentPosPlatform == e_posPlatform.BOTLEFT ? e_posPlatform.BOT : currentPosPlatform + 1;
        else
            currentPosPlatform = currentPosPlatform == e_posPlatform.BOT ? e_posPlatform.BOTLEFT : currentPosPlatform - 1;
    }

    public bool Pause { get; set; }
    public bool GameStarted { get; set; }
}