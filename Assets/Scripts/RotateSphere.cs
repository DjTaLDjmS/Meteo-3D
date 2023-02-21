using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RotateSphere : MonoBehaviour
{
    [SerializeField] float rotateSpeed = 20f;
    Vector2 rotate;

    void OnRotate(InputValue value)
    {
        rotate = value.Get<Vector2>();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(-rotate.y * rotateSpeed * Time.deltaTime, rotate.x * rotateSpeed * Time.deltaTime, 0f, Space.World);
    }
}
