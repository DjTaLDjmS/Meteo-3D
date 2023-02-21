using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using TMPro;
using UnityEngine.Networking;
using Unity.VisualScripting;
using static SearchProvider;
using static UnityEngine.Rendering.DebugUI;

public class SearchProvider : MonoBehaviour
{
    private List<City> cityObjectList;

    private List<string> cityList;

    private string apiKey;

    [SerializeField]
    private TMP_Dropdown CitySelect;

    [SerializeField]
    private TMP_InputField InputField;

    [SerializeField]
    private TMP_Text OutputField;

    [SerializeField]
    private Toggle ForecastMeteoToggle;

    Vector2 longLat;

    private enum MeteoType
    {
        Actual,
        Forecast
    }

    // Start is called before the first frame update
    void Start()
    {
        string cityDatas = File.ReadAllText(@"Assets\Data\city.list.json");
        cityObjectList = JsonConvert.DeserializeObject<List<City>>(cityDatas);
        cityList = cityObjectList.Select(city => char.ToUpper(city.name[0]) + city.name.Substring(1)).ToList();
        apiKey = File.ReadAllText(@"Assets\Scripts\apiKey.txt");
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    public void ReadStringInput(string textField)
    {
        if (textField.Length >= 3)
        {
            List<string> cities = cityList.Where(city => city.ToLower().StartsWith(textField.ToLower())).ToList();

            if (cities != null)
            {
                if (cities.Count >= 1)
                {
                    CitySelect.options.Clear();
                    CitySelect.value = -1;

                    //Display DropDownList
                    foreach (var city in cities)
                    {
                        CitySelect.options.Add(new TMP_Dropdown.OptionData() { text = city });
                    }

                    if (cities.Count <= 20)
                    {
                        CitySelect.Hide();
                        CitySelect.Show();
                    }
                    else
                    {
                        CitySelect.Hide();
                    }
                }
            }
            else
            {
                CitySelect.options.Clear();
                CitySelect.Hide();
            }


            InputField.Select();
            InputField.caretPosition = InputField.text.Length;
        }
        else
        {
            CitySelect.options.Clear();
        }
    }

    public void GetMeteoCityName()
    {
        string city = CitySelect.options[CitySelect.value].text;

        if (!ForecastMeteoToggle.isOn)
        {
            string queryActualMeteo = "https://api.openweathermap.org/data/2.5/weather?q=" + city +
                "&units=metric&lang=fr&appid=" + apiKey;

            StartCoroutine(GetMeteoRequest(queryActualMeteo, MeteoType.Actual));
        }
        else
        {
            string queryForecastMeteo = "https://api.openweathermap.org/data/2.5/forecast?q=" + city +
                    "&units=metric&lang=fr&appid=" + apiKey;

            StartCoroutine(GetMeteoRequest(queryForecastMeteo, MeteoType.Forecast));
        }
    }

    public void GetMeteoCoordinates(float latitude, float longitude)
    {
        if (!ForecastMeteoToggle.isOn)
        {
            string queryActualMeteo = "https://api.openweathermap.org/data/2.5/weather?lat=" + latitude + "&lon=" + longitude +
            "&units=metric&lang=fr&appid=" + apiKey;

            StartCoroutine(GetMeteoRequest(queryActualMeteo, MeteoType.Actual));
        }
        else
        {
            string queryForecastMeteo = "https://api.openweathermap.org/data/2.5/forecast?lat=" + latitude + "&lon=" + longitude +
                "&units=metric&lang=fr&appid=" + apiKey;

            StartCoroutine(GetMeteoRequest(queryForecastMeteo, MeteoType.Forecast));
        }
    }

    IEnumerator GetMeteoRequest(string uri, MeteoType meteoType)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    //Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);

                    switch (meteoType)
                    {
                        case MeteoType.Actual:
                            Meteo actualMeteoData = JsonConvert.DeserializeObject<Meteo>(webRequest.downloadHandler.text);
                            DisplayActualMeteo(actualMeteoData);
                            break;
                        case MeteoType.Forecast:
                            MeteoList forecastMeteoData = JsonConvert.DeserializeObject<MeteoList>(webRequest.downloadHandler.text);
                            DisplayForecastMeteo(forecastMeteoData);
                            break;
                        default:
                            break;
                    }
                    break;
            }
        }
    }

    private void DisplayActualMeteo(Meteo meteoData)
    {
        string textTemperature = "Température : " + meteoData.main.temp.ToString() + "°C";
        string textDescription = "Temps : " + meteoData.weather.ElementAt(0).description;
        double wind = meteoData.wind.speed * 3.6;
        string textWindSpeed = "Vent : " + wind.ToString() + "km/h";
        OutputField.text = textTemperature + "\n" + textDescription + "\n" + textWindSpeed;
    }

    private void DisplayForecastMeteo(MeteoList meteoData)
    {
        List<Meteo> meteoByDay = meteoData.list.Where(meteo => meteo.dateMeteo.Hour == 12).ToList();
        OutputField.text = String.Empty;
        foreach (var meteo in meteoByDay)
        {
            string textDayMeteo = meteo.dateMeteo.ToLongDateString();
            string textTemperature = "Température : " + meteo.main.temp.ToString() + "°C";
            string textDescription = "Temps : " + meteo.weather.ElementAt(0).description;
            OutputField.text += textDayMeteo + "\n" + textTemperature + "\n" + textDescription + "\n\n";
        }
    }

    public class City
    {
        public string name { get; set; }
        public string country { get; set; }
        public Coordinates coord { get; set; }
        public class Coordinates
        {
            public double lon { get; set; }
            public double lat { get; set; }
        }
    }

    public class MeteoList
    {
        public List<Meteo> list { get; set; }
    }

    public class Meteo
    {
        public Temperature main { get; set; }
        public List<Weather> weather { get; set; }
        public Wind wind { get; set; }
        public DateTime dateMeteo { get; set; }

        private string dt_txt;
        public string Dt_txt
        {
            get { return dt_txt; }
            set { 
                dt_txt = value;
                dateMeteo = DateTime.ParseExact(dt_txt, "yyyy-MM-dd HH:mm:ss", null);
            }
        }

        public City city { get; set; }

        public class Temperature
        {
            public double temp { get; set; }
        }

        public class Weather
        {
            public string description { get; set; }
        }
        public class Wind
        {
            public double speed { get; set; }
        }

        public class City
        {
            public string Name { get; set; }
        }
    }
}
