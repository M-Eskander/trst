using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UpgradeCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public Image iconImage;
    public Image backgroundImage;
    public Color commonColor = new Color(0.25f, 0.25f, 0.3f, 0.9f);
    public Color rareColor = new Color(0.15f, 0.25f, 0.55f, 0.9f);
    public Color epicColor = new Color(0.45f, 0.15f, 0.4f, 0.9f);

    private UpgradeManager.UpgradeOption upgrade;

    void Start()
    {
        GetComponent<Button>()?.onClick.AddListener(OnClick);
    }

    public void Setup(UpgradeManager.UpgradeOption upgradeOption)
    {
        upgrade = upgradeOption;

        if (nameText != null) nameText.text = upgradeOption.name;
        if (descriptionText != null) descriptionText.text = upgradeOption.description;
        if (iconImage != null && upgradeOption.icon != null) iconImage.sprite = upgradeOption.icon;

        // Color by rarity
        Color bgColor = upgradeOption.rarity switch
        {
            UpgradeManager.UpgradeRarity.Common => commonColor,
            UpgradeManager.UpgradeRarity.Rare => rareColor,
            UpgradeManager.UpgradeRarity.Epic => epicColor,
            _ => commonColor
        };

        if (backgroundImage != null)
        {
            backgroundImage.color = bgColor;
        }
    }

    void OnClick()
    {
        if (upgrade != null)
        {
            UpgradeManager.Instance.ApplyUpgrade(upgrade);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = Vector3.one * 1.05f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }
}
