using UnityEngine;
using UnityEngine.UI;

public class GeneralPanelBinder : UIBinder
{
    [System.Serializable]
    public class GeneralCardRef
    {
        public Text txtName;
        public Text txtPersonality;
        public Slider sliderTroops;
        public Slider sliderTrust;
        public Slider sliderMorale;
        public Text txtStatus;
        public Button btnATK;
        public Button btnDEF;
        public Button btnRET;
        public Text txtSkills;
        public Image imgATKHighlight;
        public Image imgDEFHighlight;
        public Image imgRETHighlight;
    }

    public GeneralCardRef[] generalCards = new GeneralCardRef[3];
}
