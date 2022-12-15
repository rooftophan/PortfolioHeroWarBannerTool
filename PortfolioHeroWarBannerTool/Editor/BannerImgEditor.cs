using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using LitJson;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;

public class BannerImgEditor : EditorWindow
{
    [MenuItem("Build/MakeBannerFilesInfo")]
    public static void MakeBannerFilesInfo()
    {
        _bannerPath = Application.dataPath + BannerHelper.originBannerPath;

        BannerImageFilesInfo bannerFilesInfo = LoadLocalBannerImgInfo();

        byte[] bannerInfoBytes = GetBannerFilesBytes(bannerFilesInfo);

        BundleFile.WriteBytesToFilePath(bannerInfoBytes, _bannerPath, BannerHelper.localBannerFilesFile);
    }

    #region Variables

    static string _bannerPath;

    #endregion

    #region Methods

    static BannerImageFilesInfo LoadLocalBannerImgInfo()
    {
        BannerImageFilesInfo retValue = new BannerImageFilesInfo();

        string[] bannerFiles = Directory.GetFiles(_bannerPath, "*.*", SearchOption.AllDirectories);

        if (bannerFiles != null && bannerFiles.Length > 0) {
            for (int i = 0; i < bannerFiles.Length; i++) {
                string localPathName = bannerFiles[i];

                int fileLength = localPathName.Length;
                if (fileLength > 5) {
                    string lastName = localPathName.Substring(fileLength - 5, 5);
                    if (lastName == ".meta") {
                        continue;
                    }
                }

                //Debug.Log(string.Format("LoadAllBannerImgList filePathName : {0}", localPathName));
                string bannerPathName = localPathName.Replace(_bannerPath, "").Replace('\\', '/');
                string[] bannerSplits = bannerPathName.Split('/');
                string bannerName = "";
                if (bannerSplits != null && bannerSplits.Length > 0) {
                    bannerName = bannerSplits[bannerSplits.Length - 1];
                } else {
                    bannerName = bannerPathName;
                }
                int bannerLangType = GetBannerLanguageType(bannerPathName);
                BannerDefinitions.MainType bannerMainType = BannerDefinitions.MainType.All;

                if (bannerLangType != -1) {
                    bannerMainType = BannerDefinitions.MainType.Language;
                } else {
                    string[] bannerPathSplit = bannerPathName.Split('/');
                    if (bannerPathSplit != null && bannerPathSplit.Length > 0) {
                        if (bannerPathSplit[0] == BannerHelper.commonPathName) {
                            bannerMainType = BannerDefinitions.MainType.common;
                        }
                    }
                }

                BannerImageInfo inputBannerInfo = new BannerImageInfo();

                string uploadDate = string.Format("{0}{1:D2}{2:D2}{3:D2}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Minute);
                inputBannerInfo.bannerName = GetBannerName(bannerPathName);
                inputBannerInfo.uploadPath = GetUploadPathName(bannerPathName, uploadDate);
                inputBannerInfo.bannerPathName = bannerPathName;
                inputBannerInfo.fileSize = IOUtil.GetFileSize(localPathName);
                inputBannerInfo.uploadDate = uploadDate;
                byte[] imgBytes = BannerFile.ReadBytesByPath(localPathName);
                inputBannerInfo.md5Hash = StringEncription.MakeMD5ByBytes(imgBytes);

                if(bannerMainType == BannerDefinitions.MainType.common) {
                    if (retValue.commonList == null)
                        retValue.commonList = new List<BannerImageInfo>();

                    retValue.commonList.Add(inputBannerInfo);
                } else if (bannerMainType == BannerDefinitions.MainType.Language) {
                    if(retValue.languageList == null) {
                        retValue.languageList = new Dictionary<string, List<BannerImageInfo>>();
                    }

                    string langKey = ((LanguageType)bannerLangType).ToString();
                    List<BannerImageInfo> langBannerInfos = null;
                    if (retValue.languageList.ContainsKey(langKey)) {
                        langBannerInfos = retValue.languageList[langKey];
                    } else {
                        langBannerInfos = new List<BannerImageInfo>();
                        retValue.languageList.Add(langKey, langBannerInfos);
                    }

                    langBannerInfos.Add(inputBannerInfo);
                }

            }

        }

        return retValue;
    }

    static int GetBannerLanguageType(string bannerPathName)
    {
        string[] bannerPathSplit = bannerPathName.Split('/');

        if (bannerPathSplit == null || bannerPathSplit.Length == 0)
            return -1;

        int maxLang = 16;
        for (int i = 1; i < maxLang; i++) {
            LanguageType langType = (LanguageType)i;
            if (bannerPathSplit[0] == langType.ToString()) {
                //Debug.Log(string.Format("GetBannerLanguageType langType : {0}", langType));
                return (int)langType;
            }
        }

        return -1;
    }

    static string GetBannerName(string bannerPathName)
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
        if (bannerSplit != null && bannerSplit.Length > 0) {
            return bannerSplit[bannerSplit.Length - 1];
        } else {
            return bannerPathName;
        }
    }

    static string GetUploadPathName(string bannerPathName, string uploadDate)
    {
        string retValue = "";
        string[] pathSplit = bannerPathName.Split('/');
        if (pathSplit != null && pathSplit.Length > 1) {
            retValue = string.Format("{0}/", pathSplit[0]);
            for (int i = 1; i < pathSplit.Length; i++) {
                if (i == 1) {
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

    static byte[] GetBannerFilesBytes(BannerImageFilesInfo bannerFilesInfo)
    {
        string str = JsonMapper.ToJson(bannerFilesInfo);

        return Encoding.UTF8.GetBytes(str);
    }

    #endregion
}

#endif
