using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class BannerUploadToolManager : MonoBehaviour
{
    #region Serialize Variables

#pragma warning disable 649

    [SerializeField] Button _uploadButton = default(Button);

    [SerializeField] Transform _bannerBoardContent = default(Transform);
    [SerializeField] BannerToolImgBoard _bannerImgBoard = default(BannerToolImgBoard);

    [SerializeField] Dropdown _buildKindDropdown = default(Dropdown);

    [SerializeField] InputField _ftpURLInputField = default(InputField);
    [SerializeField] InputField _ftpIDInputField = default(InputField);
    [SerializeField] InputField _ftpPWInputField = default(InputField);

    [SerializeField] Dropdown _mainTypeDropdown = default(Dropdown);
    [SerializeField] GameObject _languageTypeObj = default(GameObject);
    [SerializeField] Dropdown _languageTypeDropdown = default(Dropdown);

    [SerializeField] GameObject _platformTypeObj = default(GameObject);
    [SerializeField] Dropdown _platformTypeDropdown = default(Dropdown);
    [SerializeField] Dropdown _appVerTypeDropdown = default(Dropdown);
    [SerializeField] GameObject _platformMainTypeObj = default(GameObject);
    [SerializeField] Dropdown _platformMainDropdown = default(Dropdown);
    [SerializeField] GameObject _platformLangTypeObj = default(GameObject);
    [SerializeField] Dropdown _platformLangDropdown = default(Dropdown);

    [SerializeField] Image _bannerImg = default(Image);

    [SerializeField] BannerToolDownloader _downloadManager = default(BannerToolDownloader);

    [SerializeField] Button _allEnableListButton = default(Button);
    [SerializeField] Button _allDisableListButton = default(Button);

    [SerializeField] InputField _versionInputField = default(InputField);

    [SerializeField] BannerToolInfoPopup _toolInfoPopup = default(BannerToolInfoPopup);
    [SerializeField] Toggle _changedListToggle = default(Toggle);

    [SerializeField] Toggle _onlyPlatformToggle = default(Toggle);
    [SerializeField] Toggle _androidToggle = default(Toggle);
    [SerializeField] InputField _platformNumText = default(InputField);
    [SerializeField] Button _onlyPlatformButton = default(Button);

    [Header("BannerSelectInfoObj")]
    [SerializeField] Text _bannerMainTypeText = default(Text);
    [SerializeField] Text _languageTypeText = default(Text);
    [SerializeField] Text _bannerNameText = default(Text);
    [SerializeField] Text _bannerPathText = default(Text);
    [SerializeField] Text _bannerLocalPathText = default(Text);
    [SerializeField] Text _bannerFileSizeText = default(Text);

    [Header("Support Language Obj")]
    [SerializeField] Transform _supportLangContent = default(Transform);
    [SerializeField] BannerToolSupportLang _supportLangBoard = default(BannerToolSupportLang);

    [Header("Platform Appversion Obj")]
    [SerializeField] GameObject _platformAppverObj;
    [SerializeField] InputField _androidAppversionInput;
    [SerializeField] InputField _iosAppversionInput;
    [SerializeField] Dropdown _uploadPlatformDropdown;

#pragma warning restore 649

    #endregion

    #region Variables

    string[] _buildKindOptions = new string[]
    {
        "Dev", "ENT", "LIVE"
    };

    const string _webURL = "https://.....";

    string _bannerPath;

    BannerUploadFTPManager _uploadFTPManager = new BannerUploadFTPManager();

    BannerImageToolInfo _curSelectBannerToolInfo = null;

    BannerToolSettingInfo _bannerToolSettingInfo;

    int _maxLang = 16;

    BannerDefinitions.BannerToolManagerStep _bannerStep = BannerDefinitions.BannerToolManagerStep.None;

    List<string> _androidAppversionList = new List<string>();
    List<string> _iOSAppVersionList = new List<string>();

    string _selectAppversion;

    BannerImgToolFilesInfo _bannerToolObjectListInfo = new BannerImgToolFilesInfo();
    BannerImgToolFilesInfo _serverBannerToolListInfo = null;
    BannerImgToolFilesInfo _onlyPlatformBannterToolListInfo = new BannerImgToolFilesInfo();

    Dictionary<string, BannerImageToolInfo> _serverCompareBannerToolInfos;

    BannerUploadBytesInfo _bannerToolUploadByteInfo;

    List<BannerToolSupportLang> _supportLangList = new List<BannerToolSupportLang>();

    int _uploadCount = 0;

    #endregion

    #region MonoBehaviour Methods

    private void Awake()
    {
        InitBannerSelectInfoObj();

        _bannerPath = Application.dataPath + BannerHelper.originBannerPath;

        _uploadFTPManager.RootLocalPath = _bannerPath;
        _uploadFTPManager.OnFTPUploadState = OnFTPUploadState;

        _bannerImgBoard.gameObject.SetActive(false);

        _uploadButton.onClick.RemoveAllListeners();
        _uploadButton.onClick.AddListener(() => OnUploadBannerListNew());

        _ftpPWInputField.inputType = InputField.InputType.Password;

        SetBannerSettingInfo();

        LoadAllBannerImgListNew();

        SetBuildKindDropdown();

        SetMainTypeDropdown();
        SetLanguageTypeDropdown();

        _bannerImg.gameObject.SetActive(false);

        _allEnableListButton.onClick.RemoveAllListeners();
        _allEnableListButton.onClick.AddListener(OnAllEnableListNew);

        _allDisableListButton.onClick.RemoveAllListeners();
        _allDisableListButton.onClick.AddListener(OnAllDisableListNew);

        SetPlatformDropdown();
        SetPlatformMainTypeDropdown();
        SetPlatformLangTypeDropdown();

        _changedListToggle.isOn = _bannerToolSettingInfo.changedListView == 0 ? false : true;
        _changedListToggle.onValueChanged.RemoveAllListeners();
        _changedListToggle.onValueChanged.AddListener(OnChangedListToggle);

        SetSupportLangList();

        SetUploadPlatformDropdown();
    }

    private void Start()
    {
        SetBannerStep(BannerDefinitions.BannerToolManagerStep.DownloadBannerFilesInfoNew);

        SetMainTypeNew((BannerDefinitions.MainType)_bannerToolSettingInfo.mainType);
    }

    #endregion

    #region Methods

    void SetBannerSettingInfo()
    {
        _bannerToolSettingInfo = BannerToolUtil.LoadFileObjectInfo<BannerToolSettingInfo>(BannerToolUtil.GetBannerSettingInfoRootPath(), BannerHelper.bannerToolSettingFile);
        if(_bannerToolSettingInfo == null) {
            _bannerToolSettingInfo = new BannerToolSettingInfo();
            _bannerToolSettingInfo.InitBannerToolSetInfo();
        }

        _ftpURLInputField.text = _bannerToolSettingInfo.ftpURL;
        _ftpIDInputField.text = _bannerToolSettingInfo.ftpID;

        _versionInputField.text = _bannerToolSettingInfo.version.ToString();

        _androidAppversionInput.text = _bannerToolSettingInfo.androidAppver.ToString();
        _iosAppversionInput.text = _bannerToolSettingInfo.iOSAppver.ToString();
    }

    void SaveBannerSettingInfo()
    {
        if(_bannerToolSettingInfo == null) {
            _bannerToolSettingInfo = new BannerToolSettingInfo();
            _bannerToolSettingInfo.InitBannerToolSetInfo();
        }

        _bannerToolSettingInfo.ftpURL = _ftpURLInputField.text;
        _bannerToolSettingInfo.ftpID = _ftpIDInputField.text;

        _bannerToolSettingInfo.version = GetPlatformVersion(_versionInputField);
        _bannerToolSettingInfo.androidAppver = GetPlatformVersion(_androidAppversionInput);
        _bannerToolSettingInfo.iOSAppver = GetPlatformVersion(_iosAppversionInput);

        BannerToolUtil.WriteObjectFile<BannerToolSettingInfo>(BannerToolUtil.GetBannerSettingInfoRootPath(), BannerHelper.bannerToolSettingFile, _bannerToolSettingInfo);
    }

    void InitBannerSelectInfoObj()
    {
        _bannerMainTypeText.text = "";
        _languageTypeText.text = "";
        _bannerNameText.text = "";
        _bannerPathText.text = "";
        _bannerLocalPathText.text = "";
        _bannerFileSizeText.text = "";
    }

    void SetSupportLangList()
    {
        _supportLangBoard.gameObject.SetActive(false);

        for (int i = 1; i < _maxLang; i++) {
            LanguageType langType = (LanguageType)i;
            if (langType == LanguageType.ko || langType == LanguageType.th || langType == LanguageType.en ||
                langType == LanguageType.ja || langType == LanguageType.zh_hans || langType == LanguageType.zh_hant ||
                langType == LanguageType.de || langType == LanguageType.fr)
                AddSupportLangBoard(langType, true);
            else
                AddSupportLangBoard(langType, false);
        }
    }

    void RefreshSupportLangList()
    {
        HashSet<string> supportLangHashSet = new HashSet<string>();
        if (_serverBannerToolListInfo != null) {
            for (int i = 0; i < _serverBannerToolListInfo.supportLangList.Count; i++) {
                supportLangHashSet.Add(_serverBannerToolListInfo.supportLangList[i]);
            }
        }

        for (int i = 0;i< _supportLangList.Count; i++) {
            if(_supportLangList[i].LangNameText.text == "ko" || _supportLangList[i].LangNameText.text == "th" || _supportLangList[i].LangNameText.text == "en" ||
                _supportLangList[i].LangNameText.text == "ja" || _supportLangList[i].LangNameText.text == "zh_hans" || _supportLangList[i].LangNameText.text == "zh_hant" ||
                _supportLangList[i].LangNameText.text == "de" || _supportLangList[i].LangNameText.text == "fr"  ||
                supportLangHashSet.Contains(_supportLangList[i].LangNameText.text)) {
                _supportLangList[i].CheckToggle.isOn = true;
            } else {
                _supportLangList[i].CheckToggle.isOn = false;
            }
        }
    }

    void ReleaseSupportLangList()
    {
        for(int i = 0;i< _supportLangList.Count; i++) {
            Destroy(_supportLangList[i].gameObject);
        }

        _supportLangList.Count();
    }

    void LoadAllBannerImgListNew()
    {
        _bannerToolObjectListInfo.InitBannerFilesInfo();

        string[] bannerFiles = Directory.GetFiles(_bannerPath, "*.*", SearchOption.AllDirectories);

        if (bannerFiles != null && bannerFiles.Length > 0) {
            string progressTitle = "Init Banner Files";

            for (int i = 0; i < bannerFiles.Length; i++) {
                string localPathName = bannerFiles[i];

                if (localPathName.Contains("BannerFilesInfo.bytes"))
                    continue;

                int fileLength = localPathName.Length;
                if (fileLength > 5) {
                    string lastName = localPathName.Substring(fileLength - 5, 5);
                    if (lastName == ".meta") {
                        continue;
                    }
                }

                string bannerPathName = localPathName.Replace(_bannerPath, "").Replace('\\', '/');
                string[] bannerSplits = bannerPathName.Split('/');
                string bannerName = "";
                if (bannerSplits != null && bannerSplits.Length > 0) {
                    bannerName = bannerSplits[bannerSplits.Length - 1];
                } else {
                    bannerName = bannerPathName;
                }

                BannerImageToolInfo inputBannerToolInfo = new BannerImageToolInfo();

                BannerToolImgBoard bannerImgObj = Instantiate<BannerToolImgBoard>(_bannerImgBoard);
                bannerImgObj.transform.SetParent(_bannerBoardContent);
                bannerImgObj.transform.localScale = Vector3.one;
                bannerImgObj.gameObject.SetActive(false);

                inputBannerToolInfo.localPathName = localPathName;
                inputBannerToolInfo.bannerPathName = bannerPathName;
                inputBannerToolInfo.originBannerPathName = bannerPathName;

                bannerImgObj.BannerNameText.text = bannerName;


                inputBannerToolInfo.bannerImgObj = bannerImgObj;
                inputBannerToolInfo.SetCheckBannerState(false);

                inputBannerToolInfo.langType = GetBannerLanguageType(bannerPathName);
                inputBannerToolInfo.mainType = (int)BannerDefinitions.MainType.All;

                if (inputBannerToolInfo.langType != -1) {
                    inputBannerToolInfo.mainType = (int)BannerDefinitions.MainType.Language;
                } else {
                    string[] bannerPathSplit = bannerPathName.Split('/');
                    if (bannerPathSplit != null && bannerPathSplit.Length > 0) {
                        if (bannerPathSplit[0] == BannerHelper.commonPathName) {
                            inputBannerToolInfo.mainType = (int)BannerDefinitions.MainType.common;
                        } else if (bannerPathSplit[0] == BannerHelper.platformPathName) {
                            inputBannerToolInfo.mainType = (int)BannerDefinitions.MainType.platform;
                            inputBannerToolInfo.platformType = GetBannerPlatformType(bannerPathSplit);
                            inputBannerToolInfo.platformAppversion = bannerPathSplit[2];
                            if (inputBannerToolInfo.platformType == (int)BannerDefinitions.PlatformType.Android) {
                                if (!_androidAppversionList.Contains(bannerPathSplit[2]))
                                    _androidAppversionList.Add(bannerPathSplit[2]);
                            } else if (inputBannerToolInfo.platformType == (int)BannerDefinitions.PlatformType.iOS) {
                                if (!_iOSAppVersionList.Contains(bannerPathSplit[2]))
                                    _iOSAppVersionList.Add(bannerPathSplit[2]);
                            }

                            inputBannerToolInfo.platformLangType = GetBannerPlatformLangType(bannerPathSplit);
                            if (inputBannerToolInfo.platformLangType != -1) {
                                inputBannerToolInfo.platformMainType = (int)BannerDefinitions.MainType.Language;
                            } else {
                                if (bannerPathSplit[3] == BannerHelper.commonPathName) {
                                    inputBannerToolInfo.platformMainType = (int)BannerDefinitions.MainType.common;
                                }
                            }

                            string platformPrePath = string.Format("{0}/{1}/{2}/", bannerPathSplit[0], bannerPathSplit[1], bannerPathSplit[2]);
                            inputBannerToolInfo.bannerPathName = inputBannerToolInfo.bannerPathName.Replace(platformPrePath, "");
                        }
                    }
                }


                inputBannerToolInfo.bannerName = GetBannerName(bannerPathName);

                inputBannerToolInfo.fileSize = IOUtil.GetFileSize(localPathName);
                inputBannerToolInfo.savefileSize = inputBannerToolInfo.fileSize;

                byte[] imgBytes = BannerFile.ReadBytesByPath(localPathName);
                inputBannerToolInfo.md5Hash = StringEncription.MakeMD5ByBytes(imgBytes);
                inputBannerToolInfo.saveMD5Hash = inputBannerToolInfo.md5Hash;

                bannerImgObj.BannerBoardButton.onClick.RemoveAllListeners();
                bannerImgObj.BannerBoardButton.onClick.AddListener(() => OnSelectBannerBoardNew(inputBannerToolInfo));

                bannerImgObj.CheckToggle.onValueChanged.RemoveAllListeners();
                bannerImgObj.CheckToggle.onValueChanged.AddListener((x) => OnCheckBannerBoardNew(x, inputBannerToolInfo));

                inputBannerToolInfo.compareBannerKey = GetCompareBannerToolKey(inputBannerToolInfo);

                AddBannerToolObjectInfo(inputBannerToolInfo);

#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar(progressTitle, bannerPathName, (float)i / (float)bannerFiles.Length);
#endif
            }

#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
    }

    void RefreshBoardState()
    {
        RefreshBoardBaseState((BannerBaseToolFilesInfo)_bannerToolObjectListInfo);

        List<string> platformKeys = _bannerToolObjectListInfo.platformBannerFilesInfos.Keys.ToList();
        for(int i = 0;i< platformKeys.Count; i++) {
            Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo> appVerBaseToolFilesInfos = _bannerToolObjectListInfo.platformBannerFilesInfos[platformKeys[i]];
            List<string> appVerKeys = appVerBaseToolFilesInfos.Keys.ToList();
            for(int j = 0;j< appVerKeys.Count; j++) {
                BannerBaseToolFilesInfo appVerBaseToolFiesInfo = appVerBaseToolFilesInfos[appVerKeys[j]];
                RefreshBoardBaseState(appVerBaseToolFiesInfo);
            }
        }
    }

    void RefreshBoardBaseState(BannerBaseToolFilesInfo baseToolFilesInfo)
    {
        for (int i = 0; i < baseToolFilesInfo.commonList.Count; i++) {
            BannerImageToolInfo bannerToolInfo = baseToolFilesInfo.commonList[i];
            SetBannerToolBoardState(bannerToolInfo);
        }

        List<string> langKeys = baseToolFilesInfo.languageList.Keys.ToList();
        for (int i = 0; i < langKeys.Count; i++) {
            List<BannerImageToolInfo> langBannerToolInfos = baseToolFilesInfo.languageList[langKeys[i]];
            for (int j = 0; j < langBannerToolInfos.Count; j++) {
                BannerImageToolInfo bannerToolInfo = langBannerToolInfos[j];
                SetBannerToolBoardState(bannerToolInfo);
            }
        }
    }

    void SetBannerToolBoardState(BannerImageToolInfo bannerToolInfo)
    {
        if (_serverCompareBannerToolInfos != null) {
            if (_serverCompareBannerToolInfos.ContainsKey(bannerToolInfo.compareBannerKey)) {
                BannerImageToolInfo compareToolInfo = _serverCompareBannerToolInfos[bannerToolInfo.compareBannerKey];
                if(compareToolInfo.md5Hash == bannerToolInfo.md5Hash) {
                    bannerToolInfo.SetBoardState(BannerToolBoardState.ExistSame);
                    SetServerBannerToolData(bannerToolInfo, compareToolInfo);
                } else {
                    bannerToolInfo.SetBoardState(BannerToolBoardState.Enable);
                }
            } else {
                bannerToolInfo.SetBoardState(BannerToolBoardState.New);
                ResetBannerToolData(bannerToolInfo);
            }
        } else {
            bannerToolInfo.SetBoardState(BannerToolBoardState.New);
            ResetBannerToolData(bannerToolInfo);
        }
    }

    void AddBannerToolObjectInfo(BannerImageToolInfo bannerToolInfo)
    {
        if(bannerToolInfo.mainType == (int)BannerDefinitions.MainType.common) {
            _bannerToolObjectListInfo.commonList.Add(bannerToolInfo);
        } else if(bannerToolInfo.mainType == (int)BannerDefinitions.MainType.Language) {
            string bannerLangTypeKey = ((LanguageType)bannerToolInfo.langType).ToString();
            List<BannerImageToolInfo> langToolInfos = null;
            if (_bannerToolObjectListInfo.languageList.ContainsKey(bannerLangTypeKey)) {
                langToolInfos = _bannerToolObjectListInfo.languageList[bannerLangTypeKey];
            } else {
                langToolInfos = new List<BannerImageToolInfo>();
                _bannerToolObjectListInfo.languageList.Add(bannerLangTypeKey, langToolInfos);
            }

            langToolInfos.Add(bannerToolInfo);
        } else if(bannerToolInfo.mainType == (int)BannerDefinitions.MainType.platform) {
            string platformKey = ((BannerDefinitions.PlatformType)bannerToolInfo.platformType).ToString();

            Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo> appVerBannerFilesInfo = null;
            if (_bannerToolObjectListInfo.platformBannerFilesInfos.ContainsKey(platformKey)) {
                appVerBannerFilesInfo = _bannerToolObjectListInfo.platformBannerFilesInfos[platformKey];
            } else {
                appVerBannerFilesInfo = new Dictionary<string, BannerBaseToolFilesInfo>();
                _bannerToolObjectListInfo.platformBannerFilesInfos.Add(platformKey, appVerBannerFilesInfo);
            }

            string appVerKey = bannerToolInfo.platformAppversion;
            BannerBaseToolFilesInfo bannerBaseFilesInfo = null;
            if (appVerBannerFilesInfo.ContainsKey(appVerKey)) {
                bannerBaseFilesInfo = appVerBannerFilesInfo[appVerKey];
            } else {
                bannerBaseFilesInfo = new BannerBaseToolFilesInfo();
                appVerBannerFilesInfo.Add(appVerKey, bannerBaseFilesInfo);
            }

            if (bannerToolInfo.platformMainType == (int)BannerDefinitions.MainType.common) {
                bannerBaseFilesInfo.commonList.Add(bannerToolInfo);
            } else if(bannerToolInfo.platformMainType == (int)BannerDefinitions.MainType.Language) {
                string platformLangTypeKey = ((LanguageType)bannerToolInfo.platformLangType).ToString();
                List<BannerImageToolInfo> platformLangBannerInfos = null;
                if (bannerBaseFilesInfo.languageList.ContainsKey(platformLangTypeKey)) {
                    platformLangBannerInfos = bannerBaseFilesInfo.languageList[platformLangTypeKey];
                } else {
                    platformLangBannerInfos = new List<BannerImageToolInfo>();
                    bannerBaseFilesInfo.languageList.Add(platformLangTypeKey, platformLangBannerInfos);
                }

                platformLangBannerInfos.Add(bannerToolInfo);
            }
        }
    }

    string GetUploadPathName(string bannerPathName)
    {
        string retValue = "";
        string[] pathSplit = bannerPathName.Split('/');
        if(pathSplit != null && pathSplit.Length > 1) {
            retValue = string.Format("{0}/", pathSplit[0]);
            for(int i = 1;i< pathSplit.Length; i++) {
                if(i == 1) {
                    retValue += pathSplit[i];
                } else {
                    retValue += string.Format("_{0}", pathSplit[i]);
                }
            }
        } else {
            retValue = bannerPathName;
        }

        return retValue;
    }

    string GetBannerName(string bannerPathName)
    {
        char[] nameChars = bannerPathName.ToCharArray();
        int pointIndex = -1;
        for (int j = nameChars.Length - 1; j >= 0; j--) {
            if (nameChars[j] == '.') {
                pointIndex = j;
                break;
            }
        }

        if (pointIndex != -1)
            bannerPathName = bannerPathName.Substring(0, pointIndex);

        string[] bannerSplit = bannerPathName.Split('/');
        if(bannerSplit != null && bannerSplit.Length > 0) {
            return bannerSplit[bannerSplit.Length - 1];
        } else {
            return bannerPathName;
        }
    }

    int GetBannerLanguageType(string bannerPathName)
    {
        string[] bannerPathSplit = bannerPathName.Split('/');

        if (bannerPathSplit == null || bannerPathSplit.Length == 0)
            return -1;

        for (int i = 1; i < _maxLang; i++) {
            LanguageType langType = (LanguageType)i;
            if (bannerPathSplit[0] == langType.ToString()) {
                return (int)langType;
            }
        }

        return -1;
    }

    int GetBannerPlatformLangType(string[] bannerPathSplit)
    {
        if (bannerPathSplit == null || bannerPathSplit.Length < 3)
            return -1;

        for (int i = 1; i < _maxLang; i++) {
            LanguageType langType = (LanguageType)i;
            if (bannerPathSplit[3] == langType.ToString()) {
                return (int)langType;
            }
        }

        return -1;
    }

    int GetBannerPlatformType(string[] bannerPathSplit)
    {
        if (bannerPathSplit == null || bannerPathSplit.Length <= 1)
            return -1;

        if(bannerPathSplit[1] == BannerDefinitions.PlatformType.Android.ToString()) {
            return (int)BannerDefinitions.PlatformType.Android;
        } else if(bannerPathSplit[1] == BannerDefinitions.PlatformType.iOS.ToString()) {
            return (int)BannerDefinitions.PlatformType.iOS;
        }

        return -1;
    }

    List<BannerImageToolInfo> GetUploadBannerListNew(bool isAllBanner = false)
    {
        List<BannerImageToolInfo> retValue = new List<BannerImageToolInfo>();

        SetUploadBannerBaseToolObjectInfo(retValue, _bannerToolObjectListInfo, isAllBanner);

        List<string> platformKeys = _bannerToolObjectListInfo.platformBannerFilesInfos.Keys.ToList();
        for(int i = 0;i< platformKeys.Count; i++) {
            Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo> appVerFilesInfos = _bannerToolObjectListInfo.platformBannerFilesInfos[platformKeys[i]];
            List<string> appVerKeys = appVerFilesInfos.Keys.ToList();
            for(int j = 0;j< appVerKeys.Count; j++) {
                BannerBaseToolFilesInfo appVerBannerBaseTool = appVerFilesInfos[appVerKeys[j]];

                SetUploadBannerBaseToolObjectInfo(retValue, appVerBannerBaseTool, isAllBanner);
            }
        }

        return retValue;
    }

    void SetUploadBannerBaseToolObjectInfo(List<BannerImageToolInfo> uploadToolInfos, BannerBaseToolFilesInfo bannerBaseToolInfo, bool isAllBanner = false)
    {
        for (int i = 0; i < bannerBaseToolInfo.commonList.Count; i++) {
            BannerImageToolInfo bannerToolInfo = bannerBaseToolInfo.commonList[i];
            if (bannerToolInfo.isCheckState) {
                bannerToolInfo.uploadDate = string.Format("{0}{1:D2}{2:D2}{3:D2}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Minute);
                bannerToolInfo.uploadPath = GetUploadPathName(bannerToolInfo.originBannerPathName);
                uploadToolInfos.Add(bannerToolInfo);
            } else {
                if(bannerToolInfo.boardState == BannerToolBoardState.Enable) {
                    if (_serverCompareBannerToolInfos.ContainsKey(bannerToolInfo.compareBannerKey)) {
                        SetServerBannerToolData(bannerToolInfo, _serverCompareBannerToolInfos[bannerToolInfo.compareBannerKey]);
                    }
                }

                if (isAllBanner) {
                    uploadToolInfos.Add(bannerToolInfo);
                }
            }
        }

        List<string> langKeys = bannerBaseToolInfo.languageList.Keys.ToList();
        for (int i = 0; i < langKeys.Count; i++) {
            List<BannerImageToolInfo> bannerLangToolInfos = bannerBaseToolInfo.languageList[langKeys[i]];
            for (int j = 0; j < bannerLangToolInfos.Count; j++) {
                BannerImageToolInfo bannerToolInfo = bannerLangToolInfos[j];
                if (bannerToolInfo.isCheckState) {
                    bannerToolInfo.uploadDate = string.Format("{0}{1:D2}{2:D2}{3:D2}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Minute);
                    bannerToolInfo.uploadPath = GetUploadPathName(bannerToolInfo.originBannerPathName);
                    uploadToolInfos.Add(bannerToolInfo);
                } else {
                    if (bannerToolInfo.boardState == BannerToolBoardState.Enable) {
                        if (_serverCompareBannerToolInfos.ContainsKey(bannerToolInfo.compareBannerKey)) {
                            SetServerBannerToolData(bannerToolInfo, _serverCompareBannerToolInfos[bannerToolInfo.compareBannerKey]);
                        }
                    }

                    if (isAllBanner) {
                        uploadToolInfos.Add(bannerToolInfo);
                    }
                }
            }
        }
    }

    void SetBuildKindDropdown()
    {
        _buildKindDropdown.ClearOptions();

        _buildKindDropdown.AddOptions(_buildKindOptions.ToList());
        _buildKindDropdown.onValueChanged.RemoveAllListeners();
        _buildKindDropdown.value = _bannerToolSettingInfo.buildKind;
        _buildKindDropdown.onValueChanged.AddListener(OnChangeBuildKindDropdown);

        if(GetBuildKind() == BannerHelper.uploadPlatformType) {
            _platformAppverObj.SetActive(true);
        } else {
            _platformAppverObj.SetActive(false);
        }
    }

    void SetMainTypeDropdown()
    {
        _mainTypeDropdown.ClearOptions();
        List<string> contentDropDowns = new List<string>();
        for (int i = 0; i < (int)BannerDefinitions.MainType.platform; i++) {
            contentDropDowns.Add(((BannerDefinitions.MainType)i).ToString());
        }

        _mainTypeDropdown.AddOptions(contentDropDowns);
        _mainTypeDropdown.onValueChanged.RemoveAllListeners();
        _mainTypeDropdown.value = _bannerToolSettingInfo.mainType;
        _mainTypeDropdown.onValueChanged.AddListener(OnChangeMainTypeDropdown);
    }

    void SetLanguageTypeDropdown()
    {
        _languageTypeDropdown.ClearOptions();
        List<string> contentDropDowns = new List<string>();
        for (int i = 1; i < _maxLang; i++) {
            contentDropDowns.Add(((LanguageType)i).ToString());
        }

        _languageTypeDropdown.AddOptions(contentDropDowns);
        _languageTypeDropdown.onValueChanged.RemoveAllListeners();
        _languageTypeDropdown.onValueChanged.AddListener(OnChangeLangTypeDropdown);

    }

    void SetPlatformDropdown()
    {
        _platformTypeDropdown.ClearOptions();

        List<string> contentDropDowns = new List<string>();
        for (int i = 0; i < (int)BannerDefinitions.PlatformType.Max; i++) {
            contentDropDowns.Add(((BannerDefinitions.PlatformType)i).ToString());
        }

        _platformTypeDropdown.AddOptions(contentDropDowns);
        _platformTypeDropdown.onValueChanged.RemoveAllListeners();
        _platformTypeDropdown.value = _bannerToolSettingInfo.platformType;
        _platformTypeDropdown.onValueChanged.AddListener(OnChangePlatformDropdown);
    }

    void SetPlatformMainTypeDropdown()
    {
        _platformMainDropdown.ClearOptions();
        List<string> contentDropDowns = new List<string>();
        for (int i = 0; i < (int)BannerDefinitions.MainType.platform; i++) {
            contentDropDowns.Add(((BannerDefinitions.MainType)i).ToString());
        }

        _platformMainDropdown.AddOptions(contentDropDowns);
        _platformMainDropdown.onValueChanged.RemoveAllListeners();
        _platformMainDropdown.value = _bannerToolSettingInfo.platformMainType;
        _platformMainDropdown.onValueChanged.AddListener(OnChangePlatformMainTypeDropdown);
    }

    void SetPlatformLangTypeDropdown()
    {
        _platformLangDropdown.ClearOptions();
        List<string> contentDropDowns = new List<string>();
        for (int i = 1; i < _maxLang; i++) {
            contentDropDowns.Add(((LanguageType)i).ToString());
        }

        _platformLangDropdown.AddOptions(contentDropDowns);
        _platformLangDropdown.onValueChanged.RemoveAllListeners();
        _platformLangDropdown.value = _bannerToolSettingInfo.platformLangType - 1;
        _platformLangDropdown.onValueChanged.AddListener(OnChangePlatformLangTypeDropdown);
    }

    void SetMainTypeNew(BannerDefinitions.MainType mainType)
    {
        _bannerToolSettingInfo.mainType = (int)mainType;
        switch (mainType) {
            case BannerDefinitions.MainType.All:
                _languageTypeObj.SetActive(false);
                _platformTypeObj.SetActive(false);
                SetAllBannerObjList(true);
                RefreshEnableBannerList(_changedListToggle.isOn);
                break;
            case BannerDefinitions.MainType.common:
                _languageTypeObj.SetActive(false);
                _platformTypeObj.SetActive(false);
                SetCommonBannerObjList();
                RefreshEnableBannerList(_changedListToggle.isOn);
                break;
            case BannerDefinitions.MainType.Language:
                _languageTypeObj.SetActive(true);
                _platformTypeObj.SetActive(false);
                _languageTypeDropdown.value = _bannerToolSettingInfo.languageType - 1;
                SetLangBannerObjList((LanguageType)_bannerToolSettingInfo.languageType);
                break;
            case BannerDefinitions.MainType.platform:
                _languageTypeObj.SetActive(false);
                _platformTypeObj.SetActive(true);
                SetAppversionDropdown();
                SetPlatformMainTypeNew((BannerDefinitions.MainType)_bannerToolSettingInfo.platformMainType);
                break;
        }
    }

    void SetPlatformMainTypeNew(BannerDefinitions.MainType platformMainType)
    {
        _bannerToolSettingInfo.platformMainType = (int)platformMainType;
        BannerBaseToolFilesInfo bannerBaseFilesInfo = GetPlatformBannerObjList((BannerDefinitions.PlatformType)_bannerToolSettingInfo.platformType, _appVerTypeDropdown.value);
        switch (platformMainType) {
            case BannerDefinitions.MainType.All:
                _platformLangTypeObj.SetActive(false);
                SetPlatformAllBannerObjList(bannerBaseFilesInfo);
                RefreshEnableBannerList(_changedListToggle.isOn);
                break;
            case BannerDefinitions.MainType.common:
                _platformLangTypeObj.SetActive(false);
                SetPlatformCommonBannerObjList(bannerBaseFilesInfo);
                RefreshEnableBannerList(_changedListToggle.isOn);
                break;
            case BannerDefinitions.MainType.Language:
                _platformLangTypeObj.SetActive(true);
                SetPlatformLangBannerObjList(bannerBaseFilesInfo, (LanguageType)_bannerToolSettingInfo.platformLangType);
                break;
        }
    }

    void SetPlatformAllBannerObjList(BannerBaseToolFilesInfo baseToolFilesInfo)
    {
        SetAllBannerObjList(false);

        if (baseToolFilesInfo != null) {
            SetBaseBannerObjList(baseToolFilesInfo, true);
        }
    }

    void SetPlatformCommonBannerObjList(BannerBaseToolFilesInfo baseToolFilesInfo)
    {
        SetAllBannerObjList(false);

        if(baseToolFilesInfo != null) {
            if(baseToolFilesInfo.commonList != null && baseToolFilesInfo.commonList.Count > 0) {
                for(int i = 0;i< baseToolFilesInfo.commonList.Count; i++) {
                    baseToolFilesInfo.commonList[i].SetViewState(true);
                }
            }
        }
    }

    void SetPlatformLangBannerObjList(BannerBaseToolFilesInfo baseToolFilesInfo, LanguageType langType)
    {
        SetAllBannerObjList(false);

        if(baseToolFilesInfo != null) {
            if(baseToolFilesInfo.languageList != null && baseToolFilesInfo.languageList.Count > 0) {
                if (baseToolFilesInfo.languageList.ContainsKey(langType.ToString())) {
                    List<BannerImageToolInfo> bannerLangToolInfos = baseToolFilesInfo.languageList[langType.ToString()];
                    for(int i = 0;i< bannerLangToolInfos.Count; i++) {
                        BannerImageToolInfo bannerToolInfo = bannerLangToolInfos[i];
                        bannerToolInfo.SetViewState(true);
                    }
                }
            }
        }

        RefreshEnableBannerList(_changedListToggle.isOn);
    }

    void SetBaseBannerObjList(BannerBaseToolFilesInfo baseToolFilesInfo, bool isEnable)
    {
        for (int i = 0; i < baseToolFilesInfo.commonList.Count; i++) {
            BannerImageToolInfo bannerToolInfo = baseToolFilesInfo.commonList[i];
            bannerToolInfo.SetViewState(isEnable);

            if (_serverCompareBannerToolInfos != null) {
                if (_serverCompareBannerToolInfos.ContainsKey(bannerToolInfo.compareBannerKey)) {
                    SetServerBannerToolData(bannerToolInfo, _serverCompareBannerToolInfos[bannerToolInfo.compareBannerKey]);
                }
            }
        }

        List<string> langKeys = baseToolFilesInfo.languageList.Keys.ToList();
        for (int i = 0; i < langKeys.Count; i++) {
            List<BannerImageToolInfo> langBannerToolInfos = baseToolFilesInfo.languageList[langKeys[i]];
            for (int j = 0; j < langBannerToolInfos.Count; j++) {
                BannerImageToolInfo bannerToolInfo = langBannerToolInfos[j];
                bannerToolInfo.SetViewState(isEnable);
            }
        }
    }

    void SetServerBannerToolData(BannerImageToolInfo bannerToolData, BannerImageToolInfo serverBannerData)
    {
        bannerToolData.uploadPath = serverBannerData.uploadPath;
        bannerToolData.fileSize = serverBannerData.fileSize;
        bannerToolData.uploadDate = serverBannerData.uploadDate;
        bannerToolData.md5Hash = serverBannerData.md5Hash;
    }

    void ResetBannerToolData(BannerImageToolInfo bannerToolData)
    {
        bannerToolData.fileSize = bannerToolData.savefileSize;
        bannerToolData.md5Hash = bannerToolData.saveMD5Hash;
    }

    void SetAllBannerObjList(bool isEnable)
    {
        SetBaseBannerObjList(_bannerToolObjectListInfo, isEnable);

        List<string> platformKeys = _bannerToolObjectListInfo.platformBannerFilesInfos.Keys.ToList();
        for(int i = 0;i< platformKeys.Count; i++) {
            Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo> appVerFilesInfos = _bannerToolObjectListInfo.platformBannerFilesInfos[platformKeys[i]];
            List<string> appVerKeys = appVerFilesInfos.Keys.ToList();
            for(int j = 0;j< appVerKeys.Count; j++) {
                BannerBaseToolFilesInfo bannerBaseFilesInfo = appVerFilesInfos[appVerKeys[j]];
                SetBaseBannerObjList(bannerBaseFilesInfo, isEnable);
            }
        }
    }

    void SetCommonBannerObjList()
    {
        SetAllBannerObjList(false);

        for (int i = 0; i < _bannerToolObjectListInfo.commonList.Count; i++) {
            BannerImageToolInfo bannerToolInfo = _bannerToolObjectListInfo.commonList[i];
            bannerToolInfo.SetViewState(true);
        }
    }

    void SetLangBannerObjList(LanguageType langType)
    {
        SetAllBannerObjList(false);

        string langStr = langType.ToString();
        if (_bannerToolObjectListInfo.languageList.ContainsKey(langStr)) {
            List<BannerImageToolInfo> langBannerList = _bannerToolObjectListInfo.languageList[langStr];
            for(int i = 0;i< langBannerList.Count; i++) {
                BannerImageToolInfo bannerToolInfo = langBannerList[i];
                bannerToolInfo.SetViewState(true);
            }
        }

        RefreshEnableBannerList(_changedListToggle.isOn);
    }

    BannerBaseToolFilesInfo GetPlatformBannerObjList(BannerDefinitions.PlatformType platformType, int appVerIndex)
    {
        BannerBaseToolFilesInfo retValue = null;

        string appVerStr = "";
        if (platformType == BannerDefinitions.PlatformType.Android) {
            if(_androidAppversionList != null && _androidAppversionList.Count > 0) {
                if(appVerIndex < _androidAppversionList.Count) {
                    appVerStr = _androidAppversionList[appVerIndex];
                }
            }
        } else if(platformType == BannerDefinitions.PlatformType.iOS) {
            if (_iOSAppVersionList != null && _iOSAppVersionList.Count > 0) {
                if (appVerIndex < _iOSAppVersionList.Count) {
                    appVerStr = _iOSAppVersionList[appVerIndex];
                }
            }
        }

        if (!string.IsNullOrEmpty(appVerStr)) {
            if (_bannerToolObjectListInfo.platformBannerFilesInfos.ContainsKey(platformType.ToString())) {
                Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo> appVerBannerFilesInfos = _bannerToolObjectListInfo.platformBannerFilesInfos[platformType.ToString()];
                if (appVerBannerFilesInfos.ContainsKey(appVerStr)) {
                    retValue = appVerBannerFilesInfos[appVerStr];
                }
            }
        }

        return retValue;
    }

    string GetCompareBannerToolKey(BannerImageToolInfo bannerImgToolInfo)
    {
        string compareKey = "";
        if (bannerImgToolInfo.mainType == (int)BannerDefinitions.MainType.common) {
            compareKey = BannerToolUtil.GetCompareBannerFileKey(BannerDefinitions.MainType.common, bannerImgToolInfo.originBannerPathName);
        } else if (bannerImgToolInfo.mainType == (int)BannerDefinitions.MainType.Language) {
            compareKey = BannerToolUtil.GetCompareBannerFileKey(BannerDefinitions.MainType.Language, ((LanguageType)bannerImgToolInfo.langType).ToString(), bannerImgToolInfo.originBannerPathName);
        } else if (bannerImgToolInfo.mainType == (int)BannerDefinitions.MainType.platform) {
            if (bannerImgToolInfo.platformMainType == (int)BannerDefinitions.MainType.common) {
                compareKey = BannerToolUtil.GetCompareBannerFileKey(BannerDefinitions.MainType.platform, ((BannerDefinitions.PlatformType)bannerImgToolInfo.platformType).ToString(),
                            bannerImgToolInfo.platformAppversion, BannerDefinitions.MainType.common.ToString(), bannerImgToolInfo.originBannerPathName);
            } else if (bannerImgToolInfo.platformMainType == (int)BannerDefinitions.MainType.Language) {
                compareKey = BannerToolUtil.GetCompareBannerFileKey(BannerDefinitions.MainType.platform, ((BannerDefinitions.PlatformType)bannerImgToolInfo.platformType).ToString(),
                                bannerImgToolInfo.platformAppversion, BannerDefinitions.MainType.Language.ToString(), ((LanguageType)bannerImgToolInfo.platformLangType).ToString(), bannerImgToolInfo.originBannerPathName);
            }
        }

        return compareKey;
    }

    BannerUploadBytesInfo GetBannerImageToolFilesBytesInfo()
    {
        BannerUploadBytesInfo retValue = new BannerUploadBytesInfo();

        retValue.uploadPath = BannerToolUtil.bannerToolFilesInfoName;
        retValue.uploadBytes = BannerToolUtil.GetBannerToolFilesBytes(_bannerToolObjectListInfo);

        return retValue;
    }

    BannerUploadBytesInfo GetAndroidBannerFilesBytesInfoNew()
    {
        BannerUploadBytesInfo retValue = new BannerUploadBytesInfo();

        BannerImageFilesInfo uploadFilesInfo = BannerToolUtil.GetUploadBannerImageFilesInfo(_bannerToolObjectListInfo, BannerDefinitions.PlatformType.Android);

        retValue.uploadPath = string.Format(BannerHelper.bannerImgFilesFile, "Android");
        retValue.uploadBytes = GetCompressBannerFilesBytes(uploadFilesInfo);

        return retValue;
    }

    BannerUploadBytesInfo GetiOSBannerFilesBytesInfoNew()
    {
        BannerUploadBytesInfo retValue = new BannerUploadBytesInfo();

        BannerImageFilesInfo uploadFilesInfo = BannerToolUtil.GetUploadBannerImageFilesInfo(_bannerToolObjectListInfo, BannerDefinitions.PlatformType.iOS);

        retValue.uploadPath = string.Format(BannerHelper.bannerImgFilesFile, "iOS");
        retValue.uploadBytes = GetCompressBannerFilesBytes(uploadFilesInfo);

        return retValue;
    }

    byte[] GetCompressBannerFilesBytes(BannerImageFilesInfo bannerFilesInfo)
    {
        string str = JsonMapper.ToJson(bannerFilesInfo);

        byte[] compressBytes = CLZF2.Compress(Encoding.UTF8.GetBytes(str));

        return compressBytes;
    }

    void SetBannerStep(BannerDefinitions.BannerToolManagerStep bannerToolStep)
    {
        _bannerStep = bannerToolStep;
        switch (_bannerStep) {
            case BannerDefinitions.BannerToolManagerStep.DownloadBannerFilesInfoNew:
                SetDownloadBannerInfoStepNew();
                break;
            case BannerDefinitions.BannerToolManagerStep.LoadBannerFilesInfo:
                SetLoadBannerInfoStep();
                break;
            case BannerDefinitions.BannerToolManagerStep.Manage:
                break;
        }
    }

    void SetDownloadBannerInfoStepNew()
    {
        _serverBannerToolListInfo = null;
        _serverCompareBannerToolInfos = null;

        if(GetBuildKind() == BannerHelper.uploadPlatformType) {
            string platformStr = ((BannerDefinitions.PlatformType)_bannerToolSettingInfo.uploadPlatform).ToString();
            int uploadAppver = 0;
            if ((BannerDefinitions.PlatformType)_bannerToolSettingInfo.uploadPlatform == BannerDefinitions.PlatformType.Android) {
                uploadAppver = _bannerToolSettingInfo.androidAppver;
            } else if ((BannerDefinitions.PlatformType)_bannerToolSettingInfo.uploadPlatform == BannerDefinitions.PlatformType.iOS) {
                uploadAppver = _bannerToolSettingInfo.iOSAppver;
            }

            string rootPath = string.Format(BannerHelper.rootFTPPath, GetBuildKind()) + string.Format("/{0}/{1}", platformStr, uploadAppver.ToString());
            string filePath = string.Format("{0}/{1}", rootPath, BannerToolUtil.bannerToolFilesInfoName);
            _downloadManager.AddDownloadFile(_webURL, filePath, OnCompleteBannerInfoNew, OnFailBannerInfoNew);
        } else {
            string rootPath = string.Format(BannerHelper.rootFTPPath, GetBuildKind());
            string filePath = string.Format("{0}/{1}", rootPath, BannerToolUtil.bannerToolFilesInfoName);
            _downloadManager.AddDownloadFile(_webURL, filePath, OnCompleteBannerInfoNew, OnFailBannerInfoNew);
        }
    }

    void SetLoadBannerInfoStep()
    {
        SetBannerStep(BannerDefinitions.BannerToolManagerStep.Manage);
    }

    string GetBuildKind()
    {
        return _buildKindOptions[_buildKindDropdown.value];
    }

    void WriteBannerFilesInfo(string rootPath)
    {
        for (int i = 0; i < _uploadFTPManager.UploadBytesList.Count; i++) {
            BannerUploadBytesInfo bannerFilesInfo = _uploadFTPManager.UploadBytesList[i];
            string path = rootPath + "/" + bannerFilesInfo.uploadPath.Substring(0, bannerFilesInfo.uploadPath.LastIndexOf('/') + 1);
            string[] pathSplit = bannerFilesInfo.uploadPath.Split('/');
            string fileName = "";
            if (pathSplit != null && pathSplit.Length > 0) {
                fileName = pathSplit[pathSplit.Length - 1];
            } else {
                fileName = bannerFilesInfo.uploadPath;
            }

            if (!string.IsNullOrEmpty(fileName)) {
                BundleFile.WriteBytesToFilePath(bannerFilesInfo.uploadBytes, path, fileName);
            }
        }
    }

    void SetAppversionDropdown()
    {
        _appVerTypeDropdown.ClearOptions();
        List<string> contentDropDowns = new List<string>();
        if (_bannerToolSettingInfo.platformType == (int)BannerDefinitions.PlatformType.Android) {
            if (_androidAppversionList != null && _androidAppversionList.Count > 0) {
                for (int i = 0; i < _androidAppversionList.Count; i++) {
                    string appVer = _androidAppversionList[i];
                    contentDropDowns.Add(appVer);
                }
            }
        } else if (_bannerToolSettingInfo.platformType == (int)BannerDefinitions.PlatformType.iOS) {
            if (_iOSAppVersionList != null && _iOSAppVersionList.Count > 0) {
                for (int i = 0; i < _iOSAppVersionList.Count; i++) {
                    string appVer = _iOSAppVersionList[i];
                    contentDropDowns.Add(appVer);
                }
            }
        }

        if (contentDropDowns.Count > 0) {
            _platformMainTypeObj.gameObject.SetActive(true);
            _platformLangTypeObj.gameObject.SetActive(true);

            _appVerTypeDropdown.AddOptions(contentDropDowns);
            _appVerTypeDropdown.onValueChanged.RemoveAllListeners();
            _appVerTypeDropdown.value = 0;
            _appVerTypeDropdown.onValueChanged.AddListener(OnChangeAppverDropdown);
            SetSelectAppversion(0);
        } else {
            _platformMainTypeObj.gameObject.SetActive(false);
            _platformLangTypeObj.gameObject.SetActive(false);
        }
    }

    int GetPlatformVersion(InputField verInputField)
    {
        int version = -1;
        if (!int.TryParse(verInputField.text, out version)) {
            return version;
        }

        if (version > 0) {

            return version;
        }

        return version;
    }

    void SetSelectAppversion(int verIndex)
    {
        _selectAppversion = "";
        if (_bannerToolSettingInfo.platformType == (int)BannerDefinitions.PlatformType.Android) {
            _selectAppversion = _androidAppversionList[verIndex];
        } else if (_bannerToolSettingInfo.platformType == (int)BannerDefinitions.PlatformType.iOS) {
            _selectAppversion = _iOSAppVersionList[verIndex];
        }
    }

    void SetBaseBannerEnableState(BannerBaseToolFilesInfo baseToolFilesInfo, bool isEnable)
    {
        for (int i = 0; i < baseToolFilesInfo.commonList.Count; i++) {
            BannerImageToolInfo bannerToolInfo = baseToolFilesInfo.commonList[i];
            if(bannerToolInfo.boardState == BannerToolBoardState.Enable)
                bannerToolInfo.SetCheckBannerState(isEnable);
        }

        List<string> langKeys = baseToolFilesInfo.languageList.Keys.ToList();
        for (int i = 0; i < langKeys.Count; i++) {
            List<BannerImageToolInfo> langBannerToolInfos = baseToolFilesInfo.languageList[langKeys[i]];
            for (int j = 0; j < langBannerToolInfos.Count; j++) {
                BannerImageToolInfo bannerToolInfo = langBannerToolInfos[j];
                if (bannerToolInfo.boardState == BannerToolBoardState.Enable)
                    bannerToolInfo.SetCheckBannerState(isEnable);
            }
        }
    }

    void RefreshEnableBannerList(bool isEnable)
    {
        RefreshBaseEnableBanner(_bannerToolObjectListInfo, isEnable);

        List<string> platformKeys = _bannerToolObjectListInfo.platformBannerFilesInfos.Keys.ToList();
        for(int i = 0;i< platformKeys.Count; i++) {
            Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo> appVerBaseToolFilesInfos = _bannerToolObjectListInfo.platformBannerFilesInfos[platformKeys[i]];
            List<string> appVerKeys = appVerBaseToolFilesInfos.Keys.ToList();
            for(int j = 0;j< appVerKeys.Count; j++) {
                BannerBaseToolFilesInfo appVerBaseTool = appVerBaseToolFilesInfos[appVerKeys[j]];
                RefreshBaseEnableBanner(appVerBaseTool, isEnable);
            }
        }
    }

    void RefreshBaseEnableBanner(BannerBaseToolFilesInfo baseToolFilesInfo, bool isEnable)
    {
        for(int i = 0;i < baseToolFilesInfo.commonList.Count; i++) {
            BannerImageToolInfo bannerToolInfo = baseToolFilesInfo.commonList[i];
            if (bannerToolInfo.isViewState) {
                if (isEnable) {
                    if (bannerToolInfo.isCheckState) {
                        bannerToolInfo.bannerImgObj.gameObject.SetActive(true);
                    } else {
                        if (bannerToolInfo.boardState == BannerToolBoardState.Enable) {
                            bannerToolInfo.bannerImgObj.gameObject.SetActive(true);
                        } else {
                            bannerToolInfo.bannerImgObj.gameObject.SetActive(false);
                        }
                    }
                } else {
                    bannerToolInfo.bannerImgObj.gameObject.SetActive(true);
                }
            }
        }

        List<string> langKeys = baseToolFilesInfo.languageList.Keys.ToList();
        for(int i = 0;i < langKeys.Count; i++) {
            List<BannerImageToolInfo> langBannerToolInfos = baseToolFilesInfo.languageList[langKeys[i]];
            for(int j = 0;j< langBannerToolInfos.Count; j++) {
                BannerImageToolInfo bannerToolInfo = langBannerToolInfos[j];
                if (bannerToolInfo.isViewState) {
                    if (isEnable) {
                        if (bannerToolInfo.isCheckState) {
                            bannerToolInfo.bannerImgObj.gameObject.SetActive(true);
                        } else {
                            if (bannerToolInfo.boardState == BannerToolBoardState.Enable) {
                                bannerToolInfo.bannerImgObj.gameObject.SetActive(true);
                            } else {
                                bannerToolInfo.bannerImgObj.gameObject.SetActive(false);
                            }
                        }
                    } else {
                        bannerToolInfo.bannerImgObj.gameObject.SetActive(true);
                    }
                }
            }
        }
    }

    void AddSupportLangBoard(LanguageType langType, bool isEnable)
    {
        BannerToolSupportLang supportLang = Instantiate<BannerToolSupportLang>(_supportLangBoard);
        supportLang.CheckToggle.isOn = isEnable;
        supportLang.LangNameText.text = langType.ToString();
        supportLang.transform.SetParent(_supportLangContent);
        supportLang.transform.localScale = Vector3.one;
        supportLang.gameObject.SetActive(true);

        _supportLangList.Add(supportLang);
    }

    List<string> GetSupportLangList()
    {
        List<string> retValue = new List<string>();

        for (int i = 0; i < _supportLangList.Count; i++) {
            if (_supportLangList[i].CheckToggle.isOn) {
                retValue.Add(_supportLangList[i].LangNameText.text);
            }
        }

        return retValue;
    }

    void UploadBannerList()
    {
        int inputVersion = GetPlatformVersion(_versionInputField);

        SaveBannerSettingInfo();

        List<BannerImageToolInfo> uploadList = GetUploadBannerListNew();

        _uploadFTPManager.InitAssetFTPUploader();

        _bannerToolObjectListInfo.version = inputVersion;

        _bannerToolObjectListInfo.androidAppver = GetPlatformVersion(_androidAppversionInput);
        _bannerToolObjectListInfo.iOSAppver = GetPlatformVersion(_iosAppversionInput);

        _bannerToolObjectListInfo.supportLangList.Clear();
        List<string> supportLangList = GetSupportLangList();
        for (int i = 0; i < supportLangList.Count; i++) {
            _bannerToolObjectListInfo.supportLangList.Add(supportLangList[i]);
        }

        _bannerToolUploadByteInfo = GetBannerImageToolFilesBytesInfo();

        byte[] jsonBytes = _bannerToolUploadByteInfo.uploadBytes;

        _uploadFTPManager.UploadBytesList.Add(_bannerToolUploadByteInfo);
        _uploadFTPManager.UploadBytesList.Add(GetAndroidBannerFilesBytesInfoNew());
        _uploadFTPManager.UploadBytesList.Add(GetiOSBannerFilesBytesInfoNew());

        if(GetBuildKind() == BannerHelper.uploadPlatformType) {
            string platformStr = ((BannerDefinitions.PlatformType)_bannerToolSettingInfo.uploadPlatform).ToString();
            int uploadAppver = 0;
            if((BannerDefinitions.PlatformType)_bannerToolSettingInfo.uploadPlatform == BannerDefinitions.PlatformType.Android) {
                uploadAppver = _bannerToolObjectListInfo.androidAppver;
            } else if((BannerDefinitions.PlatformType)_bannerToolSettingInfo.uploadPlatform == BannerDefinitions.PlatformType.iOS) {
                uploadAppver = _bannerToolObjectListInfo.iOSAppver;
            }
            
            string platformRootPath = BannerToolUtil.GetBannerSettingInfoRootPath() + GetBuildKind() + string.Format("/{0}/{1}", platformStr, uploadAppver.ToString());

            string bannerToolPath = platformRootPath + "/" + BannerToolUtil.bannerToolFilesInfoName;
            BannerFile.WriteBytesFile(jsonBytes, bannerToolPath);

            WriteBannerFilesInfo(platformRootPath);

            string rootPath = string.Format(BannerHelper.rootFTPPath, GetBuildKind() + string.Format("/{0}/{1}", platformStr, uploadAppver.ToString()));
            _uploadFTPManager.UploadSFtpBannerImgList(rootPath, uploadList, _ftpURLInputField.text, _ftpIDInputField.text);

            WritePurgeList(platformRootPath, _webURL, rootPath, uploadList);
        } else {
            string bannerToolPath = BannerToolUtil.GetBannerSettingInfoRootPath() + GetBuildKind() + "/" + BannerToolUtil.bannerToolFilesInfoName;
            BannerFile.WriteBytesFile(jsonBytes, bannerToolPath);

            WriteBannerFilesInfo(BannerToolUtil.GetBannerSettingInfoRootPath() + GetBuildKind());

            string rootPath = string.Format(BannerHelper.rootFTPPath, GetBuildKind());
            _uploadFTPManager.UploadSFtpBannerImgList(rootPath, uploadList, _ftpURLInputField.text, _ftpIDInputField.text);

            WritePurgeList(BannerToolUtil.GetBannerSettingInfoRootPath() + GetBuildKind(), _webURL, rootPath, uploadList);
        }
    }

    void WritePurgeList(string platformRootPath, string ftpURL, string ftpUploadBannerPath, List<BannerImageToolInfo> uploadList)
    {
        string ftpPath = ftpURL + ftpUploadBannerPath + '/';

        StringBuilder sb = new StringBuilder();
        for(int i = 0;i< _uploadFTPManager.UploadBytesList.Count; i++) {
            BannerUploadBytesInfo uploadBytesInfo = _uploadFTPManager.UploadBytesList[i];
            string ftpUri = ftpPath + uploadBytesInfo.uploadPath;
            sb.Append(ftpUri);
            sb.Append("\n");
        }

        if (uploadList != null && uploadList.Count > 0) {
            for (int i = 0; i < uploadList.Count; i++) {
                BannerImageToolInfo bannerToolInfo = uploadList[i];
                if (bannerToolInfo.boardState == BannerToolBoardState.Enable) {
                    string ftpUri = ftpPath + bannerToolInfo.uploadPath;
                    sb.Append(ftpUri);
                    sb.Append("\n");
                }
            }
        }

        string writePurgeStr = sb.ToString();
        byte[] writePurgeBytes = Encoding.UTF8.GetBytes(writePurgeStr);
        BundleFile.WriteBytesToFilePath(writePurgeBytes, platformRootPath + "/", "BannerPurgeList.txt");
    }

    string GetBannerImgBranchesPath()
    {
        string rootProjectPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
        rootProjectPath = rootProjectPath.Substring(0, rootProjectPath.LastIndexOf('/'));
        return rootProjectPath.Substring(0, rootProjectPath.LastIndexOf('/') + 1) + "BannerImgBranches/";
    }

    void CopyBannerListFilesToBranches()
    {
        int inputVersion = GetPlatformVersion(_versionInputField);
        if (inputVersion < 0)
            inputVersion = 0;
        string versionPath = string.Format("/version_{0}", inputVersion);
        string destPath = GetBannerImgBranchesPath() + GetBuildKind() + versionPath;
        BundleFile.DeleteFileAndDirectory(destPath);
        for (int i = 0; i < _uploadFTPManager.UploadBytesList.Count; i++) {
            BannerUploadBytesInfo uploadBytesInfo = _uploadFTPManager.UploadBytesList[i];
            string path = destPath + "/" + uploadBytesInfo.uploadPath.Substring(0, uploadBytesInfo.uploadPath.LastIndexOf('/') + 1);
            string[] pathSplit = uploadBytesInfo.uploadPath.Split('/');
            string fileName = "";
            if (pathSplit != null && pathSplit.Length > 0) {
                fileName = pathSplit[pathSplit.Length - 1];
            } else {
                fileName = uploadBytesInfo.uploadPath;
            }

            if (!string.IsNullOrEmpty(fileName)) {
                BundleFile.WriteBytesToFilePath(uploadBytesInfo.uploadBytes, path, fileName);
            }
        }

        List<BannerImageToolInfo> bannerAllToolInfos = GetUploadBannerListNew(true);
        for(int i = 0;i< bannerAllToolInfos.Count; i++) {
            BannerImageToolInfo bannerInfo = bannerAllToolInfos[i];
            string destFile = destPath + "/" + bannerInfo.uploadPath;
            
            string destFilePath = destFile.Substring(0, destFile.LastIndexOf('/') + 1);
            if (!Directory.Exists(destFilePath)) {
                Directory.CreateDirectory(destFilePath);
            }

            File.Copy(bannerInfo.localPathName, destFile);
        }
    }

    void CopyBannerListFilesToUploadPlatformBranches()
    {
        string platformStr = ((BannerDefinitions.PlatformType)_bannerToolSettingInfo.uploadPlatform).ToString();
        int uploadAppver = 0;
        if ((BannerDefinitions.PlatformType)_bannerToolSettingInfo.uploadPlatform == BannerDefinitions.PlatformType.Android) {
            uploadAppver = GetPlatformVersion(_androidAppversionInput);
        } else if ((BannerDefinitions.PlatformType)_bannerToolSettingInfo.uploadPlatform == BannerDefinitions.PlatformType.iOS) {
            uploadAppver = GetPlatformVersion(_iosAppversionInput);
        }

        string platformPath = string.Format("/{0}/{1}", platformStr, uploadAppver);
        string destPath = GetBannerImgBranchesPath() + GetBuildKind() + platformPath;
        BundleFile.DeleteFileAndDirectory(destPath);
        for (int i = 0; i < _uploadFTPManager.UploadBytesList.Count; i++) {
            BannerUploadBytesInfo uploadBytesInfo = _uploadFTPManager.UploadBytesList[i];
            string path = destPath + "/" + uploadBytesInfo.uploadPath.Substring(0, uploadBytesInfo.uploadPath.LastIndexOf('/') + 1);
            string[] pathSplit = uploadBytesInfo.uploadPath.Split('/');
            string fileName = "";
            if (pathSplit != null && pathSplit.Length > 0) {
                fileName = pathSplit[pathSplit.Length - 1];
            } else {
                fileName = uploadBytesInfo.uploadPath;
            }

            if (!string.IsNullOrEmpty(fileName)) {
                BundleFile.WriteBytesToFilePath(uploadBytesInfo.uploadBytes, path, fileName);
            }
        }

        List<BannerImageToolInfo> bannerAllToolInfos = GetUploadBannerListNew(true);
        for (int i = 0; i < bannerAllToolInfos.Count; i++) {
            BannerImageToolInfo bannerInfo = bannerAllToolInfos[i];
            string destFile = destPath + "/" + bannerInfo.uploadPath;

            string destFilePath = destFile.Substring(0, destFile.LastIndexOf('/') + 1);
            if (!Directory.Exists(destFilePath)) {
                Directory.CreateDirectory(destFilePath);
            }

            File.Copy(bannerInfo.localPathName, destFile);
        }
    }

    void SetUploadPlatformDropdown()
    {
        _uploadPlatformDropdown.ClearOptions();

        List<string> contentDropDowns = new List<string>();
        for (int i = 0; i < (int)BannerDefinitions.PlatformType.Max; i++) {
            contentDropDowns.Add(((BannerDefinitions.PlatformType)i).ToString());
        }

        _uploadPlatformDropdown.AddOptions(contentDropDowns);
        _uploadPlatformDropdown.onValueChanged.RemoveAllListeners();
        _uploadPlatformDropdown.value = _bannerToolSettingInfo.uploadPlatform;
        _uploadPlatformDropdown.onValueChanged.AddListener(OnChangeUploadPlatformDropdown);
    }

    #endregion

    #region CallBack Methods

    void OnSelectBannerBoardNew(BannerImageToolInfo bannerToolInfo)
    {
        if (_curSelectBannerToolInfo != null) {
            if (_curSelectBannerToolInfo == bannerToolInfo)
                return;

            _curSelectBannerToolInfo.bannerImgObj.SelectImgObj.SetActive(false);
        }

        _curSelectBannerToolInfo = bannerToolInfo;
        _curSelectBannerToolInfo.bannerImgObj.SelectImgObj.SetActive(true);

        byte[] byteTexture = BundleFile.ReadBytesFromFilePath(_curSelectBannerToolInfo.localPathName);
        if (byteTexture != null && byteTexture.Length > 0) {
            Texture2D texture = new Texture2D(0, 0);
            texture.LoadImage(byteTexture);

            Sprite spr = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            Vector2.zero);

            float defaultWidth = 500f;
            float defaultHeight = 300f;

            RectTransform bannerRectTrans = _bannerImg.gameObject.GetComponent<RectTransform>();

            float calcWidth = 0f;
            float calcHeight = 0f;
            if (defaultWidth / defaultHeight < texture.width / texture.height) {
                calcWidth = defaultWidth;
                calcHeight = texture.height * (defaultWidth / texture.width);
            } else {
                calcWidth = texture.width * (defaultHeight / texture.height);
                calcHeight = defaultHeight;
            }

            bannerRectTrans.sizeDelta = new Vector2(calcWidth, calcHeight);

            _bannerImg.sprite = spr;

            _bannerImg.gameObject.SetActive(true);
        }

        _bannerMainTypeText.text = ((BannerDefinitions.MainType)bannerToolInfo.mainType).ToString();
        if (bannerToolInfo.mainType == (int)BannerDefinitions.MainType.common) {
            _languageTypeText.text = "";
        } else if (bannerToolInfo.mainType == (int)BannerDefinitions.MainType.Language) {
            _languageTypeText.text = ((LanguageType)bannerToolInfo.langType).ToString();
        } else if (bannerToolInfo.mainType == (int)BannerDefinitions.MainType.platform) {
            if (bannerToolInfo.platformMainType == (int)BannerDefinitions.MainType.Language) {
                _languageTypeText.text = ((LanguageType)bannerToolInfo.platformLangType).ToString();
            } else {
                _languageTypeText.text = "";
            }
        }

        _bannerNameText.text = bannerToolInfo.bannerName;
        _bannerPathText.text = bannerToolInfo.bannerPathName;
        _bannerLocalPathText.text = bannerToolInfo.localPathName;
        _bannerFileSizeText.text = bannerToolInfo.fileSize.ToString();
    }

    void OnCheckBannerBoardNew(bool checkValue, BannerImageToolInfo bannerToolInfo)
    {
        bannerToolInfo.isCheckState = checkValue;
    }

    void OnUploadBannerListNew()
    {
        if (_bannerStep != BannerDefinitions.BannerToolManagerStep.Manage)
            return;

        int inputVersion = GetPlatformVersion(_versionInputField);
        if (inputVersion == -1) {
            _toolInfoPopup.ShowConfirmPopup("Version 입력칸에 양수값만 입력해주세요.");
            return;
        }

        if (string.IsNullOrEmpty(_ftpURLInputField.text)) {
            _toolInfoPopup.ShowConfirmPopup("FTP URL 입력칸에 주소를 입력해주세요.");
            return;
        }

        if (string.IsNullOrEmpty(_ftpIDInputField.text)) {
            _toolInfoPopup.ShowConfirmPopup("FTP ID 입력칸에 ID값을 입력해주세요.");
            return;
        }

        _uploadFTPManager.SftpKeyFile = string.Format("{0}.pem", _ftpIDInputField.text);


        if (GetBuildKind() == "LIVE") {
            _toolInfoPopup.ShowYesNoPopup("LIVE Banner 이미지 업로드 진행하시겠습니까?", OnUploadBannerList);
            return;
        }

        UploadBannerList();

        if (GetBuildKind() == BannerHelper.uploadPlatformType) {
            CopyBannerListFilesToUploadPlatformBranches();
        } else {
            CopyBannerListFilesToBranches();
        }
    }

    void OnUploadBannerList()
    {
        UploadBannerList();

        if (GetBuildKind() == BannerHelper.uploadPlatformType) {
            CopyBannerListFilesToUploadPlatformBranches();
        } else {
            CopyBannerListFilesToBranches();
        }
    }

    void OnChangeBuildKindDropdown(int value)
    {
        _bannerToolSettingInfo.buildKind = value;
        _serverBannerToolListInfo = null;
        _serverCompareBannerToolInfos = null;
        _bannerToolUploadByteInfo = null;
        LoadAllBannerImgListNew();
        _curSelectBannerToolInfo = null;
        SetMainTypeNew((BannerDefinitions.MainType)_bannerToolSettingInfo.mainType);
        SetBannerStep(BannerDefinitions.BannerToolManagerStep.DownloadBannerFilesInfoNew);
        SaveBannerSettingInfo();

        if (GetBuildKind() == BannerHelper.uploadPlatformType) {
            _platformAppverObj.SetActive(true);
        } else {
            _platformAppverObj.SetActive(false);
        }
    }

    void OnChangeMainTypeDropdown(int value)
    {
        SetMainTypeNew((BannerDefinitions.MainType)value);
        SaveBannerSettingInfo();
    }

    void OnChangePlatformMainTypeDropdown(int value)
    {
        SetPlatformMainTypeNew((BannerDefinitions.MainType)value);
        SaveBannerSettingInfo();
    }

    void OnChangeAppverDropdown(int value)
    {
        SetSelectAppversion(value);

        SetPlatformMainTypeNew((BannerDefinitions.MainType)_bannerToolSettingInfo.platformMainType);
    }

    void OnChangeLangTypeDropdown(int value)
    {
        _bannerToolSettingInfo.languageType = value + 1;
        SetLangBannerObjList((LanguageType)_bannerToolSettingInfo.languageType);

        SaveBannerSettingInfo();
    }

    void OnChangePlatformLangTypeDropdown(int value)
    {
        _bannerToolSettingInfo.platformLangType = value + 1;

        BannerBaseToolFilesInfo bannerBaseFilesInfo = GetPlatformBannerObjList((BannerDefinitions.PlatformType)_bannerToolSettingInfo.platformType, _appVerTypeDropdown.value);
        SetPlatformLangBannerObjList(bannerBaseFilesInfo, (LanguageType)_bannerToolSettingInfo.platformLangType);

        SaveBannerSettingInfo();
    }

    void OnChangePlatformDropdown(int value)
    {
        _bannerToolSettingInfo.platformType = value;

        SetAppversionDropdown();
        SetPlatformMainTypeNew((BannerDefinitions.MainType)_bannerToolSettingInfo.platformMainType);

        SaveBannerSettingInfo();
    }

    void OnAllEnableListNew()
    {
        SetBaseBannerEnableState(_bannerToolObjectListInfo, true);

        List<string> platformKeys = _bannerToolObjectListInfo.platformBannerFilesInfos.Keys.ToList();
        for (int i = 0; i < platformKeys.Count; i++) {
            Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo> appVerFilesInfos = _bannerToolObjectListInfo.platformBannerFilesInfos[platformKeys[i]];
            List<string> appVerKeys = appVerFilesInfos.Keys.ToList();
            for (int j = 0; j < appVerKeys.Count; j++) {
                BannerBaseToolFilesInfo bannerBaseFilesInfo = appVerFilesInfos[appVerKeys[j]];
                SetBaseBannerEnableState(bannerBaseFilesInfo, true);
            }
        }
    }

    void OnAllDisableListNew()
    {
        SetBaseBannerEnableState(_bannerToolObjectListInfo, false);

        List<string> platformKeys = _bannerToolObjectListInfo.platformBannerFilesInfos.Keys.ToList();
        for (int i = 0; i < platformKeys.Count; i++) {
            Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo> appVerFilesInfos = _bannerToolObjectListInfo.platformBannerFilesInfos[platformKeys[i]];
            List<string> appVerKeys = appVerFilesInfos.Keys.ToList();
            for (int j = 0; j < appVerKeys.Count; j++) {
                BannerBaseToolFilesInfo bannerBaseFilesInfo = appVerFilesInfos[appVerKeys[j]];
                SetBaseBannerEnableState(bannerBaseFilesInfo, false);
            }
        }
    }

    void OnCompleteBannerInfoNew(string filePath, UnityWebRequest webRequest)
    {
        byte[] data = webRequest.downloadHandler.data;
        string result = Encoding.UTF8.GetString(data);

        _serverBannerToolListInfo = BannerToolUtil.GetBannerImgToolFilesInfoByJson(result);
        if(_serverBannerToolListInfo != null) {
            _serverCompareBannerToolInfos = BannerToolUtil.GetCompareBannerImageToolInfo(_serverBannerToolListInfo);
        }

        RefreshBoardState();
        RefreshEnableBannerList(_changedListToggle.isOn);
        RefreshSupportLangList();

        SetBannerStep(BannerDefinitions.BannerToolManagerStep.Manage);
    }

    void OnFailBannerInfoNew(string filePath, string errorLog)
    {
        _serverBannerToolListInfo = null;
        _serverCompareBannerToolInfos = null;

        RefreshBoardState();
        RefreshEnableBannerList(_changedListToggle.isOn);

        SetBannerStep(BannerDefinitions.BannerToolManagerStep.Manage);
    }

    void OnChangedListToggle(bool value)
    {
        if (value) {
            _bannerToolSettingInfo.changedListView = 1;
        } else {
            _bannerToolSettingInfo.changedListView = 0;
        }

        SaveBannerSettingInfo();

        RefreshEnableBannerList(value);
    }

    void OnFTPUploadState(BannerFTPUploadState ftpUploadState)
    {
        switch (ftpUploadState) {
            case BannerFTPUploadState.FTPUploadStart:
                break;
            case BannerFTPUploadState.FTPUploading:
                break;
            case BannerFTPUploadState.FTPUploadFinish:
                if (_bannerToolUploadByteInfo != null) {
                    string uploadBannerToolJson = Encoding.UTF8.GetString(_bannerToolUploadByteInfo.uploadBytes);
                    _serverBannerToolListInfo = BannerToolUtil.GetBannerImgToolFilesInfoByJson(uploadBannerToolJson);
                    if (_serverBannerToolListInfo != null) {
                        _serverCompareBannerToolInfos = BannerToolUtil.GetCompareBannerImageToolInfo(_serverBannerToolListInfo);
                    }

                    _bannerToolUploadByteInfo = null;

                    RefreshBoardState();
                    RefreshEnableBannerList(_changedListToggle.isOn);
                    RefreshSupportLangList();
                }
                break;
        }
    }

    void OnChangeUploadPlatformDropdown(int value)
    {
        _bannerToolSettingInfo.uploadPlatform = value;
        SaveBannerSettingInfo();

        _serverBannerToolListInfo = null;
        _serverCompareBannerToolInfos = null;
        _bannerToolUploadByteInfo = null;
        LoadAllBannerImgListNew();
        _curSelectBannerToolInfo = null;
        SetMainTypeNew((BannerDefinitions.MainType)_bannerToolSettingInfo.mainType);
        SetBannerStep(BannerDefinitions.BannerToolManagerStep.DownloadBannerFilesInfoNew);
    }

    #endregion
}

#endif
