using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using System.Linq;
using System.Threading;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Renci.SshNet.Common;

public enum BannerFTPUploadState
{
    FTPUploadStart,
    FTPUploading,
    FTPUploadFinish,
}

public class BannerUploadFTPManager
{
    #region SubClass

    public class BannerImgUploadInfo
    {
        public BannerImgUploadInfo(string uri, string local, List<string> paths)
        {
            ftpUri = uri;
            localPath = local;
            pathList = paths;
            uploadType = 0;
        }

        public BannerImgUploadInfo(string uri, byte[] contents, List<string> paths)
        {
            ftpUri = uri;
            uploadBytes = contents;
            pathList = paths;
            uploadType = 1;
        }

        public string ftpUri;
        public string localPath;
        public List<string> pathList;
        public bool isUploading = false;
        public int uploadType = 0; // 0 : Path, 1 : Bytes
        public byte[] uploadBytes;

        public string sftpUploadPath;
    }

    #endregion

    #region Definitions

    public enum BannerFTPUploadStep
    {
        None,
        MakeFTPDirectory,
        UploadBannerImage,
        MakeSFTPDirectory,
        UploadSFTPBannerImage,
    }

    public enum FTPUploadKind
    {
        Resources,
        Sheet,
    }

    #endregion

    #region Variables

    string _rootLocalPath;

    BannerFTPUploadStep _bannerUploadStep;

    string _ftpUploadBannerPath;

    string _ftpURL;
    string _ftpID;
    string _ftpPW;

    byte[] _connectKeyFiles;

    List<BannerImageToolInfo> _bannerImgNameListNew = null;

    Queue<string> _pathQueue = new Queue<string>();
    Queue<BannerImgUploadInfo> _uploadBannerInfoQueue = new Queue<BannerImgUploadInfo>();

    string _curCreatePathName = "";
    BannerImgUploadInfo _curUploadInfo = null;

    Action<BannerFTPUploadState> _onFTPUploadState = null;

    int _curCreatePathCount = 0;
    int _createPathMaxCount = 0;

    int _curUploadCount = 0;
    int _uploadMaxCount = 0;

    bool _isUploadState = false;
    bool _isUpdatePathCreate = false;

    int _curSaveCheckUploadCount = 0;
    int _uploadSaveIntervalCount = 1000;

    string _sftpKeyFile;

    Dictionary<string, string> _pathMakeList = new Dictionary<string, string>();

    List<BannerUploadBytesInfo> _uploadBytesList = new List<BannerUploadBytesInfo>();

    ConnectionInfo _sftpConnectInfo = null;

    #endregion

    #region Properties

    public string RootLocalPath
    {
        get { return _rootLocalPath; }
        set { _rootLocalPath = value; }
    }

    public BannerFTPUploadStep BannerUploadStep
    {
        get { return _bannerUploadStep; }
    }

    public Action<BannerFTPUploadState> OnFTPUploadState
    {
        get { return _onFTPUploadState; }
        set { _onFTPUploadState = value; }
    }

    public int CurUploadCount
    {
        get { return _curUploadCount; }
        set { _curUploadCount = value; }
    }

    public int UploadMaxCount
    {
        get { return _uploadMaxCount; }
        set { _uploadMaxCount = value; }
    }

    public List<BannerUploadBytesInfo> UploadBytesList
    {
        get { return _uploadBytesList; }
        set { _uploadBytesList = value; }
    }

    public string SftpKeyFile
    {
        get { return _sftpKeyFile; }
        set { _sftpKeyFile = value; }
    }

    #endregion

    #region Methods

    public void InitAssetFTPUploader()
    {
        _bannerUploadStep = BannerFTPUploadStep.None;
        _pathQueue.Clear();
        _curUploadInfo = null;

        _pathMakeList.Clear();

        _uploadBytesList.Clear();

        _uploadBannerInfoQueue.Clear();

        _bannerImgNameListNew = null;

        if(_sftpConnectInfo != null) {
            _sftpConnectInfo = null;
        }
    }

