using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BannerToolSaveData
{
    #region Variables

    protected BannerToolSettingInfo _saveInfo = null;

    #endregion

    #region Properties

    public BannerToolSettingInfo SaveInfo
    {
        get { return _saveInfo; }
    }

    protected string SaveFileName
    {
        get { return ""; }
    }

    #endregion

    #region Methods

    protected void InitSaveData()
    {

    }

    public void SaveData()
    {
        string localData = LitJson.JsonMapper.ToJson(_saveInfo);
        FileUtility.SaveFileInDocuments(SaveFileName, localData);
    }

    public void LoadSavedData()
    {
        var jsonString = FileUtility.LoadFileInDocuments(SaveFileName);
        if (!string.IsNullOrEmpty(jsonString)) {
            _saveInfo = LitJson.JsonMapper.ToObject<BannerToolSettingInfo>(jsonString);
        } else {
            InitSaveData();
        }
    }

    public void ReleaseSavedData()
    {
        _saveInfo = null;
    }

    #endregion
}
