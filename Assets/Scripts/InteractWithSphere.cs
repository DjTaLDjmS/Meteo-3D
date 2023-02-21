using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class InteractWithSphere : MonoBehaviour
{
    Vector3 clickPos;
    bool clicked;

    Vector2 longLat;

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
            Debug.Log(" Longitude : " + longLat.x + " Latitude : " + longLat.y);
        }

        gameObject.GetComponent<SearchProvider>().GetMeteoCoordinates(longLat.x, longLat.y);
    }

    public Vector2 GetLocation()
    {
        return longLat;
    }

    public Vector2 ToSpherical(Vector3 position)
    {
        // Convert to a unit vector so our y coordinate is in the range -1...1.
        position.Normalize(); // = Normalize(position);

        // The vertical coordinate (y) varies as the sine of latitude, not the cosine.
        float lat = Mathf.Asin(position.y) * Mathf.Rad2Deg;

        // Use the 2-argument arctangent, which will correctly handle all four quadrants.
        float lon = 90 - Mathf.Atan2(position.x, position.z) * Mathf.Rad2Deg;
        
        if (lon > 180f)
        {
            lon -= 360f;
        }
        // Here I'm assuming (0, 0, 1) = 0 degrees longitude, and (1, 0, 0) = +90.
        // You can exchange/negate the components to get a different longitude convention.

        //Debug.Log(lat + ", " + lon);

        // I usually put longitude first because I associate vector.x with "horizontal."
        return new Vector2(lon, lat);
    }

    void Update()
    {

    }
}
