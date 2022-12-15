using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BannerToolSupportLang : MonoBehaviour
{
    #region Serialize Variables

#pragma warning disable 649

    [SerializeField] Toggle _checkToggle = default(Toggle);
    [SerializeField] Text _langNameText = default(Text);

#pragma warning restore 649

    #endregion

    #region Properties

    public Toggle CheckToggle
    {
        get { return _checkToggle; }
    }

    public Text LangNameText
    {
        get { return _langNameText; }
    }

    #endregion
}
