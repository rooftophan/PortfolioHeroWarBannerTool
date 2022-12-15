using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BannerToolImgBoard : MonoBehaviour
{
    #region Serialize Variables

#pragma warning disable 649

    [SerializeField] Button _bannerBoardButton = default(Button);
    [SerializeField] GameObject _selectImgObj = default(GameObject);
    [SerializeField] Toggle _checkToggle = default(Toggle);
    [SerializeField] Text _bannerNameText = default(Text);
    [SerializeField] Button _addButton = default(Button);
    [SerializeField] Button _removeButton = default(Button);

    [SerializeField] GameObject _newObject = default(GameObject);
    [SerializeField] GameObject _existSameObject = default(GameObject);

#pragma warning restore 649

    #endregion

    #region Variables

    string _localPathName;
    string _bannerPathName;
    int _mainType;
    int _langType;
    int _platformType;
    string _platformAppversion;
    int _platformMainType;
    int _platformLangType;
    BannerImageInfo _bannerInfo = new BannerImageInfo();

    #endregion

    #region Properties

    public Button BannerBoardButton
    {
        get { return _bannerBoardButton; }
    }

    public GameObject SelectImgObj
    {
        get { return _selectImgObj; }
    }

    public Toggle CheckToggle
    {
        get { return _checkToggle; }
    }

    public Text BannerNameText
    {
        get { return _bannerNameText; }
    }

    public string LocalPathName
    {
        get { return _localPathName; }
        set { _localPathName = value; }
    }

    public string BannerPathName
    {
        get { return _bannerPathName; }
        set { _bannerPathName = value; }
    }

    public int MainType
    {
        get { return _mainType; }
        set { _mainType = value; }
    }

    public int LangType
    {
        get { return _langType; }
        set { _langType = value; }
    }

    public int PlatformType
    {
        get { return _platformType; }
        set { _platformType = value; }
    }

    public string PlatformAppversion
    {
        get { return _platformAppversion; }
        set { _platformAppversion = value; }
    }

    public int PlatformMainType
    {
        get { return _platformMainType; }
        set { _platformMainType = value; }
    }

    public int PlatformLangType
    {
        get { return _platformLangType; }
        set { _platformLangType = value; }
    }

    public BannerImageInfo BannerImgInfo
    {
        get { return _bannerInfo; }
    }

    public Button AddButton
    {
        get { return _addButton; }
    }

    public Button RemoveButton
    {
        get { return _removeButton; }
    }

    public GameObject NewObject
    {
        get { return _newObject; }
    }

    public GameObject ExistSameObject
    {
        get { return _existSameObject; }
    }

    #endregion

    #region Methods

    public void CopyBannerToolImgBoard(BannerToolImgBoard toolImgBoard)
    {
        this._localPathName = toolImgBoard._localPathName;
        this._bannerPathName = toolImgBoard._bannerPathName;
        this._mainType = toolImgBoard._mainType;
        this._langType = toolImgBoard._langType;

        this._bannerInfo.bannerName = toolImgBoard._bannerInfo.bannerName;
        this._bannerInfo.uploadPath = toolImgBoard._bannerInfo.uploadPath;
        this._bannerInfo.bannerPathName = toolImgBoard._bannerInfo.bannerPathName;
        this._bannerInfo.fileSize = toolImgBoard._bannerInfo.fileSize;
        this._bannerInfo.uploadDate = toolImgBoard._bannerInfo.uploadDate;
        this._bannerInfo.md5Hash = toolImgBoard._bannerInfo.md5Hash;
    }

    #endregion
}
