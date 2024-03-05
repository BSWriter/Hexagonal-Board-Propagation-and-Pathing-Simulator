using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserOptionsSO : ScriptableObject
{
    int _selectedFeature;
    int _propDir = 0;
    float _propSpread = 1;
    float _propReach = 1;


    public void setSelectedFeature(int featureIn) { _selectedFeature = featureIn; }
    public void setPropDir(int dirIn) { _propDir = dirIn; }
    public void setPropSpread(float spreadIn) { _propSpread = spreadIn; }
    public void setPropReach(float reachIn) { _propReach = reachIn; }

    public int getCurrFeature() { return _selectedFeature; }
    public int getPropDir() { return _propDir; }
    public float getPropSpread() { return _propSpread; }
    public float getPropReach() { return _propReach; }
}