    public void UploadFtpBannerImgListNew(string uploadBannerPath, List<BannerImageToolInfo> bannerList, string url, string id, string pw)
    {
        _ftpURL = url;
        _ftpID = id;
        _ftpPW = pw;
        _ftpUploadBannerPath = uploadBannerPath;

        _bannerImgNameListNew = bannerList;

        SetBannerFTPUploadStep(BannerFTPUploadStep.MakeFTPDirectory);
    }

    public void UploadSFtpBannerImgList( string uploadBannerPath, List<BannerImageToolInfo> bannerList, string host, string id)
    {
        _ftpURL = host;
        _ftpID = id;
        _ftpUploadBannerPath = uploadBannerPath;

        _bannerImgNameListNew = bannerList;

        string fileName = Path.Combine(BannerToolUtil.GetSFTPRootPath(), _sftpKeyFile);
        var pk = new PrivateKeyFile(fileName);
        var keyFiles = new[] { pk };

        var methods = new List<AuthenticationMethod>();
        methods.Add(new PrivateKeyAuthenticationMethod(id, keyFiles));

        _sftpConnectInfo = new ConnectionInfo(host,
            id,
            methods.ToArray());

        SetBannerFTPUploadStep(BannerFTPUploadStep.MakeSFTPDirectory);
    }

    public IEnumerator UpdateCreateFolder()
    {
        while (_isUpdatePathCreate) {
            if (_pathQueue.Count == 0) {
                _isUpdatePathCreate = false;
                CheckNextCreateFolderStep();
                continue;
            }

            if (_curCreatePathName != _pathQueue.Peek()) {
                _curCreatePathName = _pathQueue.Peek();
                FtpCreateFolder(_curCreatePathName, _ftpID, _ftpPW);

                if (_bannerUploadStep == BannerFTPUploadStep.MakeFTPDirectory) {
                    string progressTitle = "FTP Create Path Banner Directory";

                    EditorUtility.DisplayProgressBar(progressTitle, _curCreatePathName, (float)_curCreatePathCount / (float)_createPathMaxCount);
                }
            }

            yield return null;
        }

        if (_bannerUploadStep == BannerFTPUploadStep.MakeFTPDirectory)
            EditorUtility.ClearProgressBar();
    }

    public IEnumerator UpdateSFTPCreateFolder()
    {
        using (var sftp = new SftpClient(_sftpConnectInfo)) {
            // SFTP 서버 연결
            sftp.Connect();

            while (_isUpdatePathCreate) {
                if (_pathQueue.Count == 0) {
                    _isUpdatePathCreate = false;
                    CheckNextCreateFolderStep();
                    continue;
                }

                if (_curCreatePathName != _pathQueue.Peek()) {
                    _curCreatePathName = _pathQueue.Peek();
                    CreateSFtpFolder(sftp, _curCreatePathName);

                    if (_bannerUploadStep == BannerFTPUploadStep.MakeSFTPDirectory) {
                        string progressTitle = "FTP Create Path Banner Directory";

                        EditorUtility.DisplayProgressBar(progressTitle, _curCreatePathName, (float)_curCreatePathCount / (float)_createPathMaxCount);
                    }
                }

                yield return null;
            }

            sftp.Disconnect();
            sftp.Dispose();
        }

        if (_bannerUploadStep == BannerFTPUploadStep.MakeSFTPDirectory)
            EditorUtility.ClearProgressBar();
    }

    public bool DirectoryExists(string directory)
    {
        bool directoryExists;
        var request = (FtpWebRequest)FtpWebRequest.Create(directory);
        request.Method = WebRequestMethods.Ftp.ListDirectory;
        request.Credentials = new NetworkCredential(_ftpID, _ftpPW);

        try {
            using (var resp = (FtpWebResponse)request.GetResponse()) {
                Debug.Log(string.Format("DirectoryExists Exist dicrectory : {0}", directory));
                directoryExists = true;
                resp.Close();
            }
        } catch (WebException) {
            directoryExists = false;
        }
        return directoryExists;
    }

