using System;
using Chess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Combat.View
{
    // One row/portrait, reused for both the turn-order rail and the team HP
    // bars. Deliberately dumb - it displays a CombatUnit and raises OnClicked;
    // it has no idea whose turn it is or what a click should mean. CombatView
    // owns all of that logic.
    public class CombatUnitBarEntry : MonoBehaviour
    {
        public TextMeshProUGUI nameLabel;
        public Slider hpSlider;
        public TextMeshProUGUI hpLabel;
        public Image portraitBackground; // placeholder swatch until real portraits exist
        public Button clickButton;
        public GameObject currentActorHighlight;

        public CombatUnit BoundUnit { get; private set; }
        public event Action<CombatUnit> OnClicked;

        void Awake()
        {
            if (clickButton != null)
                clickButton.onClick.AddListener(() => OnClicked?.Invoke(BoundUnit));
        }

        public void Bind(CombatUnit unit)
        {
            BoundUnit = unit;
            if (nameLabel != null) nameLabel.text = unit.Name;
            RefreshHP();
            SetHighlighted(false);
            SetTargetable(false);
        }

        public void RefreshHP()
        {
            if (BoundUnit == null) return;

            if (hpSlider != null)
            {
                hpSlider.maxValue = BoundUnit.MaxHP;
                hpSlider.value = BoundUnit.CurrentHP;
            }

            if (hpLabel != null) hpLabel.text = $"{BoundUnit.CurrentHP}/{BoundUnit.MaxHP}";

            if (portraitBackground != null)
                portraitBackground.color = BoundUnit.IsDefeated ? new Color(0.3f, 0.3f, 0.3f) : Color.white;
        }

        public void SetHighlighted(bool highlighted)
        {
            if (currentActorHighlight != null) currentActorHighlight.SetActive(highlighted);
        }

        public void SetTargetable(bool targetable)
        {
            if (clickButton != null)
                clickButton.interactable = targetable && BoundUnit != null && !BoundUnit.IsDefeated;
        }
    }
}