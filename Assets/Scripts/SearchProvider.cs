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

    [SerializeField]
    private GameObject PanelText;

    [SerializeField]
    private TMP_Text FilePath;

    //true pour une recherche textuelle, false pour une recherche par coordonn�es
    private bool lastSearchCityName;

    private Vector2 lastLongLat;

    private enum MeteoType
    {
        Actual,
        Forecast
    }

    // Start is called before the first frame update
    void Start()
    {

        string cityDatas = String.Empty;

#if UNITY_STANDALONE
        cityDatas = File.ReadAllText(Application.streamingAssetsPath + @"\city.list.json");
        cityObjectList = JsonConvert.DeserializeObject<List<City>>(cityDatas);
        cityList = cityObjectList.Select(city => char.ToUpper(city.name[0]) + city.name.Substring(1)).ToList();
        apiKey = File.ReadAllText(Application.streamingAssetsPath + @"\apiKey.txt");
#endif
#if UNITY_EDITOR
        cityDatas = File.ReadAllText(Application.streamingAssetsPath + @"\city.list.json");
        cityObjectList = JsonConvert.DeserializeObject<List<City>>(cityDatas);
        cityList = cityObjectList.Select(city => char.ToUpper(city.name[0]) + city.name.Substring(1)).ToList();
        apiKey = File.ReadAllText(Application.streamingAssetsPath + @"\apiKey.txt");
#endif
#if UNITY_WEBGL
        //UnityWebRequest
        Debug.Log(Application.streamingAssetsPath);
        StartCoroutine(GetCityNamesRequest(Application.streamingAssetsPath + "/city.list.json"));
        StartCoroutine(GetAPIKeyRequest(Application.streamingAssetsPath + "/apiKey.txt"));
#endif
    }

    public void ReadStringInput(string textField)
    {
        if (textField.Length >= 3)
        {
            List<string> cities = cityList.Where(city => city.ToLower().StartsWith(textField.ToLower())).ToList();

            if (cities.Count >= 0)
            {
                CitySelect.options.Clear();
                CitySelect.gameObject.SetActive(true);
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
            else
            {
                CitySelect.options.Clear();
                CitySelect.Hide();
                CitySelect.gameObject.SetActive(false);
            }

            InputField.Select();
            InputField.caretPosition = InputField.text.Length;
        }
        else
        {
            CitySelect.options.Clear();
            CitySelect.gameObject.SetActive(false);
        }
    }

    public void GetMeteoToggleButton()
    {
        if  (lastSearchCityName)
        {
            GetMeteoCityName();
        }
        else
        {
            GetMeteoCoordinates(lastLongLat.x, lastLongLat.y);
        }
    }

    public void GetMeteoCityName()
    {
        string city = CitySelect.options[CitySelect.value].text;
        InputField.text = city;

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

        lastSearchCityName = true;
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

        lastSearchCityName = false;
        lastLongLat = new Vector2(latitude, longitude);
        ClearSearch();
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
                            PanelText.GetComponent<RectTransform>().sizeDelta = new Vector2(295, 140);
                            PanelText.gameObject.SetActive(true);
                            break;
                        case MeteoType.Forecast:
                            MeteoList forecastMeteoData = JsonConvert.DeserializeObject<MeteoList>(webRequest.downloadHandler.text);
                            DisplayForecastMeteo(forecastMeteoData);
                            PanelText.GetComponent<RectTransform>().sizeDelta = new Vector2(295, 470);
                            PanelText.gameObject.SetActive(true);
                            break;
                        default:
                            break;
                    }
                    break;
            }
        }
    }

    IEnumerator GetCityNamesRequest(string uri)
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
                    cityObjectList = JsonConvert.DeserializeObject<List<City>>(webRequest.downloadHandler.text);
                    cityList = cityObjectList.Select(city => char.ToUpper(city.name[0]) + city.name.Substring(1)).ToList();
                    break;
            }
        }
    }

    IEnumerator GetAPIKeyRequest(string uri)
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
                    apiKey = webRequest.downloadHandler.text.Substring(1);
                    break;
            }
        }
    }


    public void ClearSearch()
    {
        OutputField.text = string.Empty;
        InputField.text = string.Empty;
        CitySelect.ClearOptions();
        CitySelect.value = -1;
        CitySelect.Hide();
        CitySelect.gameObject.SetActive(false);
        PanelText.gameObject.SetActive(false);
    }

    private void DisplayActualMeteo(Meteo meteoData)
    {
        OutputField.text = String.Empty;
        string textCity = "Ville : " + (!String.IsNullOrWhiteSpace(meteoData.name) ? meteoData.name : "N/A");
        string textTemperature = "Temp�rature : " + meteoData.main.temp.ToString() + "�C";
        string textDescription = "Temps : " + meteoData.weather.ElementAt(0).description;
        double wind = meteoData.wind.speed * 3.6;
        string textWindSpeed = "Vent : " + wind.ToString() + "km/h";
        OutputField.text = (!String.IsNullOrEmpty(textCity) ? textCity + "\n\n" : "") + textTemperature + "\n" + textDescription + "\n" + textWindSpeed;
    }

    private void DisplayForecastMeteo(MeteoList meteoData)
    {
        List<Meteo> meteoByDay = meteoData.list.Where(meteo => meteo.dateMeteo.Hour == 12).ToList();
        string textCity = "Ville : " + (!String.IsNullOrWhiteSpace(meteoData.city.name) ? meteoData.city.name : "N/A");
        OutputField.text = (!String.IsNullOrEmpty(textCity) ? textCity + "\n\n" : "");
        foreach (var meteo in meteoByDay)
        {
            string textDayMeteo = meteo.dateMeteo.ToLongDateString();
            string textTemperature = "Temp�rature : " + meteo.main.temp.ToString() + "�C";
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
        public City city { get; set; }
    }

    public class Meteo
    {
        public Temperature main { get; set; }
        public List<Weather> weather { get; set; }
        public Wind wind { get; set; }
        public DateTime dateMeteo { get; set; }
        public string name { get; set; }

        private string dt_txt;
        public string Dt_txt
        {
            get { return dt_txt; }
            set
            {
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
