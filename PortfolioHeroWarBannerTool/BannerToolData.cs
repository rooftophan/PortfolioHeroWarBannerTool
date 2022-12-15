using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BannerToolBoardState
{
    New,
    ExistSame,
    Enable,
}

public class BannerToolSettingInfo
{
    public string ftpURL;
    public string ftpID;
    public int buildKind;
    public int mainType;
    public int languageType;
    public int version = 1;
    public int androidAppver = 1;
    public int iOSAppver = 1;
    public int uploadPlatform;
    public int platformType;
    public int platformMainType;
    public int platformLangType;
    public int changedListView; // 0 : Hide, 1 : View

    public void InitBannerToolSetInfo()
    {
        buildKind = 0;
        mainType = (int)BannerDefinitions.MainType.All;
        languageType = (int)LanguageType.ko;
        version = 1;
        androidAppver = 1;
        iOSAppver = 1;
        uploadPlatform = (int)BannerDefinitions.PlatformType.Android;
        platformType = (int)BannerDefinitions.PlatformType.Android;
        platformMainType = (int)BannerDefinitions.MainType.All;
        platformLangType = (int)LanguageType.ko;
        changedListView = 0;
    }
}

public class BannerImageToolInfo
{
    public BannerToolBoardState boardState;

    public int langType;
    public int mainType;
    public int platformType;
    public string platformAppversion;
    public int platformMainType;
    public int platformLangType;

    public string localPathName;

    public string originBannerPathName;

    public string bannerName;
    public string uploadPath;
    public string bannerPathName;
    public long fileSize;
    public string uploadDate;
    public string md5Hash;

    public long savefileSize;
    public string saveUploadDate;
    public string saveMD5Hash;

    public bool isCheckState;
    public BannerToolImgBoard bannerImgObj;

    public string compareBannerKey;

    public bool isViewState;

    public void SetCheckBannerState(bool bannerState)
    {
        isCheckState = bannerState;
        bannerImgObj.CheckToggle.isOn = bannerState;
    }

    public void SetViewState(bool isView)
    {
        isViewState = isView;
        bannerImgObj.gameObject.SetActive(isView);
    }

    public void SetBoardState(BannerToolBoardState bannerBoardState)
    {
        boardState = bannerBoardState;
        switch (boardState) {
            case BannerToolBoardState.New:
                isCheckState = true;
                if (bannerImgObj != null) {
                    bannerImgObj.NewObject.SetActive(true);
                    bannerImgObj.ExistSameObject.SetActive(false);
                    bannerImgObj.CheckToggle.gameObject.SetActive(false);
                }
                break;
            case BannerToolBoardState.ExistSame:
                isCheckState = false;
                if (bannerImgObj != null) {
                    bannerImgObj.NewObject.SetActive(false);
                    bannerImgObj.ExistSameObject.SetActive(true);
                    bannerImgObj.CheckToggle.gameObject.SetActive(false);
                }
                break;
            case BannerToolBoardState.Enable:
                if (bannerImgObj != null) {
                    bannerImgObj.NewObject.SetActive(false);
                    bannerImgObj.ExistSameObject.SetActive(false);
                    bannerImgObj.CheckToggle.gameObject.SetActive(true);
                }
                break;
        }
    }
}

public class BannerBaseToolFilesInfo
{
    public List<BannerImageToolInfo> commonList = new List<BannerImageToolInfo>();
    public Dictionary<string /* LanguageType */, List<BannerImageToolInfo>> languageList = new Dictionary<string, List<BannerImageToolInfo>>();

    public virtual void InitBannerFilesInfo()
    {
        InitBannerBaseInfo(this);
    }

    public void CopyBannerBaseToolFilesInfo(BannerBaseToolFilesInfo baseToolFilesInfo)
    {
        for(int i = 0;i< baseToolFilesInfo.commonList.Count; i++) {
            this.commonList.Add(baseToolFilesInfo.commonList[i]);
        }

        List<string> langKeys = baseToolFilesInfo.languageList.Keys.ToList();
        for(int i = 0;i< langKeys.Count; i++) {
            List<BannerImageToolInfo> bannerInfos = baseToolFilesInfo.languageList[langKeys[i]];
            List<BannerImageToolInfo> inputBannerInfos = new List<BannerImageToolInfo>();
            this.languageList.Add(langKeys[i], inputBannerInfos);
            for (int j = 0;j< bannerInfos.Count; j++) {
                inputBannerInfos.Add(bannerInfos[j]);
            }
        }
    }