    public IEnumerator UpdateUploadFile()
    {
        while (_isUploadState) {
            if (_uploadBannerInfoQueue.Count == 0) {
                FinishFTPUpload();
                continue;
            }

            if (_curUploadInfo != _uploadBannerInfoQueue.Peek()) {
                _curUploadInfo = _uploadBannerInfoQueue.Peek();

                if (_curUploadInfo.pathList != null && _curUploadInfo.pathList.Count > 0) {
                    _createPathMaxCount = 0;
                    for (int i = 0; i < _curUploadInfo.pathList.Count; i++) {
                        if (!_pathMakeList.ContainsKey(_curUploadInfo.pathList[i])) {
                            _pathQueue.Enqueue(_curUploadInfo.pathList[i]);
                            _createPathMaxCount++;
                        }
                    }
                }

                if (_pathQueue.Count > 0) {
                    _isUpdatePathCreate = true;
                    _curCreatePathCount = 0;
                    EditorCoroutine.start(UpdateCreateFolder());
                    continue;
                }

                if(_curUploadInfo.uploadType == 0) {
                    UploadBannerFile(_curUploadInfo.ftpUri, _curUploadInfo.localPath, _ftpID, _ftpPW);
                } else if(_curUploadInfo.uploadType == 1){
                    UploadBannerFileByBytes(_curUploadInfo.ftpUri, _curUploadInfo.uploadBytes, _ftpID, _ftpPW);
                }

                string progressTitle = "FTP Upload Banner Image";
                EditorUtility.DisplayProgressBar(progressTitle, _curUploadInfo.localPath, (float)_curUploadCount / (float)_uploadMaxCount);
            }

            yield return null;
        }

        EditorUtility.ClearProgressBar();
    }

    public IEnumerator UpdateSFTPUploadFile()
    {
        using (var sftp = new SftpClient(_sftpConnectInfo)) {
            // SFTP 서버 연결
            sftp.Connect();

            while (_isUploadState) {
                if (_uploadBannerInfoQueue.Count == 0) {
                    FinishFTPUpload();
                    continue;
                }

                if (_curUploadInfo != _uploadBannerInfoQueue.Peek()) {
                    _curUploadInfo = _uploadBannerInfoQueue.Peek();

                    if (_curUploadInfo.pathList != null && _curUploadInfo.pathList.Count > 0) {
                        _createPathMaxCount = 0;
                        for (int i = 0; i < _curUploadInfo.pathList.Count; i++) {
                            if (!_pathMakeList.ContainsKey(_curUploadInfo.pathList[i])) {
                                _pathQueue.Enqueue(_curUploadInfo.pathList[i]);
                                _createPathMaxCount++;
                            }
                        }
                    }

                    if (_pathQueue.Count > 0) {
                        foreach(var path in _pathQueue) {
                            CreateOnlySFtpFolder(sftp, path);
                        }
                        _pathQueue.Clear();
                        _createPathMaxCount = 0;
                    }

                    if (_curUploadInfo.uploadType == 0) {
                        UploadSFTPBannerFile(_curUploadInfo.sftpUploadPath, _curUploadInfo.localPath, sftp);
                    } else if (_curUploadInfo.uploadType == 1) {
                        UploadSFTPBannerFileByBytes(_curUploadInfo.sftpUploadPath, _curUploadInfo.uploadBytes, sftp);
                    }

                    string progressTitle = "FTP Upload Banner Image";
                    EditorUtility.DisplayProgressBar(progressTitle, _curUploadInfo.localPath, (float)_curUploadCount / (float)_uploadMaxCount);
                }

                yield return null;
            }

            sftp.Disconnect();
            sftp.Dispose();
        }

        EditorUtility.ClearProgressBar();
    }

    void SetBannerFTPUploadStep(BannerFTPUploadStep step)
    {
        _bannerUploadStep = step;
        switch (_bannerUploadStep) {
            case BannerFTPUploadStep.MakeFTPDirectory:
                SetMakeBannerDirectoryStep();
                break;
            case BannerFTPUploadStep.UploadBannerImage:
                SetUploadFTPBannerStepNew();
                break;
            case BannerFTPUploadStep.MakeSFTPDirectory:
                SetMakeSFTPDirectoryStep();
                break;
            case BannerFTPUploadStep.UploadSFTPBannerImage:
                SetUploadSFTPBannerStep();
                break;
        }
    }

