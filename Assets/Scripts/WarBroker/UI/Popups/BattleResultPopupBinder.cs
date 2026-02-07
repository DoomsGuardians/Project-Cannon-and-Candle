using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 战斗结算弹窗 Binder
/// </summary>
public class BattleResultPopupBinder : UIBinder
{
    public TMP_Text txtTitle;
    public Transform resultContainer;
    public GameObject resultItemPrefab;
    public Button btnConfirm;
    public TMP_Text txtBtnConfirm;
    public Image imgIllustration;

    [Header("音效")]
    public AudioClip sfxEnter;
}
