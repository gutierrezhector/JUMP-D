﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    private Rigidbody rb;
    private Vector3 playerDefaultPos;

	void Start ()
    {
        rb = GetComponent<Rigidbody>();
        playerDefaultPos = transform.position;
        Pause = false;
    }
	
	void Update () // -9.81 real grav
    {
        if (GameManager.Instance.GameStarted)
        {
            if (!Pause)
            {
                if (Input.GetButtonDown("Jump"))
                {
                    rb.AddForce(Vector3.up * 8, ForceMode.Impulse);
                }
            }
        }
	}

    public void SetToStartPos()
    {
        transform.position = playerDefaultPos;
    }

    public bool Pause { get; set; }
}