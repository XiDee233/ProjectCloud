using PurrNet.Transports;
using PurrNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class TestPlayer : NetworkBehaviour
{
    [SerializeField] private float moveForce = 10f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float bounceForce = 10f;
    [SerializeField] private Rigidbody rigidbody;
    private bool _willJump;

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        if (asServer)
            return;

        //All clients set it to kinematic, so only the server runs physics!
        rigidbody.isKinematic = !isServer;
        //Only the owner has it enabled, as to run Update()
        enabled = isOwner;

        //Only the owner runs OnTick to send input to the server
        if (isOwner)
            networkManager.onTick += OnTick;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        //Unsubcribing again for cleanup
        networkManager.onTick -= OnTick;
    }

    private void Update()
    {
        //We have to store the input to be used during the next tick
        if (Input.GetKeyDown(KeyCode.Space))
            _willJump = true;
    }

    private void OnTick(bool asServer)
    {
        //In case of a host setup, we don't want this to run twice.
        if (asServer)
            return;

        //We generate the input struct that will be sent to the server
        var input = new InputData()
        {
            movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")),
            jump = _willJump
        };

        //Restting the jump bool back after we've now used it in a tick
        _willJump = false;

        //We send the input to the server
        Move(input);
    }

    //Server RPC with Unreliable channel to send the data more efficiently
    [ServerRpc(Channel.Unreliable)]
    private void Move(InputData inputData)
    {
        //This is where you can also handle cheat detection on the inputData
        //You could for example normalize it, if the magnitude is above 1
        //From here the code is basically "single-player" code from the 
        //perspective of the server

        //We generate the movement vector from the given input.
        var movement = new Vector3(inputData.movement.x, 0, inputData.movement.y) * moveForce;

        rigidbody.AddForce(movement);

        if (inputData.jump)
            rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision other)
    {
        //Other than the if-statement here, this is single-player code from the
        //perspective of the server
        if (!isServer)
            return;

        if (!other.gameObject.TryGetComponent(out TestPlayer otherPlayer))
            return;

        var direction = (transform.position - other.transform.position).normalized;
        rigidbody.AddForce(direction * bounceForce, ForceMode.Impulse);
    }

    //Struct in which we hold input data. This isn't necessary, just a clean approach
    private struct InputData
    {
        public Vector2 movement;
        public bool jump;
    }

    
}