    public void InitBannerBaseInfo(BannerBaseToolFilesInfo baseToolFilesInfo)
    {
        for (int i = 0; i < baseToolFilesInfo.commonList.Count; i++) {
            BannerImageToolInfo bannerToolInfo = baseToolFilesInfo.commonList[i];
            if (bannerToolInfo.bannerImgObj != null)
                GameObject.Destroy(bannerToolInfo.bannerImgObj.gameObject);
        }

        baseToolFilesInfo.commonList.Clear();

        List<string> langKeys = baseToolFilesInfo.languageList.Keys.ToList();
        for (int i = 0; i < langKeys.Count; i++) {
            List<BannerImageToolInfo> bannerToolList = baseToolFilesInfo.languageList[langKeys[i]];
            for (int j = 0; j < bannerToolList.Count; j++) {
                BannerImageToolInfo bannerToolInfo = bannerToolList[j];
                if (bannerToolInfo.bannerImgObj != null)
                    GameObject.Destroy(bannerToolInfo.bannerImgObj.gameObject);
            }
        }

        baseToolFilesInfo.languageList.Clear();
    }
}

public class BannerImgToolFilesInfo : BannerBaseToolFilesInfo
{
    public int version;
    public int androidAppver;
    public int iOSAppver;
    public List<string> supportLangList = new List<string>();
    public Dictionary<string /* Platform */, Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo>> platformBannerFilesInfos = new Dictionary<string, Dictionary<string, BannerBaseToolFilesInfo>>();

    public override void InitBannerFilesInfo()
    {
        base.InitBannerFilesInfo();

        List<string> platformKeys = platformBannerFilesInfos.Keys.ToList();
        for(int i = 0;i< platformKeys.Count; i++) {
            Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo> appVerFilesInfos = platformBannerFilesInfos[platformKeys[i]];
            List<string> appVerKeys = appVerFilesInfos.Keys.ToList();
            for(int j =0;j< appVerKeys.Count; j++) {
                BannerBaseToolFilesInfo baseToolFilesInfo = appVerFilesInfos[appVerKeys[j]];
                InitBannerBaseInfo(baseToolFilesInfo);
            }
        }

        platformBannerFilesInfos.Clear();
    }

    public void CopyBannerFilesInfo(BannerImgToolFilesInfo copyFilesInfo)
    {
        CopyBannerBaseToolFilesInfo(copyFilesInfo);

        List<string> platformKeys = copyFilesInfo.platformBannerFilesInfos.Keys.ToList();
        for (int i = 0; i < platformKeys.Count; i++) {
            Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo> appVerFilesInfos = copyFilesInfo.platformBannerFilesInfos[platformKeys[i]];
            Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo> inputAppVerFilesInfos = new Dictionary<string, BannerBaseToolFilesInfo>();
            platformBannerFilesInfos.Add(platformKeys[i], inputAppVerFilesInfos);
            List<string> appVerKeys = appVerFilesInfos.Keys.ToList();
            for (int j = 0; j < appVerKeys.Count; j++) {
                BannerBaseToolFilesInfo baseToolFilesInfo = appVerFilesInfos[appVerKeys[j]];
                BannerBaseToolFilesInfo inputBaseToolFilesInfo = new BannerBaseToolFilesInfo();
                inputBaseToolFilesInfo.CopyBannerBaseToolFilesInfo(baseToolFilesInfo);
                inputAppVerFilesInfos.Add(appVerKeys[j], inputBaseToolFilesInfo);
            }
        }
    }
}

public class BannerCompareFilesInfo
{
    public int version;
    public Dictionary<string, BannerImageInfo> commonList = new Dictionary<string, BannerImageInfo>();
    public Dictionary<string /* LanguageType */, Dictionary<string, BannerImageInfo>> languageList = new Dictionary<string, Dictionary<string, BannerImageInfo>>();
    public Dictionary<string /* AppVersion */, BannerCompareFilesInfo> appVersionList = new Dictionary<string, BannerCompareFilesInfo>();
}

public class BannerUploadBytesInfo
{
    public string uploadPath;
    public byte[] uploadBytes;
}