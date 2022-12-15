using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BannerToolInfoPopup : MonoBehaviour
{
    #region Serialize Variables

#pragma warning disable 649

    [SerializeField] Text _descText = default(Text);
    [SerializeField] Button _yesButton = default(Button);
    [SerializeField] Button _noButton = default(Button);
    [SerializeField] Button _confirmButton = default(Button);

#pragma warning restore 649

    #endregion

    #region Variables

    Action _onYesAction;
    Action _onNoAction;
    Action _onConfirmAction;

    #endregion

    #region MonoBehaviour Methods

    private void Awake()
    {
        _yesButton.onClick.RemoveAllListeners();
        _yesButton.onClick.AddListener(OnYesButton);

        _noButton.onClick.RemoveAllListeners();
        _noButton.onClick.AddListener(OnNoButton);

        _confirmButton.onClick.RemoveAllListeners();
        _confirmButton.onClick.AddListener(OnConfirmButton);
    }

    #endregion

    #region Methods

    public void ShowYesNoPopup(string desc, Action onYes = null, Action onNo = null)
    {
        this.gameObject.SetActive(true);

        _yesButton.gameObject.SetActive(true);
        _noButton.gameObject.SetActive(true);
        _confirmButton.gameObject.SetActive(false);

        _descText.text = desc;
        _onYesAction = onYes;
        _onNoAction = onNo;
    }

    public void ShowConfirmPopup(string desc, Action onConfirm = null)
    {
        this.gameObject.SetActive(true);

        _yesButton.gameObject.SetActive(false);
        _noButton.gameObject.SetActive(false);
        _confirmButton.gameObject.SetActive(true);

        _descText.text = desc;
        _onConfirmAction = onConfirm;
    }

    public void ClosePopup()
    {
        this.gameObject.SetActive(false);
    }

    #endregion

    #region CallBack Methods

    void OnYesButton()
    {
        if (_onYesAction != null)
            _onYesAction();

        ClosePopup();
    }

    void OnNoButton()
    {
        if (_onNoAction != null)
            _onNoAction();

        ClosePopup();
    }

    void OnConfirmButton()
    {
        if (_onConfirmAction != null)
            _onConfirmAction();

        ClosePopup();
    }

    #endregion
}
