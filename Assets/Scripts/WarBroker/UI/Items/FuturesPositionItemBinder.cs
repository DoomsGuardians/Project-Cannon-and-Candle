using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 期货持仓列表项 Binder
/// </summary>
public class FuturesPositionItemBinder : UIBinder
{
    public TMP_Text txtContract;
    public TMP_Text txtOpenPrice;
    public TMP_Text txtCurrentPrice;
    public TMP_Text txtExpiration;
    public TMP_Text txtMargin;
    public TMP_Text txtPnL;
    public Button btnClose;
}