    void SetMakeSFTPDirectoryStep()
    {
        _pathQueue.Clear();

        _curCreatePathName = "";
        _curCreatePathCount = 0;
        _createPathMaxCount = 0;

        string[] pathSplit = _ftpUploadBannerPath.Split('/');
        if (pathSplit != null && pathSplit.Length > 1) {
            string curPath = "";
            for (int i = 0; i < pathSplit.Length; i++) {
                curPath += pathSplit[i];
                _pathQueue.Enqueue(curPath);
                curPath += '/';
            }
        } else {
            _pathQueue.Enqueue(_ftpUploadBannerPath);
        }

        _createPathMaxCount++;

        if (_pathQueue.Count > 0) {
            _isUpdatePathCreate = true;
            EditorCoroutine.start(UpdateSFTPCreateFolder());
        } else {
            SetBannerFTPUploadStep(BannerFTPUploadStep.UploadSFTPBannerImage);
        }
    }

    void SetUploadSFTPBannerStep()
    {
        _curUploadInfo = null;

        _curUploadCount = 0;
        _uploadMaxCount = 0;
        _uploadBannerInfoQueue.Clear();

        _curSaveCheckUploadCount = 0;

        string ftpPath = _ftpURL + _ftpUploadBannerPath + '/';

        for (int i = 0; i < _uploadBytesList.Count; i++) {
            AddBannerBytesInfo(_uploadBytesList[i].uploadPath, _uploadBytesList[i].uploadBytes);
        }

        Dictionary<string, string> cachePathList = new Dictionary<string, string>();

        for (int i = 0; i < _bannerImgNameListNew.Count; i++) {
            BannerImageToolInfo bannerToolInfo = _bannerImgNameListNew[i];
            List<string> pathList = AssetBundleUtil.GetAssetBundleNamePathList(bannerToolInfo.uploadPath);
            List<string> inputPathList = new List<string>();
            if (pathList.Count > 0) {
                string curPath = "";
                for (int j = 0; j < pathList.Count; j++) {
                    if (string.IsNullOrEmpty(pathList[j])) continue;

                    curPath += "/" + pathList[j];
                    if (!cachePathList.ContainsKey(curPath)) {
                        cachePathList.Add(curPath, "");
                        string ftpPathUri = _ftpUploadBannerPath + curPath;
                        inputPathList.Add(ftpPathUri);
                    }
                }
            }

            string ftpUri = ftpPath + bannerToolInfo.uploadPath;
            string localPath = bannerToolInfo.localPathName;
            string sftpUploadPath = string.Format("./{0}/{1}", _ftpUploadBannerPath, bannerToolInfo.uploadPath);
            AddSFTPBannerInfo(ftpUri, localPath, inputPathList, sftpUploadPath);
        }

        if (_onFTPUploadState != null) {
            _onFTPUploadState(BannerFTPUploadState.FTPUploadStart);
        }

        _isUploadState = true;

        EditorCoroutine.start(UpdateSFTPUploadFile());
    }

    void SetMakeBannerDirectoryStep()
    {
        _pathQueue.Clear();

        _curCreatePathName = "";
        _curCreatePathCount = 0;
        _createPathMaxCount = 0;

        string[] pathSplit = _ftpUploadBannerPath.Split('/');
        if(pathSplit != null && pathSplit.Length > 1) {
            string curPath = "";
            for(int i = 0;i< pathSplit.Length; i++) {
                curPath += pathSplit[i];
                _pathQueue.Enqueue(_ftpURL + curPath);
                curPath += '/';
            }
        } else {
            _pathQueue.Enqueue(_ftpURL + _ftpUploadBannerPath);
        }
        
        _createPathMaxCount++;

        if (_pathQueue.Count > 0) {
            _isUpdatePathCreate = true;
            EditorCoroutine.start(UpdateCreateFolder());
        } else {
            SetBannerFTPUploadStep(BannerFTPUploadStep.UploadBannerImage);
        }
    }


