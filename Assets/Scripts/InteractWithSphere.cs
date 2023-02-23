using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class InteractWithSphere : MonoBehaviour
{
    Vector3 clickPos;
    bool clicked;
    public Vector2 longLat;

    void OnMouseDown()
    {
        clicked = Input.GetMouseButtonDown(0);
        clickPos = Input.mousePosition;

        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(clickPos);
        if (Physics.Raycast(ray, out hit) && clicked)
        {
            Vector3 offset = hit.collider.transform.InverseTransformPoint(hit.point);
            longLat = ToSpherical(offset);
            Debug.Log(" Latitude : " + longLat.y + " Longitude : " + longLat.x);
        }
    }

    Vector2 ToSpherical(Vector3 position)
    {
        position.Normalize();
        float lat = Mathf.Asin(position.y) * Mathf.Rad2Deg;
        float lon = 90 - Mathf.Atan2(position.x, position.z) * Mathf.Rad2Deg;
        if(lon > 180f)
        {
            lon -= 360f;
        }
        return new Vector2(lon, lat);
    }
}
