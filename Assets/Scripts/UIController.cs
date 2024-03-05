using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Windows;

public class UIController : MonoBehaviour
{
    UserOptionsSO _optionsSO;

    [SerializeField]
    GameObject _propagationOptions;
    [SerializeField]
    GameObject _background;
    [SerializeField]
    GameObject _reducedBackground;

    TextMeshProUGUI _propDirText;
    TextMeshProUGUI _propSpreadText;
    TextMeshProUGUI _propReachText;

    private void Start()
    {
        _propDirText = _propagationOptions.transform.Find("Direction").Find("Indicator").GetComponent<TextMeshProUGUI>();
        _propSpreadText = _propagationOptions.transform.Find("Spread").Find("Indicator").GetComponent<TextMeshProUGUI>();
        _propReachText = _propagationOptions.transform.Find("Reach").Find("Indicator").GetComponent<TextMeshProUGUI>();

        _propagationOptions.SetActive(false);

        _optionsSO = InstanceManager.Instance.getUserOptionsSOInstance();
        _background.SetActive(false);
        _reducedBackground.SetActive(true);
    }

    public void changeDir(float newVal)
    {
        _propDirText.text = $"{newVal}";

        _optionsSO.setPropDir((int)newVal);
    }

    public void changeSpread(float newVal)
    {
        _propSpreadText.text = $"{newVal}";

        _optionsSO.setPropSpread((int)newVal);
    }

    public void changeReach(float newVal)
    {
        _propReachText.text = $"{newVal}";

        _optionsSO.setPropReach((int)newVal);
    }

    public void changeCurrentFeature(int input)
    {
        if (input <= 1)
        {
            _propagationOptions.SetActive(false);
            _background.SetActive(false);
            _reducedBackground.SetActive(true);
        }
        else
        {
            _propagationOptions.SetActive(true);
            _background.SetActive(true);
            _reducedBackground.SetActive(false);
        }

        _optionsSO.setSelectedFeature(input);
    }
}