    public void AddBannerBytesInfo(string bannerPath, byte[] contents)
    {
        string ftpPath = _ftpURL + _ftpUploadBannerPath + '/';

        string bannerInfoFtpUri = ftpPath + bannerPath;
        BannerImgUploadInfo inputUploadInfo = new BannerImgUploadInfo(bannerInfoFtpUri, contents, null);
        inputUploadInfo.sftpUploadPath = string.Format("./{0}/{1}", _ftpUploadBannerPath, bannerPath);
        _uploadBannerInfoQueue.Enqueue(inputUploadInfo);

        _uploadMaxCount++;
    }

    void SetUploadFTPBannerStepNew()
    {
        _curUploadInfo = null;

        _curUploadCount = 0;
        _uploadMaxCount = 0;
        _uploadBannerInfoQueue.Clear();

        _curSaveCheckUploadCount = 0;

        string ftpPath = _ftpURL + _ftpUploadBannerPath + '/';

        for (int i = 0; i < _uploadBytesList.Count; i++) {
            AddBannerBytesInfo(_uploadBytesList[i].uploadPath, _uploadBytesList[i].uploadBytes);
        }

        Dictionary<string, string> cachePathList = new Dictionary<string, string>();

        for (int i = 0; i < _bannerImgNameListNew.Count; i++) {
            BannerImageToolInfo bannerToolInfo = _bannerImgNameListNew[i];
            List<string> pathList = AssetBundleUtil.GetAssetBundleNamePathList(bannerToolInfo.uploadPath);
            List<string> inputPathList = new List<string>();
            if (pathList.Count > 0) {
                string curPath = "";
                for (int j = 0; j < pathList.Count; j++) {
                    if (string.IsNullOrEmpty(pathList[j])) continue;

                    curPath += "/" + pathList[j];
                    if (!cachePathList.ContainsKey(curPath)) {
                        cachePathList.Add(curPath, "");
                        string ftpPathUri = _ftpURL + _ftpUploadBannerPath + curPath;
                        inputPathList.Add(ftpPathUri);
                    }
                }
            }

            string ftpUri = ftpPath + bannerToolInfo.uploadPath;
            string localPath = bannerToolInfo.localPathName;
            AddBannerInfo(ftpUri, localPath, inputPathList);
        }

        if (_onFTPUploadState != null) {
            _onFTPUploadState(BannerFTPUploadState.FTPUploadStart);
        }

        _isUploadState = true;

        EditorCoroutine.start(UpdateUploadFile());
    }

    void AddBannerInfo(string ftpUri, string localPath, List<string> pathList)
    {
        _uploadBannerInfoQueue.Enqueue(new BannerImgUploadInfo(ftpUri, localPath, pathList));

        _uploadMaxCount++;
    }

    void AddSFTPBannerInfo(string ftpUri, string localPath, List<string> pathList, string sftpUploadPath)
    {
        BannerImgUploadInfo inputUploadInfo = new BannerImgUploadInfo(ftpUri, localPath, pathList);
        inputUploadInfo.sftpUploadPath = sftpUploadPath;
        _uploadBannerInfoQueue.Enqueue(inputUploadInfo);

        _uploadMaxCount++;
    }

