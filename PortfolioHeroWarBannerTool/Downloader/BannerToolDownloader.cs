using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BannerToolDownloader : MonoBehaviour
{
    #region Methods

    public void AddDownloadFile(string webURL, string filePath, Action<string, UnityWebRequest> onComplete, Action<string, string> onFail)
    {
        StartCoroutine(DownLoadBannerFile(webURL, filePath, onComplete, onFail));
    }

    #endregion

    #region Coroutine Methods

    IEnumerator DownLoadBannerFile(string webURL, string filePath, Action<string, UnityWebRequest> onComplete, Action<string, string> onFail)
    {
        string url = webURL + filePath + "?t=" + TimeUtil.GetTimeStamp().ToString();
        Debug.Log(string.Format("DownLoadBannerFile url : {0}", url));

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();
        if (request.isNetworkError || request.isHttpError) {
            Debug.Log(request.error);
            if (onFail != null)
                onFail(filePath, request.error);
        } else {
            Debug.Log("File successfully downloaded and saved to " + filePath);

            if (onComplete != null)
                onComplete(filePath, request);
        }
    }

    #endregion
}
