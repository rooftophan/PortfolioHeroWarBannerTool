using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class BannerToolUtil
{
    public const string bannerToolFilesInfoName = "BannerToolFilesInfo.bytes";

    public static string GetBannerSettingInfoRootPath()
    {
        string rootProjectPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
        rootProjectPath = rootProjectPath.Substring(0, rootProjectPath.LastIndexOf('/'));
        return rootProjectPath.Substring(0, rootProjectPath.LastIndexOf('/') + 1) + "BannerSettingInfo/";
    }

    public static string GetSFTPRootPath()
    {
        string rootProjectPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
        rootProjectPath = rootProjectPath.Substring(0, rootProjectPath.LastIndexOf('/'));
        return rootProjectPath.Substring(0, rootProjectPath.LastIndexOf('/') + 1) + "heroeswarsftpkey/";
    }

    public static T LoadFileObjectInfo<T>(string path, string fileName) where T : class
    {
        T retValue = null;

        try {
            string loadFile = BundleFile.ReadStringFromFilePath(path, fileName);
            if (!string.IsNullOrEmpty(loadFile)) {
                retValue = JsonMapper.ToObject<T>(loadFile);
            }
        } catch (Exception ex) {
            Debug.Log(string.Format("LoadJsonObjectInfo exception : {0}", ex));
        }

        return retValue;
    }

    public static T LoadFileObjectInfoByJson<T>(string jsonString) where T : class
    {
        T retValue = null;

        try {
            if (!string.IsNullOrEmpty(jsonString)) {
                retValue = JsonMapper.ToObject<T>(jsonString);
            }
        } catch (Exception ex) {
            Debug.Log(string.Format("LoadFileObjectInfoByJson exception : {0}", ex));
        }

        return retValue;
    }

    public static void WriteObjectFile<T>(string path, string fileName, T objectInfo) where T : class
    {
        if (objectInfo == null)
            return;

        try {
            BundleFile.WriteStringToFilePath(JsonMapper.ToJson(objectInfo), path, fileName);
        } catch (Exception ex) {
            Debug.Log(string.Format("WriteObjectJson exception : {0}", ex));
        }
    }

    public static string GetBannerImgFilesInfoJsonString(BannerImageFilesInfo bannerFilesInfo)
    {
        StringBuilder sb = new StringBuilder();
        JsonWriter writer = new JsonWriter(sb);
        writer.WriteObjectStart();
        {
            WriteBannerImgFilesInfoBase(writer, bannerFilesInfo);

            writer.WritePropertyName("AppVersionList");
            writer.WriteObjectStart();
            {
                if(bannerFilesInfo.appVersionList != null && bannerFilesInfo.appVersionList.Count > 0) {
                    List<string> appVersionKeys = bannerFilesInfo.appVersionList.Keys.ToList();
                    for(int i = 0;i< appVersionKeys.Count; i++) {
                        BannerBaseFilesInfo appVersionBaseFiles = bannerFilesInfo.appVersionList[appVersionKeys[i]];
                        writer.WritePropertyName(appVersionKeys[i]);
                        writer.WriteObjectStart();
                        {
                            WriteBannerImgFilesInfoBase(writer, appVersionBaseFiles);
                        }
                        writer.WriteObjectEnd();
                    }
                }
            }
            writer.WriteObjectEnd();

        }
        writer.WriteObjectEnd();

        return sb.ToString();
    }

    static void WriteBannerImgFilesInfoBase(JsonWriter writer, BannerBaseFilesInfo bannerBaseFilesInfo)
    {
        writer.WritePropertyName("commonList");
        writer.WriteArrayStart();
        {
            if (bannerBaseFilesInfo.commonList != null && bannerBaseFilesInfo.commonList.Count > 0) {
                for (int i = 0; i < bannerBaseFilesInfo.commonList.Count; i++) {
                    BannerImageInfo commonBannerInfo = bannerBaseFilesInfo.commonList[i];
                    writer.WriteObjectStart();
                    {
                        writer.WritePropertyName("bannerName");
                        writer.Write(commonBannerInfo.bannerName);

                        writer.WritePropertyName("uploadPath");
                        writer.Write(commonBannerInfo.uploadPath);

                        writer.WritePropertyName("bannerPathName");
                        writer.Write(commonBannerInfo.bannerPathName);

                        writer.WritePropertyName("fileSize");
                        writer.Write(commonBannerInfo.fileSize);

                        writer.WritePropertyName("uploadDate");
                        writer.Write(commonBannerInfo.uploadDate);
                    }
                    writer.WriteObjectEnd();
                }
            }
        }
        writer.WriteArrayEnd();

        writer.WritePropertyName("languageList");
        writer.WriteObjectStart();
        {
            if (bannerBaseFilesInfo.languageList != null && bannerBaseFilesInfo.languageList.Count > 0) {
                List<string> langKeys = bannerBaseFilesInfo.languageList.Keys.ToList();
                for (int i = 0; i < langKeys.Count; i++) {
                    List<BannerImageInfo> langBannerInfoList = bannerBaseFilesInfo.languageList[langKeys[i]];
                    writer.WritePropertyName(langKeys[i]);
                    writer.WriteArrayStart();
                    {
                        for (int j = 0; j < langBannerInfoList.Count; j++) {
                            BannerImageInfo langBannerInfo = langBannerInfoList[j];
                            writer.WriteObjectStart();
                            {
                                writer.WritePropertyName("bannerName");
                                writer.Write(langBannerInfo.bannerName);

                                writer.WritePropertyName("uploadPath");
                                writer.Write(langBannerInfo.uploadPath);

                                writer.WritePropertyName("bannerPathName");
                                writer.Write(langBannerInfo.bannerPathName);

                                writer.WritePropertyName("fileSize");
                                writer.Write(langBannerInfo.fileSize);

                                writer.WritePropertyName("uploadDate");
                                writer.Write(langBannerInfo.uploadDate);
                            }
                            writer.WriteObjectEnd();
                        }
                    }
                    writer.WriteArrayEnd();
                }
            }
        }
        writer.WriteObjectEnd();
    }

    public static BannerCompareFilesInfo GetJsonBannerCompareFilesInfo(string jsonString)
    {
        BannerCompareFilesInfo retValue = new BannerCompareFilesInfo();

        JsonData compareFilesJson = JsonMapper.ToObject(jsonString);
        retValue.version = (int)compareFilesJson["version"];

        SetCompareFilesBaseInfo(retValue.commonList, retValue.languageList, compareFilesJson);

        JsonData appVersionListJson = compareFilesJson["appVersionList"];
        foreach (DictionaryEntry entry in appVersionListJson as IDictionary) {
            string keyValue = (string)entry.Key;
            JsonData appVersionJson = (JsonData)entry.Value;
            BannerCompareFilesInfo inputAppversionFilesInfo = new BannerCompareFilesInfo();
            retValue.appVersionList.Add(keyValue, inputAppversionFilesInfo);

            SetCompareFilesBaseInfo(inputAppversionFilesInfo.commonList, inputAppversionFilesInfo.languageList, appVersionJson);
        }

        return retValue;
    }

    static void SetCompareFilesBaseInfo(Dictionary<string, BannerImageInfo> commonList, Dictionary<string /* LanguageType */, Dictionary<string, BannerImageInfo>> languageList, JsonData compareFilesJson)
    {
        JsonData commonListJson = compareFilesJson["commonList"];
        for (int i = 0; i < commonListJson.Count; i++) {
            JsonData commonBannerInfoJson = commonListJson[i];

            BannerImageInfo inputCommonBannerInfo = new BannerImageInfo();
            inputCommonBannerInfo.bannerName = (string)commonBannerInfoJson["bannerName"];
            inputCommonBannerInfo.uploadPath = (string)commonBannerInfoJson["uploadPath"];
            inputCommonBannerInfo.bannerPathName = (string)commonBannerInfoJson["bannerPathName"];
            if (commonBannerInfoJson["fileSize"].IsInt) {
                inputCommonBannerInfo.fileSize = (long)(int)commonBannerInfoJson["fileSize"];
            } else if (commonBannerInfoJson["fileSize"].IsLong) {
                inputCommonBannerInfo.fileSize = (long)commonBannerInfoJson["fileSize"];
            }
            inputCommonBannerInfo.uploadDate = (string)commonBannerInfoJson["uploadDate"];

            string[] pathSplit = inputCommonBannerInfo.bannerPathName.Split('#');
            string commonKey = pathSplit[0];
            commonList.Add(commonKey, inputCommonBannerInfo);
        }

        JsonData langListJson = compareFilesJson["languageList"];
        foreach (DictionaryEntry entry in langListJson as IDictionary) {
            string keyValue = (string)entry.Key;
            Dictionary<string, BannerImageInfo> langBannerInfos = new Dictionary<string, BannerImageInfo>();

            JsonData langJson = (JsonData)entry.Value;
            for (int i = 0; i < langJson.Count; i++) {
                JsonData langBannerInfoJson = langJson[i];

                BannerImageInfo inputLangBannerInfo = new BannerImageInfo();
                inputLangBannerInfo.bannerName = (string)langBannerInfoJson["bannerName"];
                inputLangBannerInfo.uploadPath = (string)langBannerInfoJson["uploadPath"];
                inputLangBannerInfo.bannerPathName = (string)langBannerInfoJson["bannerPathName"];
                if (langBannerInfoJson["fileSize"].IsInt) {
                    inputLangBannerInfo.fileSize = (long)(int)langBannerInfoJson["fileSize"];
                } else if (langBannerInfoJson["fileSize"].IsLong) {
                    inputLangBannerInfo.fileSize = (long)langBannerInfoJson["fileSize"];
                }
                inputLangBannerInfo.uploadDate = (string)langBannerInfoJson["uploadDate"];

                string[] pathSplit = inputLangBannerInfo.bannerPathName.Split('#');
                string langKey = pathSplit[0];
                langBannerInfos.Add(langKey, inputLangBannerInfo);
            }

            languageList.Add(keyValue, langBannerInfos);
        }
    }

    public static void WriteBannerImgFilesInfo(string path, string fileName, BannerToolSettingInfo bannerSettingInfo)
    {
        if (bannerSettingInfo == null)
            return;

        try {
            BundleFile.WriteStringToFilePath(JsonMapper.ToJson(bannerSettingInfo), path, fileName);
        } catch (Exception ex) {
            Debug.Log(string.Format("WriteAssetBundlesSetting exception : {0}", ex));
        }
    }

    public static string GetCompareBannerFileKey(BannerDefinitions.MainType bannerType, params string[] bannerValues)
    {
        string retValue = "";

        if (bannerType == BannerDefinitions.MainType.common) {
            retValue = string.Format("{0}_{1}", bannerType.ToString(), bannerValues[0]);
        } else if (bannerType == BannerDefinitions.MainType.Language) {
            retValue = string.Format("{0}_{1}_{2}", bannerType.ToString(), bannerValues[0], bannerValues[1]);
        } else if (bannerType == BannerDefinitions.MainType.platform) {
            if(bannerValues[2] == BannerDefinitions.MainType.common.ToString()) {
                retValue = string.Format("{0}_{1}_{2}_{3}_{4}", bannerType.ToString(), bannerValues[0], bannerValues[1], bannerValues[2], bannerValues[3]);
            } else if(bannerValues[2] == BannerDefinitions.MainType.Language.ToString()) {
                retValue = string.Format("{0}_{1}_{2}_{3}_{4}_{5}", bannerType.ToString(), bannerValues[0], bannerValues[1], bannerValues[2], bannerValues[3], bannerValues[4]);
            }
        }

        return retValue;
    }

    public static BannerImgToolFilesInfo LoadBannerImgToolFilesInfo(string path)
    {
        byte[] readBytes = BannerFile.ReadBytesByPath(path);
        BannerImgToolFilesInfo retValue = null;
        if (readBytes == null) {
            retValue = new BannerImgToolFilesInfo();
        } else {
            string jsonString = Encoding.UTF8.GetString(readBytes);
            retValue = GetBannerImgToolFilesInfoByJson(jsonString);
        }

        return retValue;
    }

    public static void WriteBannerImgToolFilesInfo(BannerImgToolFilesInfo bannerToolFilesInfo, string path)
    {
        byte[] jsonBytes = GetBannerToolFilesBytes(bannerToolFilesInfo);
        BannerFile.WriteBytesFile(jsonBytes, path);
    }

    public static byte[] GetBannerToolFilesBytes(BannerImgToolFilesInfo bannerToolFilesInfo)
    {
        string jsonBannerString = GetJsonStringBannerToolFilesInfo(bannerToolFilesInfo);
        return Encoding.UTF8.GetBytes(jsonBannerString);
    }

    public static BannerImgToolFilesInfo GetBannerImgToolFilesInfoByJson(string bannerJsonString)
    {
        BannerImgToolFilesInfo retValue = new BannerImgToolFilesInfo();

        JsonData bannerToolJson = JsonMapper.ToObject(bannerJsonString);
        retValue.version = (int)bannerToolJson["version"];

        if (((IDictionary)bannerToolJson).Contains("androidAppver")) {
            retValue.androidAppver = (int)bannerToolJson["androidAppver"];
        }

        if (((IDictionary)bannerToolJson).Contains("iOSAppver")) {
            retValue.iOSAppver = (int)bannerToolJson["iOSAppver"];
        }

        if (((IDictionary)bannerToolJson).Contains("supportLangList")) {
            for (int i = 0; i < bannerToolJson["supportLangList"].Count; i++) {
                retValue.supportLangList.Add((string)bannerToolJson["supportLangList"][i]);
            }
        }

        SetBannerImgBaseToolFilesInfo(retValue, bannerToolJson);

        JsonData platformBannerFilesJson = bannerToolJson["platformBannerFilesInfos"];
        foreach (DictionaryEntry entry in platformBannerFilesJson as IDictionary) {
            string platformKey = (string)entry.Key;
            JsonData platformJson = (JsonData)entry.Value;
            
            Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo> inputAppVerBannerFilesInfos = new Dictionary<string, BannerBaseToolFilesInfo>();
            foreach (DictionaryEntry subEntry in platformJson as IDictionary) {
                string appVerKey = (string)subEntry.Key;
                JsonData appVerJson = (JsonData)subEntry.Value;

                BannerBaseToolFilesInfo platformBannerBaseFilesInfo = new BannerBaseToolFilesInfo();
                SetBannerImgBaseToolFilesInfo(platformBannerBaseFilesInfo, appVerJson);

                inputAppVerBannerFilesInfos.Add(appVerKey, platformBannerBaseFilesInfo);
            }

            retValue.platformBannerFilesInfos.Add(platformKey, inputAppVerBannerFilesInfos);
        }

        return retValue;
    }

    static void SetBannerImgBaseToolFilesInfo(BannerBaseToolFilesInfo bannerBaseToolFilesInfo, JsonData bannerToolJson)
    {
        JsonData commonListJson = bannerToolJson["commonList"];
        for (int i = 0; i < commonListJson.Count; i++) {
            JsonData commonBannerJson = commonListJson[i];
            bannerBaseToolFilesInfo.commonList.Add(GetBannerImgToolInfo(commonBannerJson));
        }

        JsonData languageListJson = bannerToolJson["languageList"];
        foreach (DictionaryEntry entry in languageListJson as IDictionary) {
            string keyValue = (string)entry.Key;
            JsonData langJson = (JsonData)entry.Value;
            List<BannerImageToolInfo> inputLangToolInfos = new List<BannerImageToolInfo>();
            for(int i = 0;i< langJson.Count; i++) {
                JsonData langBannerJson = langJson[i];
                inputLangToolInfos.Add(GetBannerImgToolInfo(langBannerJson));
            }

            bannerBaseToolFilesInfo.languageList.Add(keyValue, inputLangToolInfos);
        }
    }

    static BannerImageToolInfo GetBannerImgToolInfo(JsonData bannerToolInfoJson)
    {
        BannerImageToolInfo retValue = new BannerImageToolInfo();

        retValue.originBannerPathName = (string)bannerToolInfoJson["originBannerPathName"];
        retValue.bannerName = (string)bannerToolInfoJson["bannerName"];
        retValue.uploadPath = (string)bannerToolInfoJson["uploadPath"];
        retValue.bannerPathName = (string)bannerToolInfoJson["bannerPathName"];

        if (bannerToolInfoJson["fileSize"].IsInt) {
            retValue.fileSize = (long)(int)bannerToolInfoJson["fileSize"];
        } else if (bannerToolInfoJson["fileSize"].IsLong) {
            retValue.fileSize = (long)bannerToolInfoJson["fileSize"];
        }

        retValue.uploadDate = (string)bannerToolInfoJson["uploadDate"];
        retValue.md5Hash = (string)bannerToolInfoJson["md5Hash"];

        return retValue;
    }

    public static string GetJsonStringBannerToolFilesInfo(BannerImgToolFilesInfo bannerToolFilesInfo)
    {
        StringBuilder sb = new StringBuilder();
        JsonWriter writer = new JsonWriter(sb);
        writer.WriteObjectStart();
        {
            writer.WritePropertyName("version");
            writer.Write(bannerToolFilesInfo.version);

            writer.WritePropertyName("androidAppver");
            writer.Write(bannerToolFilesInfo.androidAppver);

            writer.WritePropertyName("iOSAppver");
            writer.Write(bannerToolFilesInfo.iOSAppver);

            writer.WritePropertyName("supportLangList");
            writer.WriteArrayStart();
            {
                for(int i = 0;i< bannerToolFilesInfo.supportLangList.Count; i++) {
                    writer.Write(bannerToolFilesInfo.supportLangList[i]);
                }
            }
            writer.WriteArrayEnd();

            WriteJsonBannerBaseToolFiles(writer, bannerToolFilesInfo);

            writer.WritePropertyName("platformBannerFilesInfos");
            writer.WriteObjectStart();
            {
                List<string> platformKeys = bannerToolFilesInfo.platformBannerFilesInfos.Keys.ToList();
                for(int i = 0;i< platformKeys.Count; i++) {
                    writer.WritePropertyName(platformKeys[i]);
                    writer.WriteObjectStart();
                    {
                        Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo> appVerBannerInfos = bannerToolFilesInfo.platformBannerFilesInfos[platformKeys[i]];
                        List<string> appVerKeys = appVerBannerInfos.Keys.ToList();
                        for (int j = 0; j < appVerKeys.Count; j++) {
                            writer.WritePropertyName(appVerKeys[j]);
                            writer.WriteObjectStart();
                            {
                                BannerBaseToolFilesInfo platformBaseToolInfo = appVerBannerInfos[appVerKeys[j]];
                                WriteJsonBannerBaseToolFiles(writer, platformBaseToolInfo);
                            }
                            writer.WriteObjectEnd();
                        }
                    }
                    writer.WriteObjectEnd();   
                }
            }
            writer.WriteObjectEnd();
        }
        writer.WriteObjectEnd();

        return sb.ToString();
    }

    public static void WriteJsonBannerBaseToolFiles(JsonWriter writer, BannerBaseToolFilesInfo baseToolFilesInfo)
    {
        writer.WritePropertyName("commonList");
        writer.WriteArrayStart();
        {
            for(int i = 0;i< baseToolFilesInfo.commonList.Count; i++) {
                BannerImageToolInfo bannerToolInfo = baseToolFilesInfo.commonList[i];
                WriteJsonBannerToolInfo(writer, bannerToolInfo);
            }
        }
        writer.WriteArrayEnd();

        writer.WritePropertyName("languageList");
        writer.WriteObjectStart();
        {
            List<string> langKeys = baseToolFilesInfo.languageList.Keys.ToList();
            for(int i = 0;i< langKeys.Count; i++) {
                List<BannerImageToolInfo> bannerLangInfos = baseToolFilesInfo.languageList[langKeys[i]];
                writer.WritePropertyName(langKeys[i]);
                writer.WriteArrayStart();
                {
                    for(int j = 0;j< bannerLangInfos.Count; j++) {
                        BannerImageToolInfo bannerToolInfo = bannerLangInfos[j];
                        WriteJsonBannerToolInfo(writer, bannerToolInfo);
                    }
                }
                writer.WriteArrayEnd();
            }
        }
        writer.WriteObjectEnd();
    }

    public static void WriteJsonBannerToolInfo(JsonWriter writer, BannerImageToolInfo bannerToolInfo)
    {
        writer.WriteObjectStart();
        {
            writer.WritePropertyName("originBannerPathName");
            writer.Write(bannerToolInfo.originBannerPathName);

            writer.WritePropertyName("bannerName");
            writer.Write(bannerToolInfo.bannerName);

            writer.WritePropertyName("uploadPath");
            writer.Write(bannerToolInfo.uploadPath);

            writer.WritePropertyName("bannerPathName");
            writer.Write(bannerToolInfo.bannerPathName);

            writer.WritePropertyName("fileSize");
            writer.Write(bannerToolInfo.fileSize);

            writer.WritePropertyName("uploadDate");
            writer.Write(bannerToolInfo.uploadDate);

            writer.WritePropertyName("md5Hash");
            writer.Write(bannerToolInfo.md5Hash);
        }
        writer.WriteObjectEnd();
    }

    public static BannerImageFilesInfo GetUploadBannerImageFilesInfo(BannerImgToolFilesInfo bannerToolFilesInfo, BannerDefinitions.PlatformType platformType)
    {
        BannerImageFilesInfo retValue = new BannerImageFilesInfo();
        retValue.version = bannerToolFilesInfo.version;
        if(platformType == BannerDefinitions.PlatformType.Android) {
            retValue.appVer = bannerToolFilesInfo.androidAppver;
        } else if (platformType == BannerDefinitions.PlatformType.iOS) {
            retValue.appVer = bannerToolFilesInfo.iOSAppver;
        }

        retValue.supportLangList = new List<string>();
        for (int i = 0; i < bannerToolFilesInfo.supportLangList.Count; i++) {
            retValue.supportLangList.Add(bannerToolFilesInfo.supportLangList[i]);
        }

        SetBannerImageBaseFilesInfo(bannerToolFilesInfo, retValue);

        retValue.appVersionList = new Dictionary<string, BannerBaseFilesInfo>();
        if (bannerToolFilesInfo.platformBannerFilesInfos.ContainsKey(platformType.ToString())) {
            Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo> platformBannerBaseToolInfos = bannerToolFilesInfo.platformBannerFilesInfos[platformType.ToString()];
            List<string> appVerToolKeys = platformBannerBaseToolInfos.Keys.ToList();
            for(int i = 0;i< appVerToolKeys.Count; i++) {
                BannerBaseToolFilesInfo appVerBaseToolInfo = platformBannerBaseToolInfos[appVerToolKeys[i]];
                BannerBaseFilesInfo inputBannerBaseFiles = new BannerBaseFilesInfo();
                SetBannerImageBaseFilesInfo(appVerBaseToolInfo, inputBannerBaseFiles);

                retValue.appVersionList.Add(appVerToolKeys[i], inputBannerBaseFiles);
            }
        }

        return retValue;
    }

    static void SetBannerImageBaseFilesInfo(BannerBaseToolFilesInfo baseToolFilesInfo, BannerBaseFilesInfo bannerBaseInfo)
    {
        bannerBaseInfo.commonList = new List<BannerImageInfo>();

        for (int i = 0; i < baseToolFilesInfo.commonList.Count; i++) {
            BannerImageToolInfo bannerToolInfo = baseToolFilesInfo.commonList[i];
            BannerImageInfo inputBannerInfo = new BannerImageInfo();
            CopyBannerToolInfo(bannerToolInfo, inputBannerInfo);
            bannerBaseInfo.commonList.Add(inputBannerInfo);
        }

        bannerBaseInfo.languageList = new Dictionary<string, List<BannerImageInfo>>();
        List<string> langKeys = baseToolFilesInfo.languageList.Keys.ToList();
        for(int i = 0;i< langKeys.Count; i++) {
            List<BannerImageToolInfo> langToolInfos = baseToolFilesInfo.languageList[langKeys[i]];
            List<BannerImageInfo> inputLangBannerInfos = new List<BannerImageInfo>();
            for(int j = 0;j< langToolInfos.Count; j++) {
                BannerImageToolInfo bannerToolInfo = langToolInfos[j];
                BannerImageInfo inputBannerInfo = new BannerImageInfo();
                CopyBannerToolInfo(bannerToolInfo, inputBannerInfo);
                inputLangBannerInfos.Add(inputBannerInfo);
            }

            bannerBaseInfo.languageList.Add(langKeys[i], inputLangBannerInfos);
        }
    }

    static void CopyBannerToolInfo(BannerImageToolInfo bannerToolInfo, BannerImageInfo bannerInfo)
    {
        bannerInfo.bannerName = bannerToolInfo.bannerName;
        bannerInfo.uploadPath = bannerToolInfo.uploadPath;
        bannerInfo.bannerPathName = bannerToolInfo.bannerPathName;
        bannerInfo.fileSize = bannerToolInfo.fileSize;
        bannerInfo.uploadDate = bannerToolInfo.uploadDate;
        bannerInfo.md5Hash = bannerToolInfo.md5Hash;
    }

    public static Dictionary<string, BannerImageToolInfo> GetCompareBannerImageToolInfo(BannerImgToolFilesInfo bannerToolFilesInfo)
    {
        Dictionary<string, BannerImageToolInfo> retValue = new Dictionary<string, BannerImageToolInfo>();

        if (bannerToolFilesInfo.commonList != null && bannerToolFilesInfo.commonList.Count > 0) {
            for (int i = 0; i < bannerToolFilesInfo.commonList.Count; i++) {
                BannerImageToolInfo bannerInfo = bannerToolFilesInfo.commonList[i];
                string commonKey = GetCompareBannerFileKey(BannerDefinitions.MainType.common, bannerInfo.originBannerPathName);
                retValue.Add(commonKey, bannerInfo);
            }
        }

        if (bannerToolFilesInfo.languageList != null && bannerToolFilesInfo.languageList.Count > 0) {
            List<string> langKeys = bannerToolFilesInfo.languageList.Keys.ToList();
            for (int i = 0; i < langKeys.Count; i++) {
                List<BannerImageToolInfo> langBannerList = bannerToolFilesInfo.languageList[langKeys[i]];
                for (int j = 0; j < langBannerList.Count; j++) {
                    BannerImageToolInfo langBannerInfo = langBannerList[j];
                    string compareLangKey = GetCompareBannerFileKey(BannerDefinitions.MainType.Language, langKeys[i], langBannerInfo.originBannerPathName);
                    retValue.Add(compareLangKey, langBannerInfo);
                }
            }
        }

        List<string> platformKeys = bannerToolFilesInfo.platformBannerFilesInfos.Keys.ToList();
        for(int i = 0;i< platformKeys.Count; i++) {
            string platformString = platformKeys[i];
            Dictionary<string /* AppVersion */, BannerBaseToolFilesInfo> appVerBannerFilesInfo = bannerToolFilesInfo.platformBannerFilesInfos[platformString];

            List<string> appVerKeys = appVerBannerFilesInfo.Keys.ToList();
            for (int j = 0; j < appVerKeys.Count; j++) {
                string appVerKey = appVerKeys[j];
                BannerBaseToolFilesInfo appVerFilesInfo = appVerBannerFilesInfo[appVerKey];
                for (int k = 0; k < appVerFilesInfo.commonList.Count; k++) {
                    BannerImageToolInfo bannerToolInfo = appVerFilesInfo.commonList[k];
                    string appVerCommonKey = GetCompareBannerFileKey(BannerDefinitions.MainType.platform, platformString,
                        appVerKey, BannerDefinitions.MainType.common.ToString(), bannerToolInfo.originBannerPathName);
                    retValue.Add(appVerCommonKey, bannerToolInfo);
                }

                List<string> appVerLangKeys = appVerFilesInfo.languageList.Keys.ToList();
                for (int k = 0; k < appVerLangKeys.Count; k++) {
                    string appVerLangKey = appVerLangKeys[k];
                    List<BannerImageToolInfo> langBannerList = appVerFilesInfo.languageList[appVerLangKey];
                    for (int l = 0; l < langBannerList.Count; l++) {
                        BannerImageToolInfo bannerToolInfo = langBannerList[l];
                        string verCompareLangKey = GetCompareBannerFileKey(BannerDefinitions.MainType.platform, platformString,
                            appVerKey, BannerDefinitions.MainType.Language.ToString(), appVerLangKey, bannerToolInfo.originBannerPathName);

                        retValue.Add(verCompareLangKey, bannerToolInfo);
                    }
                }
            }

        }

        return retValue;
    }
}