    void FtpCreateFolder(string uriPath, string id, string pw)
    {
        if (string.IsNullOrEmpty(uriPath)) return;

        try {
            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(uriPath);
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            request.Credentials = new NetworkCredential(id, pw);

            using (var resp = (FtpWebResponse)request.GetResponse()) {
                Debug.Log(resp.StatusCode);

                resp.Close();
                _pathQueue.Dequeue();

                _curCreatePathCount++;

                if (!_pathMakeList.ContainsKey(uriPath))
                    _pathMakeList.Add(uriPath, "");
            }
        } catch (WebException ex) {
            FtpWebResponse response = (FtpWebResponse)ex.Response;
            if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable) {
                response.Close();

                _pathQueue.Dequeue();
                _curCreatePathCount++;

                if (!_pathMakeList.ContainsKey(uriPath))
                    _pathMakeList.Add(uriPath, "");
            } else {
                response.Close();
                string exString = string.Format("Fail uriPath : {0}, reason : {1}", uriPath, ex);
                Debug.Log(exString);
                _isUpdatePathCreate = false;
                EditorUtility.ClearProgressBar();
                if (EditorUtility.DisplayDialog("Banner Image",
                   exString, "OK")) {

                }
            }
        }
    }

    void CheckNextCreateFolderStep()
    {
        if (_bannerUploadStep == BannerFTPUploadStep.MakeFTPDirectory) {
            SetBannerFTPUploadStep(BannerFTPUploadStep.UploadBannerImage);
        } else if (_bannerUploadStep == BannerFTPUploadStep.UploadBannerImage) {
            _curUploadInfo = null;
        }

        if (_bannerUploadStep == BannerFTPUploadStep.MakeSFTPDirectory) {
            SetBannerFTPUploadStep(BannerFTPUploadStep.UploadSFTPBannerImage);
        } else if (_bannerUploadStep == BannerFTPUploadStep.UploadSFTPBannerImage) {
            _curUploadInfo = null;
        }
    }

    void UploadBannerFile(string uri, string localPath, string id, string pw)
    {
        if (string.IsNullOrEmpty(uri)) return;
        if (string.IsNullOrEmpty(localPath)) return;

        try {
            FileStream fs = new FileStream(localPath, FileMode.Open, FileAccess.Read);
            if (fs != null) {
                byte[] contents = new byte[fs.Length];
                if (contents != null) {
                    fs.Read(contents, 0, (int)contents.Length);
                } else return;

                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(id, pw);
                request.ContentLength = fs.Length;

                Stream rqStream = request.GetRequestStream();
                rqStream.Write(contents, 0, contents.Length);
                rqStream.Close();

                using (var resp = (FtpWebResponse)request.GetResponse()) {
                    resp.Close();

                    _uploadBannerInfoQueue.Dequeue();
                    _curUploadCount++;

                    Debug.LogFormat("Success UpLoad : {0}, StatusCode : {1}", uri, resp.StatusCode);
                }
            }
        } catch (Exception ex) {
            string exString = string.Format("Exception uri : {0}, localPath : {1}, reason : {2}", uri, localPath, ex);
            Debug.Log(exString);
            _isUploadState = false;

            EditorUtility.ClearProgressBar();
            if (EditorUtility.DisplayDialog("Banner Image",
               exString, "OK")) {

            }
        }
    }

    void UploadSFTPBannerFile(string uri, string localPath, SftpClient client)
    {
        Debug.Log(string.Format("UploadSFTPBannerFile uri : {0}, localPath : {1}", uri, localPath));

        if (string.IsNullOrEmpty(uri)) return;
        if (string.IsNullOrEmpty(localPath)) return;

        try {
            FileStream fs = new FileStream(localPath, FileMode.Open, FileAccess.Read);
            if (fs != null) {
                client.UploadFile(fs, uri);

                _uploadBannerInfoQueue.Dequeue();
                _curUploadCount++;
            }
        } catch (Exception ex) {
            string exString = string.Format("Exception uri : {0}, localPath : {1}, reason : {2}", uri, localPath, ex);
            Debug.Log(exString);
            _isUploadState = false;

            EditorUtility.ClearProgressBar();
            if (EditorUtility.DisplayDialog("Banner Image",
               exString, "OK")) {

            }
        }
    }

    void DeleteBannerFile(string uri, string id, string pw)
    {
        if (string.IsNullOrEmpty(uri)) return;

        try {
            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            request.Credentials = new NetworkCredential(id, pw);

            using (var resp = (FtpWebResponse)request.GetResponse()) {
                resp.Close();
            }
        } catch (Exception ex) {
            string exString = string.Format("DeleteBannerFile Exception uri : {0}, reason : {1}", uri, ex);
            Debug.Log(exString);

            EditorUtility.ClearProgressBar();
            if (EditorUtility.DisplayDialog("Delete Banner Image Fail",
               exString, "OK")) {

            }
        }
    }

    void UploadBannerFileByBytes(string uri, byte[] contents, string id, string pw)
    {
        if (string.IsNullOrEmpty(uri)) return;

        try {
            if (contents != null) {
                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(id, pw);
                request.ContentLength = contents.Length;

                Stream rqStream = request.GetRequestStream();
                rqStream.Write(contents, 0, contents.Length);
                rqStream.Close();

                using (var resp = (FtpWebResponse)request.GetResponse()) {
                    resp.Close();

                    _uploadBannerInfoQueue.Dequeue();
                    _curUploadCount++;
                }
            }
        } catch (Exception ex) {
            string exString = string.Format("UploadBannerFileByBytes Exception uri : {0}, reason : {1}", uri, ex);
            Debug.Log(exString);
            _isUploadState = false;

            EditorUtility.ClearProgressBar();
            if (EditorUtility.DisplayDialog("Banner Image",
               exString, "OK")) {

            }
        }
    }

    void UploadSFTPBannerFileByBytes(string uri, byte[] contents, SftpClient client)
    {
        Debug.Log(string.Format("UploadSFTPBannerFileByBytes uri : {0}", uri));

        if (string.IsNullOrEmpty(uri)) return;

        try {
            if (contents != null) {
                var uploadStream = new MemoryStream(contents);
                client.UploadFile(uploadStream, uri);

                _uploadBannerInfoQueue.Dequeue();
                _curUploadCount++;
            }
        } catch (Exception ex) {
            string exString = string.Format("UploadBannerFileByBytes Exception uri : {0}, reason : {1}", uri, ex);
            Debug.Log(exString);
            _isUploadState = false;

            EditorUtility.ClearProgressBar();
            if (EditorUtility.DisplayDialog("Banner Image",
               exString, "OK")) {

            }
        }
    }

    void FinishFTPUpload()
    {
        if (_onFTPUploadState != null) {
            _onFTPUploadState(BannerFTPUploadState.FTPUploadFinish);
        }

        _bannerUploadStep = BannerFTPUploadStep.None;

        _isUploadState = false;
        Debug.Log(string.Format("FinishFTPUpload"));
    }

    public void UploadSFTPFile(string host, string id, string uploadPath, byte[] contents)
    {
        string fileName  = Path.Combine(BannerToolUtil.GetSFTPRootPath(), _sftpKeyFile);
        var pk = new PrivateKeyFile(fileName);
        var keyFiles = new[] { pk };

        var methods = new List<AuthenticationMethod>();
        methods.Add(new PrivateKeyAuthenticationMethod(id, keyFiles));

        var ci = new ConnectionInfo(host,
            id,
            methods.ToArray());

        using (var sftp = new SftpClient(ci)) {
            // SFTP 서버 연결
            sftp.Connect();

            var uploadStream = new MemoryStream(contents);
            sftp.UploadFile(uploadStream, uploadPath);

            sftp.Disconnect();
            sftp.Dispose();
        }
    }

    void CreateSFtpFolder(SftpClient client, string createPathName)
    {
        try {
            SftpFileAttributes attrs = client.GetAttributes(createPathName);
            if (!attrs.IsDirectory) {
                throw new Exception("not directory");
            }
        } catch (SftpPathNotFoundException) {
            client.CreateDirectory(createPathName);
        }

        _pathQueue.Dequeue();
        _curCreatePathCount++;
    }

    void CreateOnlySFtpFolder(SftpClient client, string createPathName)
    {
        try {
            SftpFileAttributes attrs = client.GetAttributes(createPathName);

            if (!attrs.IsDirectory) {
                throw new Exception("not directory");
            }
        } catch (SftpPathNotFoundException) {
            client.CreateDirectory(createPathName);
        }
    }

    #endregion
}

#endif
