using UnityEngine;
using TMPro;

public class TooltipPanelBinder : UIBinder
{
    [Header("Tooltip 内容")]
    public TMP_Text txtTitle;
    public TMP_Text txtContent;
    public RectTransform panelRect;
}
