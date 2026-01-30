using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 期货持仓列表项 Binder
/// </summary>
public class FuturesPositionItemBinder : UIBinder
{
    public Text txtContract;
    public Text txtOpenPrice;
    public Text txtCurrentPrice;
    public Text txtExpiration;
    public Text txtMargin;
    public Text txtPnL;
    public Button btnClose;
}
