using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗结算弹窗 Binder
/// </summary>
public class BattleResultPopupBinder : UIBinder
{
    public Text txtTitle;
    public Transform resultContainer;
    public GameObject resultItemPrefab;
    public Button btnConfirm;
}
